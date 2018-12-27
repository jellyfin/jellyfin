using System;

namespace MediaBrowser.Model.Threading
{
    public interface ITimerFactory
    {
        ITimer Create(Action<object> callback, object state, TimeSpan dueTime, TimeSpan period);
        ITimer Create(Action<object> callback, object state, int dueTimeMs, int periodMs);
    }
}
