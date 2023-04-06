using AsyncKeyedLock;

namespace MediaBrowser.Common.Concurrency
{
    /// <summary>
    /// Class AsyncKeyedLock.
    /// </summary>
    public static class AsyncKeyedLock
    {
        /// <summary>
        /// Gets the AsyncKeyedLock.
        /// </summary>
        public static AsyncKeyedLocker<string> Locker => new(o =>
        {
            o.PoolSize = 20;
            o.PoolInitialFill = 1;
        });
    }
}
