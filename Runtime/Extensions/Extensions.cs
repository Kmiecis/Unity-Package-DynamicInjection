using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        #region GameObject
        public static bool IsPrefab(this GameObject self)
        {
            return self.scene.rootCount == 0;
        }
        #endregion

        #region List
        public static T Last<T>(this List<T> self)
        {
            return self[self.Count - 1];
        }

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
        #endregion
    }
}
