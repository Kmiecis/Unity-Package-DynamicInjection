using UnityEngine;

namespace Common.Injection
{
    /// <summary>
    /// Base <see cref="DI_IDependant"/> implementation for <see cref="MonoBehaviour"/>
    /// </summary>
    public abstract class DI_ADependantBehaviour : MonoBehaviour
    {
        protected virtual void Awake()
        {
            DI_Binder.Bind(this);
        }

        protected virtual void OnDestroy()
        {
            DI_Binder.Unbind(this);
        }
    }
}
