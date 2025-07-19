using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a class to have itself installed as a dependency by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class DI_Install : Attribute
    {
        /// <summary>
        /// Allows defining a type by which dependency should be installed. Defaults to class type
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
