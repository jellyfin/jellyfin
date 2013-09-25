
namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class NativeApp
    /// </summary>
    public static class NativeApp
    {
        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public static void Shutdown()
        {
            MainStartup.Shutdown();
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public static void Restart()
        {
            MainStartup.Restart();
        }
    }
}
