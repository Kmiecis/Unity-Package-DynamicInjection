namespace Common.Injection
{
    /// <summary>
    /// Base <see cref="DI_IDependant"/> implementation
    /// </summary>
    public abstract class DI_ADependant : DI_IDependant
    {
        public void Bind()
        {
            DI_Binder.Bind(this);
        }

        public void Unbind()
        {
            DI_Binder.Unbind(this);
        }
    }
}
