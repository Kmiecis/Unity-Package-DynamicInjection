using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a field to have it's value installed as a dependency by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DI_Install : Attribute
    {
        /// <summary>
        /// Allows defining a type by which dependency should be installed. Defaults to field type
        /// </summary>
        public readonly Type type;

        public DI_Install(Type type)
        {
            this.type = type;
        }
        
        public DI_Install() :
            this(null)
        {
        }
    }
}
