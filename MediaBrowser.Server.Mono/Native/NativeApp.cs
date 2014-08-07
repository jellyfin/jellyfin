using MediaBrowser.Server.Mono;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Updates;

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
			MainClass.Shutdown ();
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public static void Restart()
        {
			MainClass.Restart ();
		}

		/// <summary>
		/// Determines whether this instance [can self restart].
		/// </summary>
		/// <returns><c>true</c> if this instance [can self restart]; otherwise, <c>false</c>.</returns>
		public static bool CanSelfRestart
		{
			get
			{
				return MainClass.CanSelfRestart;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance can self update.
		/// </summary>
		/// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
		public static bool CanSelfUpdate
		{
			get
			{
				return MainClass.CanSelfUpdate;
			}
		}

		public static bool SupportsAutoRunAtStartup
		{
			get { return false; }
		}

        public static void PreventSystemStandby()
        {
            
        }

        public async Task<CheckForUpdateResult> CheckForApplicationUpdate(Version currentVersion,
            PackageVersionClass updateLevel,
            IInstallationManager installationManager,
            CancellationToken cancellationToken,
            IProgress<double> progress)
        {
            var result = new CheckForUpdateResult { AvailableVersion = currentVersion.ToString(), IsUpdateAvailable = false };
			
			return Task.FromResult(result);
        }
    }
}
