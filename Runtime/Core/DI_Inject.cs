using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a field or a method to have a dependency injected into, once, by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class DI_Inject : Attribute
    {
        /// <summary>
        /// Allows defining a type by which dependency should be injected. Defaults to field type
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Allows defining a callback method to call upon dependency injection. Defaults to On{type}Inject
        /// </summary>
        [Obsolete]
        public readonly string callback;

        [Obsolete("Use the Attribute on desired Method")]
        public DI_Inject(Type type, string callback)
        {
            this.type = type;
            this.callback = callback;
        }

        public DI_Inject(Type type)
        {
            this.type = type;
        }

        [Obsolete("Use the Attribute on desired Method")]
        public DI_Inject(string callback)
        {
            this.callback = callback;
        }

        public DI_Inject()
        {
        }
    }
}
