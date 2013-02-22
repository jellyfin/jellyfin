
namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// An interface to be implemented by the applications hosting a kernel
    /// </summary>
    public interface IApplicationHost
    {
        /// <summary>
        /// Restarts this instance.
        /// </summary>
        void Restart();

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        void ReloadLogger();
    }
}
