using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Threading;

namespace Emby.Common.Implementations.Threading
{
    public class CommonTimer : ITimer
    {
        private readonly Timer _timer;

        public CommonTimer(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTime, period);
        }

        public CommonTimer(Action<object> callback, object state, int dueTimeMs, int periodMs)
        {
            _timer = new Timer(new TimerCallback(callback), state, dueTimeMs, periodMs);
        }

        public void Change(TimeSpan dueTime, TimeSpan period)
        {
            _timer.Change(dueTime, period);
        }

        public void Change(int dueTimeMs, int periodMs)
        {
            _timer.Change(dueTimeMs, periodMs);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
