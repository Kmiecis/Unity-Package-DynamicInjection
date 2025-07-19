using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace Common.Injection
{
    /// <summary>
    /// Handles class fields marked by <see cref="DI_Inject"/> or <see cref="DI_Update"/> and classes by <see cref="DI_Install"/> dependency binding
    /// </summary>
    public static class DI_Binder
    {
        private class Listener
        {
            public object target;
            public FieldInfo field;
            public MethodInfo method;

            public virtual void Call(object dependency)
            {
                if (method != null && dependency != null)
                {
#if ENABLE_DI_LOGS
                    DebugLog("Invoking", target, method, dependency);
#endif
                    method.Invoke(target, new object[] { dependency });
                }

                if (field != null)
                {
#if ENABLE_DI_LOGS
                    DebugLog("Injecting", target, field, dependency);
#endif
                    field.SetValue(target, dependency);
                }
            }
        }

        private class ListenerList : List<Listener>
        {
            public void Call(object dependency)
            {
                this.ForEach(listener => listener.Call(dependency));
            }

            public void Remove(object target)
            {
                this.RemoveAll(listener => listener.target == target);
            }
        }

        private class DependencyList : List<object>
        {
        }

        private class InstallData
        {
            public Type type;
        }

        private class InjectData
        {
            public Type type;
            public FieldInfo field;
            public MethodInfo method;
            public bool updater;
        }

        private class ReflectionData
        {
            public List<InstallData> installs;
            public List<InjectData> injects;
        }

        private static Dictionary<Type, DependencyList> _dependencyLists = new Dictionary<Type, DependencyList>();
        private static Dictionary<Type, ListenerList> _injectorLists = new Dictionary<Type, ListenerList>();
        private static Dictionary<Type, ListenerList> _updaterLists = new Dictionary<Type, ListenerList>();
        private static Dictionary<Type, ReflectionData> _reflections = new Dictionary<Type, ReflectionData>();

        /// <summary>
        /// Binds <paramref name="target"/> by handling Injects and Installs, defined by assigned attributes.
        /// </summary>
        public static void Bind(object target)
        {
            var type = target.GetType();
            var list = EnsureReflection(type);

            if (list.installs != null)
            {
                foreach (var install in list.installs)
                {
                    Install(target, install);
                }
            }

            if (list.injects != null)
            {
                foreach (var inject in list.injects)
                {
                    Inject(target, inject);
                }
            }
        }

        /// <summary>
        /// Unbinds <paramref name="target"/> by clearing Injects and Installs, defined by assigned attributes.
        /// </summary>
        public static void Unbind(object target)
        {
            var type = target.GetType();
            var list = EnsureReflection(type);

            if (list.installs != null)
            {
                foreach (var install in list.installs)
                {
                    Uninstall(target, install);
                }
            }

            if (list.injects != null)
            {
                foreach (var inject in list.injects)
                {
                    Uninject(target, inject);
                }
            }
        }

        /// <summary>
        /// Manually Installs <paramref name="target"/> as Dependency of <paramref name="type"/> .
        /// </summary>
        public static void Install(object target, Type type)
        {
#if ENABLE_DI_LOGS
            DebugLog("Installing", target);
#endif
            AddDependency(type, target);
            RefreshDependency(type);
        }

        /// <summary>
        /// Manually Installs <paramref name="target"/> as Dependency of Type <typeparamref name="T"/>.
        /// </summary>
        public static void Install<T>(T target)
        {
            Install(target, typeof(T));
        }

        /// <summary>
        /// Uninstalls manually assigned Dependency <paramref name="target"/> of <paramref name="type"/>.
        /// </summary>
        public static void Uninstall(object target, Type type)
        {
#if ENABLE_DI_LOGS
            DebugLog("Uninstalling", target);
#endif
            if (RemoveDependency(type, target))
            {
                RefreshDependency(type);
            }
        }

        /// <summary>
        /// Uninstalls manually assigned <paramref name="target"/> Dependency of Type <typeparamref name="T"/>.
        /// </summary>
        public static void Uninstall<T>(T target)
        {
            Uninstall(target, typeof(T));
        }

        private static void Install(object target, InstallData install)
        {
            Install(target, install.type);
        }

        private static void Uninstall(object target, InstallData install)
        {
            Uninstall(target, install.type);
        }

        private static void Inject(object target, InjectData inject)
        {
            var listener = new Listener
            {
                target = target,
                field = inject.field,
                method = inject.method
            };

            var injected = RefreshListener(inject.type, listener);

            if (inject.updater)
            {
                AddUpdater(inject.type, listener);
            }
            else if (!injected)
            {
                AddInjector(inject.type, listener);
            }
        }

        private static void Uninject(object target, InjectData inject)
        {
#if ENABLE_DI_LOGS
            DebugLog("Uninjecting", target, inject.field);
#endif
            RemoveUpdater(inject.type, target);

            inject.field.SetValue(target, null);
        }

        private static bool RefreshListener(Type type, Listener listener)
        {
            if (TryGetDependencies(type, out var dependencies) &&
                dependencies.TryGetLast(out var dependency))
            {
                listener.Call(dependency);
                return true;
            }
            return false;
        }

        private static void RefreshDependency(Type type)
        {
            if (!TryGetDependencies(type, out var dependencies) ||
                !dependencies.TryGetLast(out var dependency))
            {
                dependency = null;
            }

            if (TryGetUpdaters(type, out var updaters))
            {
                updaters.Call(dependency);
            }

            if (TryGetInjectors(type, out var injectors))
            {
                injectors.Call(dependency);
                injectors.Clear();

                RemoveInjectors(type);
            }
        }

        #region Dependencies
        private static void AddDependency(Type type, object dependency)
        {
            EnsureDependencies(type).Add(dependency);
        }

        private static bool RemoveDependency(Type type, object dependency)
        {
            if (TryGetDependencies(type, out var dependencies))
            {
                var changed = dependencies.Remove(dependency);

                if (dependencies.Count == 0)
                {
                    RemoveDependencies(type);
                }

                return changed;
            }
            return false;
        }

        private static bool TryGetDependencies(Type type, out DependencyList dependencies)
        {
            return _dependencyLists.TryGetValue(type, out dependencies);
        }

        private static DependencyList EnsureDependencies(Type type)
        {
            if (!_dependencyLists.TryGetValue(type, out var dependencies))
                _dependencyLists[type] = dependencies = new DependencyList();
            return dependencies;
        }

        private static void RemoveDependencies(Type type)
        {
            _dependencyLists.Remove(type);
        }
        #endregion

        #region Updaters
        private static void AddUpdater(Type type, Listener updater)
        {
            EnsureUpdaters(type).Add(updater);
        }

        private static void RemoveUpdater(Type type, object updater)
        {
            if (TryGetUpdaters(type, out var updaters))
            {
                updaters.Remove(updater);
                if (updaters.Count == 0)
                {
                    RemoveUpdaters(type);
                }
            }
        }

        private static bool TryGetUpdaters(Type type, out ListenerList updaters)
        {
            return _updaterLists.TryGetValue(type, out updaters);
        }

        private static ListenerList EnsureUpdaters(Type type)
        {
            if (!_updaterLists.TryGetValue(type, out var updaters))
                _updaterLists[type] = updaters = new ListenerList();
            return updaters;
        }

        private static void RemoveUpdaters(Type type)
        {
            _updaterLists.Remove(type);
        }
        #endregion

        #region Injectors
        private static void AddInjector(Type type, Listener injector)
        {
            EnsureInjectors(type).Add(injector);
        }

        private static void RemoveInjector(Type type, object injector)
        {
            if (TryGetInjectors(type, out var injectors))
            {
                injectors.Remove(injector);
                if (injectors.Count == 0)
                {
                    RemoveInjectors(type);
                }
            }
        }

        private static bool TryGetInjectors(Type type, out ListenerList injectors)
        {
            return _injectorLists.TryGetValue(type, out injectors);
        }

        private static ListenerList EnsureInjectors(Type type)
        {
            if (!_injectorLists.TryGetValue(type, out var injectors))
                _injectorLists[type] = injectors = new ListenerList();
            return injectors;
        }

        private static void RemoveInjectors(Type type)
        {
            _injectorLists.Remove(type);
        }
        #endregion

        private static ReflectionData EnsureReflection(Type type)
        {
            if (!_reflections.TryGetValue(type, out var result))
                _reflections[type] = result = CreateReflection(type);
            return result;
        }

        private static ReflectionData CreateReflection(Type type)
        {
            var injects = CreateInjectDatas(type);
            var installs = CreateInstallDatas(type);

            if (installs == null && injects == null)
            {   // Special case, for external installs
                installs = new List<InstallData> { new InstallData { type = type } };
            }

            return new ReflectionData()
            {
                installs = installs,
                injects = injects
            };
        }

        private static List<InstallData> CreateInstallDatas(Type type)
        {
            var result = new List<InstallData>();

            CreateInstallDatas(type, result);

            return result.Count > 0 ? result : null;
        }

        private static void CreateInstallDatas(Type type, List<InstallData> result)
        {
            foreach (var attribute in type.GetCustomAttributes<DI_Install>())
            {
                var data = new InstallData
                {
                    type = attribute.type ?? type
                };

                result.Add(data);
            }

            foreach (var item in type.GetInterfaces())
            {
                CreateInstallDatas(item, result);
            }

            if (type.BaseType != null)
            {
                CreateInstallDatas(type.BaseType, result);
            }
        }

        private static List<InjectData> CreateInjectDatas(Type type)
        {
            const BindingFlags BINDINGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var result = new List<InjectData>();

            var fields = type.GetAllFields(BINDINGS);
            foreach (var field in fields)
            {
                var inject = CreateInjectData(type, field);
                if (inject != null)
                {
                    result.Add(inject);
                }
            }

            var methods = type.GetAllMethods(BINDINGS);
            foreach (var method in methods)
            {
                var inject = CreateInjectData(type, method);
                if (inject != null)
                {
                    result.Add(inject);
                }
            }

            return result.Count > 0 ? result : null;
        }

        private static InjectData CreateInjectData(Type type, FieldInfo field)
        {
            if (field.TryGetCustomAttribute<DI_Inject>(out var attribute))
            {
                var injectType = attribute.type ?? field.FieldType;

                return new InjectData()
                {
                    type = injectType,
                    field = field,
                    updater = attribute is DI_Update
                };
            }

            return null;
        }

        private static InjectData CreateInjectData(Type type, MethodInfo method)
        {
            if (method.TryGetMethodParameterType(out var parameterType))
            {
                if (method.TryGetCustomAttribute<DI_Inject>(out var attribute))
                {
                    var injectType = attribute.type ?? parameterType;

                    return new InjectData()
                    {
                        type = injectType,
                        method = method,
                        updater = attribute is DI_Update
                    };
                }
                // Backward compatibility
                else if (method.Name == $"On{parameterType.Name}Inject")
                {
                    UnityEngine.Debug.LogWarning($"[{nameof(DI_Binder)}] implicit Inject is now Obsolete.\nMark the method {method.Name} explicitly with {nameof(DI_Inject)} or {nameof(DI_Update)} to keep the functionality working in the future.");

                    return new InjectData()
                    {
                        type = parameterType,
                        method = method
                    };
                }
            }

            return null;
        }

        private static void Clear()
        {
            _dependencyLists = new Dictionary<Type, DependencyList>();
            _injectorLists = new Dictionary<Type, ListenerList>();
            _updaterLists = new Dictionary<Type, ListenerList>();
            _reflections = new Dictionary<Type, ReflectionData>();
        }

#if UNITY_EDITOR
        [DidReloadScripts]
        private static void OnReloadScripts()
        {
            Clear();

            void OnPlayModeStateChanged(PlayModeStateChange change)
            {
                if (change == PlayModeStateChange.ExitingPlayMode)
                {
                    Clear();
                }
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
#endif

#if ENABLE_DI_LOGS
        private const string TARGET_COLOR = "FF8000";
        private const string FIELD_COLOR = "00FFFF";
        private const string VALUE_COLOR = "FFFFFF";

        private static void DebugLog(string message, object target, object field, object value)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#{FIELD_COLOR}>[{field}]</color> of <color=#{TARGET_COLOR}>[{target}]</color> with value <color=#{VALUE_COLOR}>[{value}]</color>");
        }

        private static void DebugLog(string message, object target, object field)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#{FIELD_COLOR}>[{field}]</color> of <color=#{TARGET_COLOR}>[{target}]</color>");
        }

        private static void DebugLog(string message, object target)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#{TARGET_COLOR}>[{target}]</color>");
        }

        private static void DebugWarning(string message, object target)
        {
            UnityEngine.Debug.LogWarning($"[{nameof(DI_Binder)}] {message} <color=#{TARGET_COLOR}>[{target}]</color>");
        }
#endif
    }
}
