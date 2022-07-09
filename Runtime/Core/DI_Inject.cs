using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a field to have a dependency injected into by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DI_Inject : Attribute
    {
        /// <summary>
        /// Allows defining a type by which dependency should be injected. Defaults to field type
        /// </summary>
        public readonly Type type;
        /// <summary>
        /// Allows defining a callback method to call upon dependency injection. Defaults to On{type}Inject
        /// </summary>
        public readonly string callback;
        
        public DI_Inject(Type type, string callback)
        {
            this.type = type;
            this.callback = callback;
        }

        public DI_Inject(Type type) :
            this(type, null)
        {
        }

        public DI_Inject(string callback) :
            this(null, callback)
        {
        }

        public DI_Inject() :
            this(null, null)
        {
        }
    }
}
