using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common.Injection
{
    internal static class Extensions
    {
        #region FieldInfo
        public static bool TryGetCustomAttribute<T>(this FieldInfo self, out T attribute)
            where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        #endregion

        #region MethodInfo
        public static bool TryGetCustomAttribute<T>(this MethodInfo self, out T attribute)
            where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }

        public static bool TryGetMethodParameterType(this MethodInfo self, out Type type)
        {
            var parameters = self.GetParameters();
            type = parameters.Length == 1 ? parameters[0].ParameterType : null;
            return type != null;
        }
        #endregion

        #region List
        public static bool TryGetAt<T>(this List<T> self, int index, out T item)
        {
            if (-1 < index && index < self.Count)
            {
                item = self[index];
                return true;
            }
            item = default;
            return false;
        }

        public static bool TryGetLast<T>(this List<T> self, out T item)
        {
            return self.TryGetAt(self.Count - 1, out item);
        }
        #endregion

        #region Type
        public static IEnumerable<FieldInfo> GetAllFields(this Type self, BindingFlags bindingAttr)
        {
            if (self != null)
            {
                return self.GetFields(bindingAttr | BindingFlags.DeclaredOnly)
                    .Concat(self.BaseType.GetAllFields(bindingAttr));
            }
            return Enumerable.Empty<FieldInfo>();
        }

        public static IEnumerable<MethodInfo> GetAllMethods(this Type self, BindingFlags bindingAttr)
        {
            if (self != null)
            {
                return self.GetMethods(bindingAttr | BindingFlags.DeclaredOnly)
                .Concat(self.BaseType.GetAllMethods(bindingAttr));
            }
            return Enumerable.Empty<MethodInfo>();
        }
        #endregion
    }
}
