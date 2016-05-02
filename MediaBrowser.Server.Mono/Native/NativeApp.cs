using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;

namespace MediaBrowser.Server.Mono.Native
{
    /// <summary>
    /// Class NativeApp
    /// </summary>
    internal class NativeApp : BaseMonoApp
    {
        public NativeApp(StartupOptions startupOptions, ILogger logger)
            : base(startupOptions, logger)
        {
        }

        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public override void Shutdown()
        {
            MainClass.Shutdown();
        }

        /// <summary>
        /// Determines whether this instance [can self restart].
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public override bool CanSelfRestart
        {
            get
            {
                // A restart script must be provided
                return StartupOptions.ContainsOption("-restartpath");
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public override void Restart(StartupOptions startupOptions)
        {
            MainClass.Restart(startupOptions);
        }
    }
}
