using System;
using System.Runtime.InteropServices;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class NativeApp
    /// </summary>
    public static class Standby
    {
        public static void PreventSleepAndMonitorOff()
        {
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);
        }

        public static void PreventSleep()
        {
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
        }

        // Clear EXECUTION_STATE flags to allow the system to sleep and turn off monitor normally
        public static void AllowSleep()
        {
            NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
        }

        internal static class NativeMethods
        {
            // Import SetThreadExecutionState Win32 API and necessary flags
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern uint SetThreadExecutionState(uint esFlags);
            public const uint ES_CONTINUOUS = 0x80000000;
            public const uint ES_SYSTEM_REQUIRED = 0x00000001;
            public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        }

        [Flags]
        internal enum EXECUTION_STATE : uint
        {
            ES_NONE = 0,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_USER_PRESENT = 0x00000004,
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000
        }
    }
}
