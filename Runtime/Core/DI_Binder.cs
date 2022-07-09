using System;
using System.Collections.Generic;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif
using UnityEngine;

namespace Common.Injection
{
    /// <summary>
    /// Handles class fields marked by <see cref="DI_Inject"/> and <see cref="DI_Install"/> dependency binding
    /// </summary>
    public static class DI_Binder
    {
        private class ListenerList : List<(object target, Action<object> callback)>
        {
            public void Call(object dependency)
            {
                this.ForEach(listener => listener.callback.Invoke(dependency));
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

        private static void AddListener(Type type, object target, Action<object> callback)
        {
            var listeners = GetListeners(type);
            listeners.Add((target, callback));
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

        private static void Install(FieldInfo field, object target, DI_Install attribute)
        {
            var type = attribute.type ?? field.FieldType;

            var dependency = field.GetValue(target);

            // Unity-specific. Could be abstracted in the future by using DI_Provider attribute on methods
            if (dependency == null)
            {
                if (type.IsSubclassOf(typeof(Component)))
                {
                    dependency = UnityEngine.Object.FindObjectOfType(type);

                    if (dependency == null)
                    {
                        var gameObject = new GameObject($"{type.Name}(Inject)");
                        dependency = gameObject.AddComponent(type);
                    }
                }
            }
            else
            {
                if (
                    dependency is Component component &&
                    component.gameObject.IsPrefab()
                )
                {
                    dependency = UnityEngine.Object.Instantiate(component);
                }
            }
            //

            if (dependency == null)
            {
                var args = attribute.args;
                dependency = args != null ? Activator.CreateInstance(type, args) : Activator.CreateInstance(type);
            }
            
#if ENABLE_DI_LOGS
            DebugLog("Installing", field, target, dependency);
#endif
            field.SetValue(target, dependency);

            AddDependency(type, dependency);
        }

        private static void Uninstall(FieldInfo field, object target, DI_Install attribute)
        {
            var type = attribute.type ?? field.FieldType;

            var dependency = field.GetValue(target);

            if (dependency != null)
            {
                RemoveDependency(type, dependency);
            }

#if ENABLE_DI_LOGS
            DebugLog("Uninstalling", field, target);
#endif
            field.SetValue(target, null);
        }

        private static void Inject(FieldInfo field, object target, DI_Inject attribute)
        {
            var type = attribute.type ?? field.FieldType;
            var callback = attribute.callback ?? "On" + type.Name + "Inject";

            void Update(object value)
            {
                if (value != null)
                {
                    var targetType = target.GetType();
                    var method = targetType.GetMethod(callback, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
#if ENABLE_DI_LOGS
                        DebugLog("Invoking", method, target, value);
#endif
                        method.Invoke(target, new object[] { value });
                    }
                }

#if ENABLE_DI_LOGS
                DebugLog("Injecting", field, target, value);
#endif
                field.SetValue(target, value);
            }

            AddListener(type, target, Update);

            var dependencies = GetDependencies(type);
            if (dependencies.TryGetLast(out var dependency))
            {
                Update(dependency);
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
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.TryGetCustomAttribute<DI_Install>(out var attributeInstall))
                {
                    Install(field, target, attributeInstall);
                }
                else if (field.TryGetCustomAttribute<DI_Inject>(out var attributeInject))
                {
                    Inject(field, target, attributeInject);
                }
            }
        }

        public static void Unbind(object target)
        {
            var type = target.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.TryGetCustomAttribute<DI_Inject>(out var attributeInject))
                {
                    Uninject(field, target, attributeInject);
                }
                else if (field.TryGetCustomAttribute<DI_Install>(out var attributeInstall))
                {
                    Uninstall(field, target, attributeInstall);
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
#endif
    }
}
