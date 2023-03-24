﻿using System;
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
            public MethodInfo callback;

            public virtual void Call(object dependency)
            {
                if (callback != null && dependency != null)
                {
#if ENABLE_DI_LOGS
                    DebugLog("Invoking", target, callback, dependency);
#endif
                    callback.Invoke(target, new object[] { dependency });
                }

#if ENABLE_DI_LOGS
                DebugLog("Injecting", target, field, dependency);
#endif
                field.SetValue(target, dependency);
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
            public MethodInfo callback;
            public bool updater;
        }

        private class ReflectionData
        {
            public InstallData install;
            public List<InjectData> injects;
        }

        private static Dictionary<Type, DependencyList> _dependencyLists = new Dictionary<Type, DependencyList>();
        private static Dictionary<Type, ListenerList> _injectorLists = new Dictionary<Type, ListenerList>();
        private static Dictionary<Type, ListenerList> _updaterLists = new Dictionary<Type, ListenerList>();
        private static Dictionary<Type, ReflectionData> _reflections = new Dictionary<Type, ReflectionData>();

        public static void Bind(object target)
        {
            var type = target.GetType();
            var list = EnsureReflection(type);

            if (list.install != null)
            {
                Install(target, list.install);
            }

            if (list.injects != null)
            {
                foreach (var inject in list.injects)
                {
                    Inject(target, inject);
                }
            }
        }

        public static void Unbind(object target)
        {
            var type = target.GetType();
            var list = EnsureReflection(type);

            if (list.install != null)
            {
                Uninstall(target, list.install);
            }

            if (list.injects != null)
            {
                foreach (var inject in list.injects)
                {
                    Uninject(target, inject);
                }
            }
        }

        private static void Install(object target, InstallData install)
        {
#if ENABLE_DI_LOGS
            DebugLog("Installing", target);
#endif

            AddDependency(install.type, target);
            RefreshDependency(install.type);
        }

        private static void Uninstall(object target, InstallData install)
        {
#if ENABLE_DI_LOGS
            DebugLog("Uninstalling", target);
#endif

            if (RemoveDependency(install.type, target))
            {
                RefreshDependency(install.type);
            }
        }

        private static void Inject(object target, InjectData inject)
        {
            var listener = new Listener
            {
                target = target,
                field = inject.field,
                callback = inject.callback
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

        private static bool RefreshDependency(Type type)
        {
            if (TryGetDependencies(type, out var dependencies) &&
                dependencies.TryGetLast(out var dependency))
            {
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

                return true;
            }
            return false;
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
                var changed = ReferenceEquals(dependencies.Last(), dependency);

                dependencies.Remove(dependency);
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
            var install = CreateInstallData(type, injects);

            return new ReflectionData()
            {
                install = install,
                injects = injects
            };
        }

        private static InstallData CreateInstallData(Type type, List<InjectData> injects)
        {
            if (type.TryGetCustomAttribute<DI_Install>(out var attribute))
            {
                return new InstallData
                {
                    type = attribute.type ?? type
                };
            }
            if (injects == null)
            {
                return new InstallData
                {
                    type = type
                };
            }
            return null;
        }
        
        private static List<InjectData> CreateInjectDatas(Type type)
        {
            const BindingFlags FIELD_BINDINGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            var fields = type.GetFields(FIELD_BINDINGS);

            if (fields.Length > 0)
            {
                var result = new List<InjectData>();
                foreach (var field in fields)
                {
                    var inject = CreateInjectData(type, field);
                    if (inject != null)
                    {
                        result.Add(inject);
                    }
                }

                if (result.Count > 0)
                {
                    return result;
                }
            }

            return null;
        }

        private static InjectData CreateInjectData(Type type, FieldInfo field)
        {
            const BindingFlags METHOD_BINDINGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            if (field.TryGetCustomAttribute<DI_Inject>(out var injectAttribute))
            {
                var injectType = injectAttribute.type ?? field.FieldType;
                var callback = injectAttribute.callback ?? $"On{injectType.Name}Inject";
                var method = type.GetMethod(callback, METHOD_BINDINGS);

                return new InjectData()
                {
                    type = injectType,
                    field = field,
                    callback = method,
                    updater = injectAttribute is DI_Update
                };
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
