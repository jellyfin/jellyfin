using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

public partial class PessimisticLockBehavior
{
    private sealed class TransactionLockingInterceptor : DbTransactionInterceptor
    {
        private readonly ILogger _logger;

        public TransactionLockingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
        {
            // Transactions span multiple commands, so the transaction lifetime, not each command,
            // owns the write lock once the transaction starts.
            return DbLock.BeginTransaction(_logger, connection, eventData, result);
        }

        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection, TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken cancellationToken = default)
        {
            return await DbLock.BeginTransactionAsync(_logger, connection, eventData, result, cancellationToken).ConfigureAwait(false);
        }

        public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        {
            DbLock.EndTransactionLock(transaction);

            base.TransactionCommitted(transaction, eventData);
        }

        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await DbLock.EndTransactionLockAsync(transaction).ConfigureAwait(false);

            await base.TransactionCommittedAsync(transaction, eventData, cancellationToken).ConfigureAwait(false);
        }

        public override void TransactionFailed(DbTransaction transaction, TransactionErrorEventData eventData)
        {
            DbLock.EndTransactionLock(transaction);

            base.TransactionFailed(transaction, eventData);
        }

        public override async Task TransactionFailedAsync(DbTransaction transaction, TransactionErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            await DbLock.EndTransactionLockAsync(transaction).ConfigureAwait(false);

            await base.TransactionFailedAsync(transaction, eventData, cancellationToken).ConfigureAwait(false);
        }

        public override void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        {
            DbLock.EndTransactionLock(transaction);

            base.TransactionRolledBack(transaction, eventData);
        }

        public override async Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            await DbLock.EndTransactionLockAsync(transaction).ConfigureAwait(false);

            await base.TransactionRolledBackAsync(transaction, eventData, cancellationToken).ConfigureAwait(false);
        }
    }
}
