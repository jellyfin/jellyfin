
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

        /// <summary>
        /// Determines whether this instance [can self restart].
        /// </summary>
        /// <returns><c>true</c> if this instance [can self restart]; otherwise, <c>false</c>.</returns>
        public static bool CanSelfRestart
        {
            get
            {
                return MainStartup.CanSelfRestart;
            }
        }
    }
}
