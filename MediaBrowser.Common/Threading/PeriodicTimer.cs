using System;
using System.Threading;
using Microsoft.Win32;

namespace MediaBrowser.Common.Threading
{
    public class PeriodicTimer : IDisposable
    {
        public Action<object> Callback { get; set; }
        private Timer _timer;
        private readonly object _state;
        private readonly object _timerLock = new object();
        private readonly TimeSpan _period;

        public PeriodicTimer(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            Callback = callback;
            _period = period;
            _state = state;

            StartTimer(dueTime);
        }

        void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                DisposeTimer();
                StartTimer(Timeout.InfiniteTimeSpan);
            }
        }

        private void TimerCallback(object state)
        {
            Callback(state);
        }

        private void StartTimer(TimeSpan dueTime)
        {
            lock (_timerLock)
            {
                _timer = new Timer(TimerCallback, _state, dueTime, _period);

                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            }
        }

        private void DisposeTimer()
        {
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            
            lock (_timerLock)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        public void Dispose()
        {
            DisposeTimer();
        }
    }
}
