using MediaBrowser.Common.Kernel;
using System;

namespace MediaBrowser.Common.Logging
{
    public abstract class BaseLogger : IDisposable
    {
        public abstract void Initialize(IKernel kernel);
        public abstract void LogEntry(LogRow row);

        public virtual void Dispose()
        {
            Logger.LogInfo("Disposing " + GetType().Name);
        }
    }
}
