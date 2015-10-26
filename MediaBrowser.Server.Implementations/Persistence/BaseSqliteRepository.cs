using MediaBrowser.Model.Logging;
using System;
using System.Threading;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public abstract class BaseSqliteRepository : IDisposable
    {
        protected readonly SemaphoreSlim WriteLock = new SemaphoreSlim(1, 1);
        protected ILogger Logger;

        protected BaseSqliteRepository(ILogManager logManager)
        {
            Logger = logManager.GetLogger(GetType().Name);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
                    {
                        WriteLock.Wait();

                        CloseConnection();
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error disposing database", ex);
                }
            }
        }

        protected abstract void CloseConnection();
    }
}
