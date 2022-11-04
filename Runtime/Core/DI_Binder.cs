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
    /// Handles class fields marked by <see cref="DI_Inject"/> and <see cref="DI_Install"/> dependency binding
    /// </summary>
    public static class DI_Binder
    {
        private class Listener
        {
            public FieldInfo field;
            public object target;
            public MethodInfo callback;

            public void Call(object dependency)
            {
                if (callback != null)
                {
#if ENABLE_DI_LOGS
                    DebugLog("Invoking", callback, target, dependency);
#endif
                    callback.Invoke(target, new object[] { dependency });
                }

#if ENABLE_DI_LOGS
                DebugLog("Injecting", field, target, dependency);
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

        private static Dictionary<Type, ListenerList> _listenerLists = new Dictionary<Type, ListenerList>();
        private static Dictionary<Type, DependencyList> _dependencyLists = new Dictionary<Type, DependencyList>();

        private static void Clear()
        {
            _listenerLists = new Dictionary<Type, ListenerList>();
            _dependencyLists = new Dictionary<Type, DependencyList>();
        }

        private static ListenerList GetListeners(Type type)
        {
            if (!_listenerLists.TryGetValue(type, out var result))
                _listenerLists[type] = result = new ListenerList();
            return result;
        }

        private static void AddListener(Type type, Listener listener)
        {
            var listeners = GetListeners(type);
            listeners.Add(listener);
        }

        private static void RemoveListeners(Type type, object target)
        {
            var listeners = GetListeners(type);
            listeners.Remove(target);
        }

        private static DependencyList GetDependencies(Type type)
        {
            if (!_dependencyLists.TryGetValue(type, out var result))
                _dependencyLists[type] = result = new DependencyList();
            return result;
        }

        private static void AddDependency(Type type, object dependency)
        {
            var dependencies = GetDependencies(type);
            dependencies.Add(dependency);
            var listeners = GetListeners(type);
            listeners.Call(dependency);
        }

        private static void RemoveDependency(Type type, object target)
        {
            var dependencies = GetDependencies(type);
            if (dependencies.Remove(target))
            {
                var listeners = GetListeners(type);
                if (dependencies.TryGetLast(out var dependency))
                {
                    listeners.Call(dependency);
                }
            }
        }

        private static void Install(object target, DI_Install attribute)
        {
            var type = attribute.type ?? target.GetType();

#if ENABLE_DI_LOGS
            DebugLog("Installing", target);
#endif
            AddDependency(type, target);
        }

        private static void Uninstall(object target, DI_Install attribute)
        {
            var type = attribute.type ?? target.GetType();

#if ENABLE_DI_LOGS
            DebugLog("Uninstalling", target);
#endif
            RemoveDependency(type, target);
        }

        private static void Inject(FieldInfo field, object target, DI_Inject attribute)
        {
            var type = attribute.type ?? field.FieldType;
            var callback = attribute.callback ?? "On" + type.Name + "Inject";

            var targetType = target.GetType();
            var methodBindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = targetType.GetMethod(callback, methodBindings);

            var listener = new Listener
            {
                field = field,
                target = target,
                callback = method
            };

            AddListener(type, listener);

            var dependencies = GetDependencies(type);
            if (dependencies.TryGetLast(out var dependency))
            {
                listener.Call(dependency);
            }
        }

        private static void Uninject(FieldInfo field, object target, DI_Inject attribute)
        {
            var type = attribute.type ?? field.FieldType;

            RemoveListeners(type, target);

#if ENABLE_DI_LOGS
            DebugLog("Uninjecting", field, target);
#endif
            field.SetValue(target, null);
        }

        public static void Bind(object target)
        {
            var type = target.GetType();

            if (type.TryGetCustomAttribute<DI_Install>(out var installAttribute))
            {
                Install(target, installAttribute);
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.TryGetCustomAttribute<DI_Inject>(out var injectAttribute))
                {
                    Inject(field, target, injectAttribute);
                }
            }
        }

        public static void Unbind(object target)
        {
            var type = target.GetType();

            if (type.TryGetCustomAttribute<DI_Install>(out var installAttribute))
            {
                Uninstall(target, installAttribute);
            }

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.TryGetCustomAttribute<DI_Inject>(out var injectAttribute))
                {
                    Uninject(field, target, injectAttribute);
                }
            }
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
        private static void DebugLog(string message, object field, object target, object value)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#00FFFF>[{field}]</color> of <color=#FF8000>[{target}]</color> with value <color=#FFFFFF>[{value}]</color>");
        }

        private static void DebugLog(string message, object field, object target)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#00FFFF>[{field}]</color> of <color=#FF8000>[{target}]</color>");
        }

        private static void DebugLog(string message, object target)
        {
            UnityEngine.Debug.Log($"[{nameof(DI_Binder)}] {message} <color=#FF8000>[{target}]</color>");
        }
#endif
    }
}
