using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class NamedLock
    /// </summary>
    public class NamedLock : IDisposable
    {
        /// <summary>
        /// The _locks
        /// </summary>
        private readonly Dictionary<string, SemaphoreSlim> _locks = new Dictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Waits the async.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task.</returns>
        public Task WaitAsync(string name)
        {
            return GetLock(name).WaitAsync();
        }

        /// <summary>
        /// Releases the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        public void Release(string name)
        {
            SemaphoreSlim semaphore;

            if (_locks.TryGetValue(name, out semaphore))
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the lock.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.Object.</returns>
        private SemaphoreSlim GetLock(string filename)
        {
            SemaphoreSlim fileLock;
            lock (_locks)
            {
                if (!_locks.TryGetValue(filename, out fileLock))
                {
                    fileLock = new SemaphoreSlim(1,1);
                    _locks[filename] = fileLock;
                }
            }
            return fileLock;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                DisposeLocks();
            }
        }

        /// <summary>
        /// Disposes the locks.
        /// </summary>
        private void DisposeLocks()
        {
            lock (_locks)
            {
                foreach (var semaphore in _locks.Values)
                {
                    semaphore.Dispose();
                }

                _locks.Clear();
            }
        }
    }
}
