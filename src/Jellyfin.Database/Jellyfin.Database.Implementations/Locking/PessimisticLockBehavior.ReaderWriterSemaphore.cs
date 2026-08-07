#pragma warning disable S2222 // Resource semaphore ownership is returned to callers and released by DbLock disposal.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Database.Implementations.Locking;

public partial class PessimisticLockBehavior
{
    private sealed partial class DbLock
    {
        // Writer-priority reader/writer lock built from semaphores. SemaphoreSlim has no thread
        // affinity, so async continuations can release the lock on whichever thread resumes them.
        private sealed class ReaderWriterSemaphore : IDisposable
        {
            // A waiting writer holds the turnstile so later readers cannot jump ahead of it.
            private readonly SemaphoreSlim _turnstileLock = new(1, 1);
            // The shared resource gate is held by one writer or by the first active reader.
            private readonly SemaphoreSlim _resourceLock = new(1, 1);
            // Protects the reader count and first-reader/last-reader transitions.
            private readonly SemaphoreSlim _readerLock = new(1, 1);
            private int _readers;

            public bool AcquireWrite(TimeSpan timeout, Action onTimeout)
            {
                _turnstileLock.Wait();
                try
                {
                    if (_resourceLock.Wait(timeout))
                    {
                        return true;
                    }

                    onTimeout();
                    _resourceLock.Wait();
                    return false;
                }
                finally
                {
                    _turnstileLock.Release();
                }
            }

            public async ValueTask<bool> AcquireWriteAsync(TimeSpan timeout, Action onTimeout, CancellationToken cancellationToken)
            {
                await _turnstileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (await _resourceLock.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
                    {
                        return true;
                    }

                    onTimeout();
                    await _resourceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return false;
                }
                finally
                {
                    _turnstileLock.Release();
                }
            }

            public void ExitWrite()
            {
                _resourceLock.Release();
            }

            public void AcquireRead()
            {
                _turnstileLock.Wait();
                _turnstileLock.Release();
                _readerLock.Wait();
                try
                {
                    _readers++;
                    if (_readers == 1)
                    {
                        _resourceLock.Wait();
                    }
                }
                catch
                {
                    _readers--;
                    throw;
                }
                finally
                {
                    _readerLock.Release();
                }
            }

            public async ValueTask AcquireReadAsync(CancellationToken cancellationToken)
            {
                await _turnstileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                _turnstileLock.Release();
                await _readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    _readers++;
                    if (_readers == 1)
                    {
                        await _resourceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch
                {
                    _readers--;
                    throw;
                }
                finally
                {
                    _readerLock.Release();
                }
            }

            public void ExitRead()
            {
                _readerLock.Wait();
                try
                {
                    _readers--;
                    if (_readers == 0)
                    {
                        _resourceLock.Release();
                    }
                }
                finally
                {
                    _readerLock.Release();
                }
            }

            public void Dispose()
            {
                _turnstileLock.Dispose();
                _resourceLock.Dispose();
                _readerLock.Dispose();
            }
        }
    }
}

#pragma warning restore S2222
