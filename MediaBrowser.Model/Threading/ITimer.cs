using System;

namespace MediaBrowser.Model.Threading
{
    public interface ITimer : IDisposable
    {
        void Change(TimeSpan dueTime, TimeSpan period);
        void Change(int dueTimeMs, int periodMs);
    }
}
