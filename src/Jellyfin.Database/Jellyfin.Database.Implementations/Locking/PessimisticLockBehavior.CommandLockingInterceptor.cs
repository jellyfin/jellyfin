using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

public partial class PessimisticLockBehavior
{
    private sealed class CommandLockingInterceptor : DbCommandInterceptor
    {
        private readonly ILogger _logger;

        public CommandLockingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            using (DbLock.EnterWrite(_logger, command.Transaction, command))
            {
                return InterceptionResult<int>.SuppressWithResult(command.ExecuteNonQuery());
            }
        }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await using (await DbLock.EnterWriteAsync(_logger, command.Transaction, command, cancellationToken).ConfigureAwait(false))
            {
                return InterceptionResult<int>.SuppressWithResult(await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false));
            }
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            using (EnterReadOrWrite(command, eventData))
            {
                return InterceptionResult<object>.SuppressWithResult(command.ExecuteScalar()!);
            }
        }

        public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            await using (await EnterReadOrWriteAsync(command, eventData, cancellationToken).ConfigureAwait(false))
            {
                return InterceptionResult<object>.SuppressWithResult((await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
            }
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            using (EnterReadOrWrite(command, eventData))
            {
                return InterceptionResult<DbDataReader>.SuppressWithResult(command.ExecuteReader());
            }
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            await using (await EnterReadOrWriteAsync(command, eventData, cancellationToken).ConfigureAwait(false))
            {
                return InterceptionResult<DbDataReader>.SuppressWithResult(await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false));
            }
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        private DbLock EnterReadOrWrite(DbCommand command, CommandEventData eventData)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            // SQLite SaveChanges can use reader/scalar commands for generated values. Those commands
            // are still part of the write pipeline, so they must wait behind an earlier writer request.
            return eventData.CommandSource == CommandSource.SaveChanges
                ? DbLock.EnterWrite(_logger, command.Transaction, command)
                : DbLock.EnterRead(_logger, command.Transaction);
        }

        private ValueTask<DbLock> EnterReadOrWriteAsync(DbCommand command, CommandEventData eventData, CancellationToken cancellationToken)
        {
            return eventData.CommandSource == CommandSource.SaveChanges
                ? DbLock.EnterWriteAsync(_logger, command.Transaction, command, cancellationToken)
                : DbLock.EnterReadAsync(_logger, command.Transaction, cancellationToken);
        }
    }
}
