using System;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.Threading
{
    public class TimerFactory : ITimerFactory
    {
        public ITimer Create(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            return new CommonTimer(callback, state, dueTime, period);
        }

        public ITimer Create(Action<object> callback, object state, int dueTimeMs, int periodMs)
        {
            return new CommonTimer(callback, state, dueTimeMs, periodMs);
        }
    }
}
