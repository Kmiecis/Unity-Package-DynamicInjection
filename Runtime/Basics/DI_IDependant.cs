namespace Common.Injection
{
    /// <summary>
    /// Dependency Injection dependant interface to implement reliable binding behaviour
    /// </summary>
    public interface DI_IDependant
    {
        /// <summary>
        /// Binds <see cref="DI_Inject"/> and <see cref="DI_Install"/> fields
        /// </summary>
        void Bind();

        /// <summary>
        /// Unbinds <see cref="DI_Inject"/> and <see cref="DI_Install"/> fields
        /// </summary>
        void Unbind();
    }
}
