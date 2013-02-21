using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MediaBrowser.UI.UserInput
{
    /// <summary>
    /// Provides a basic low-level keyboard listener
    /// Inspired by http://blogs.msdn.com/b/toub/archive/2006/05/03/589423.aspx
    /// Use the KeyDown event to listen for keys.
    /// Make sure to detach from the event when not needed.
    /// </summary>
    public static class KeyboardListener
    {
        #region KeyDown EventHandler
        /// <summary>
        /// The _ key down
        /// </summary>
        static volatile EventHandler<KeyEventArgs> _KeyDown;
        /// <summary>
        /// Fires whenever CurrentItem changes
        /// </summary>
        public static event EventHandler<KeyEventArgs> KeyDown
        {
            add
            {
                if (_KeyDown == null)
                {
                    StartListening();
                }

                _KeyDown += value;
            }
            remove
            {
                _KeyDown -= value;

                if (_KeyDown == null && _hookID != IntPtr.Zero)
                {
                    StopListening();
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:KeyDown" /> event.
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        private static void OnKeyDown(KeyEventArgs e)
        {
            e.SuppressKeyPress = false;

            if (_KeyDown != null)
            {
                // For now, don't async this
                // This will give listeners a chance to modify SuppressKeyPress if they want
                try
                {
                    _KeyDown(null, e);
                }
                catch (Exception ex)
                {
                }
            }
        }
        #endregion

        /// <summary>
        /// The W h_ KEYBOAR d_ LL
        /// </summary>
        private const int WH_KEYBOARD_LL = 13;
        /// <summary>
        /// The W m_ KEYDOWN
        /// </summary>
        private const int WM_KEYDOWN = 0x0100;
        /// <summary>
        /// The W m_ SYSKEYDOWN
        /// </summary>
        private const int WM_SYSKEYDOWN = 0x0104;

        /// <summary>
        /// The _hook ID
        /// </summary>
        private static IntPtr _hookID = IntPtr.Zero;
        /// <summary>
        /// The _proc
        /// </summary>
        private static LowLevelKeyboardProc _proc = HookCallback;

        /// <summary>
        /// Starts the listening.
        /// </summary>
        private static void StartListening()
        {
            _hookID = SetHook(_proc);
        }

        /// <summary>
        /// Stops the listening.
        /// </summary>
        private static void StopListening()
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }

        /// <summary>
        /// Sets the hook.
        /// </summary>
        /// <param name="proc">The proc.</param>
        /// <returns>IntPtr.</returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// Hooks the callback.
        /// </summary>
        /// <param name="nCode">The n code.</param>
        /// <param name="wParam">The w param.</param>
        /// <param name="lParam">The l param.</param>
        /// <returns>IntPtr.</returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            var suppressKeyPress = false;

            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                {
                    var vkCode = Marshal.ReadInt32(lParam);

                    var keyData = (Keys)vkCode;

                    var e = new KeyEventArgs(keyData);

                    OnKeyDown(e);

                    suppressKeyPress = e.SuppressKeyPress;
                }
            }

            if (suppressKeyPress)
            {
                return IntPtr.Zero;
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Delegate LowLevelKeyboardProc
        /// </summary>
        /// <param name="nCode">The n code.</param>
        /// <param name="wParam">The w param.</param>
        /// <param name="lParam">The l param.</param>
        /// <returns>IntPtr.</returns>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Imports
        /// <summary>
        /// Sets the windows hook ex.
        /// </summary>
        /// <param name="idHook">The id hook.</param>
        /// <param name="lpfn">The LPFN.</param>
        /// <param name="hMod">The h mod.</param>
        /// <param name="dwThreadId">The dw thread id.</param>
        /// <returns>IntPtr.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// Unhooks the windows hook ex.
        /// </summary>
        /// <param name="hhk">The HHK.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// Calls the next hook ex.
        /// </summary>
        /// <param name="hhk">The HHK.</param>
        /// <param name="nCode">The n code.</param>
        /// <param name="wParam">The w param.</param>
        /// <param name="lParam">The l param.</param>
        /// <returns>IntPtr.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Gets the module handle.
        /// </summary>
        /// <param name="lpModuleName">Name of the lp module.</param>
        /// <returns>IntPtr.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}
