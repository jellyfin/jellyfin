using System.Runtime.InteropServices;

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

        /// <summary>
        /// Gets a value indicating whether [supports automatic run at startup].
        /// </summary>
        /// <value><c>true</c> if [supports automatic run at startup]; otherwise, <c>false</c>.</value>
        public static bool SupportsAutoRunAtStartup
        {
            get
            {
                return true;
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
                return MainStartup.CanSelfUpdate;
            }
        }

        public static void PreventSystemStandby()
        {
            SystemHelper.ResetStandbyTimer();
        }

        internal enum EXECUTION_STATE : uint
        {
            ES_NONE = 0,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_USER_PRESENT = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000
        }

        public class SystemHelper
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

            public static void ResetStandbyTimer()
            {
                EXECUTION_STATE es = SetThreadExecutionState(EXECUTION_STATE.ES_SYSTEM_REQUIRED);
            }
        }
    }
}
