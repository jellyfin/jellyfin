using MediaBrowser.Model.Logging;
using System;
using System.Data;
using System.Threading;

namespace MediaBrowser.Server.Implementations.Persistence
{
    class SqliteShrinkMemoryTimer : IDisposable
    {
        private Timer _shrinkMemoryTimer;

        private readonly SemaphoreSlim _writeLock;
        private readonly ILogger _logger;
        private readonly IDbConnection _connection;

        public SqliteShrinkMemoryTimer(IDbConnection connection, SemaphoreSlim writeLock, ILogger logger)
        {
            _connection = connection;
            _writeLock = writeLock;
            _logger = logger;

            _shrinkMemoryTimer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(10));
        }

        private async void TimerCallback(object state)
        {
            await _writeLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = "pragma shrink_memory";
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                _logger.ErrorException("Failed to save items:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                _writeLock.Release();
            }
        }

        public void Dispose()
        {
            if (_shrinkMemoryTimer != null)
            {
                _shrinkMemoryTimer.Dispose();
                _shrinkMemoryTimer = null;
            }
        }
    }
}
