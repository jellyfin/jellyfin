using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using MediaBrowser.Controller.Power;
using MediaBrowser.Model.Logging;
using Microsoft.Win32.SafeHandles;

namespace MediaBrowser.ServerApplication.Native
{
    public class WindowsPowerManagement : IPowerManagement
    {
        [DllImport("kernel32.dll")]
        public static extern SafeWaitHandle CreateWaitableTimer(IntPtr lpTimerAttributes,
                                                                  bool bManualReset,
                                                                string lpTimerName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWaitableTimer(SafeWaitHandle hTimer,
                                                    [In] ref long pDueTime,
                                                              int lPeriod,
                                                           IntPtr pfnCompletionRoutine,
                                                           IntPtr lpArgToCompletionRoutine,
                                                             bool fResume);

        private BackgroundWorker _bgWorker;
        private readonly ILogger _logger;
        private readonly object _initLock = new object();

        public WindowsPowerManagement(ILogger logger)
        {
            _logger = logger;
        }

        public void ScheduleWake(DateTime utcTime)
        {
            //Initialize();
            //_bgWorker.RunWorkerAsync(utcTime.ToFileTime());
            throw new NotImplementedException();
        }

        private void Initialize()
        {
            lock (_initLock)
            {
                if (_bgWorker == null)
                {
                    _bgWorker = new BackgroundWorker();

                    _bgWorker.DoWork += bgWorker_DoWork;
                    _bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
                }
            }
        }

        void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //if (Woken != null)
            //{
            //    Woken(this, new EventArgs());
            //}
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                long waketime = (long)e.Argument;

                using (SafeWaitHandle handle = CreateWaitableTimer(IntPtr.Zero, true, GetType().Assembly.GetName().Name + "Timer"))
                {
                    if (SetWaitableTimer(handle, ref waketime, 0, IntPtr.Zero, IntPtr.Zero, true))
                    {
                        using (EventWaitHandle wh = new EventWaitHandle(false,
                                                               EventResetMode.AutoReset))
                        {
                            wh.SafeWaitHandle = handle;
                            wh.WaitOne();
                        }
                    }
                    else
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error scheduling wake timer", ex);
            }
        }
    }
}
