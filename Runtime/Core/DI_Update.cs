using System;

namespace Common.Injection
{
    /// <summary>
    /// Marks a field or a method to have a dependency kept updated into, by <see cref="DI_Binder"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method)]
    public class DI_Update : DI_Inject
    {
        public DI_Update(Type type, string callback) :
            base(type, callback)
        {
        }

        public DI_Update(Type type) :
            base(type, null)
        {
        }

        public DI_Update(string callback) :
            base(null, callback)
        {
        }

        public DI_Update() :
            base(null, null)
        {
        }
    }
}
