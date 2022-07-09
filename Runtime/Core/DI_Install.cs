using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a field to have it's value installed as a dependency by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DI_Install : Attribute
    {
        /// <summary>
        /// Allows defining a type by which dependency should be installed
        /// </summary>
        public readonly Type type;

        /// <summary>
        /// Allows passing additional arguments to be passed into dependency constructor
        /// </summary>
        public readonly object[] args;
        
        public DI_Install(Type type = null, object[] args = null)
        {
            this.type = type;
            this.args = args;
        }
        
        public DI_Install(Type type) :
            this(type, null)
        {
        }

        public DI_Install(object[] args) :
            this(null, args)
        {
        }
    }
}
