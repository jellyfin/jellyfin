using System;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Mac
{
	/// <summary>
	/// Class NativeApp
	/// </summary>
	public class NativeApp : BaseMonoApp
	{
		public NativeApp(ILogger logger)
	            : base(logger)
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
                return true;
            }
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
		public override void Restart(StartupOptions options)
        {
            MainClass.Restart();
        }
	}
}

