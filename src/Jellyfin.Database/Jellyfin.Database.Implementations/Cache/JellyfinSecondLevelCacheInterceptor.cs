using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Cache;

/// <summary>
/// Adapted from <see href="https://github.com/VahidN/EFCoreSecondLevelCacheInterceptor/blob/master/src/EFCoreSecondLevelCacheInterceptor/SecondLevelCacheInterceptor.cs"/>
/// with read/write lock.
/// </summary>
public class JellyfinSecondLevelCacheInterceptor : DbCommandInterceptor
{
    private static readonly ReaderWriterLockSlim _databaseLock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly IDbCommandInterceptorProcessor _processor;
    private readonly ILogger<JellyfinSecondLevelCacheInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinSecondLevelCacheInterceptor"/> class.
    /// </summary>
    /// <param name="processor">The processor.</param>
    /// <param name="logger">The logger.</param>
    public JellyfinSecondLevelCacheInterceptor(
        IDbCommandInterceptorProcessor processor,
        ILogger<JellyfinSecondLevelCacheInterceptor> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        using (DbLock.EnterWrite(_logger))
        {
            return _processor.ProcessExecutedCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterWrite(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutedCommands(command, eventData?.Context, result));
        }
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        using (DbLock.EnterWrite(_logger))
        {
            return _processor.ProcessExecutingCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterWrite(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutingCommands(command, eventData?.Context, result));
        }
    }

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        using (DbLock.EnterRead(_logger))
        {
            return _processor.ProcessExecutedCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterRead(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutedCommands(command, eventData?.Context, result));
        }
    }

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        using (DbLock.EnterRead(_logger))
        {
            return _processor.ProcessExecutingCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterRead(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutingCommands(command, eventData?.Context, result));
        }
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        using (DbLock.EnterRead(_logger))
        {
            return _processor.ProcessExecutedCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterRead(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutedCommands(command, eventData?.Context, result));
        }
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        using (DbLock.EnterRead(_logger))
        {
            return _processor.ProcessExecutingCommands(command, eventData?.Context, result);
        }
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        using (DbLock.EnterRead(_logger))
        {
            return ValueTask.FromResult(_processor.ProcessExecutingCommands(command, eventData?.Context, result));
        }
    }

#pragma warning disable MT1013 // Releasing lock without guarantee of execution
#pragma warning disable MT1012 // Acquiring lock without guarantee of releasing
    private sealed class DbLock : IDisposable
    {
        private readonly Action? _action;
        private bool _disposed;

        private static readonly IDisposable _noLock = new DbLock(null) { _disposed = true };
        private static (string Command, Guid Id, DateTimeOffset QueryDate, bool Printed) _blockQuery;

        private DbLock(Action? action = null)
        {
            _action = action;
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterWrite(ILogger logger, IDbCommand? command = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Write for {Caller}:{Line}", callerMemberName, callerNo);
            if (_databaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held {Caller}:{Line}", callerMemberName, callerNo);
                return _noLock;
            }

            BeginWriteLock(logger, command, callerMemberName, callerNo);
            return new DbLock(() =>
            {
                EndWriteLock(logger, callerMemberName, callerNo);
            });
        }

#pragma warning disable IDISP015 // Member should not return created and cached instance
        public static IDisposable EnterRead(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
#pragma warning restore IDISP015 // Member should not return created and cached instance
        {
            logger.LogTrace("Enter Read {Caller}:{Line}", callerMemberName, callerNo);
            if (_databaseLock.IsWriteLockHeld)
            {
                logger.LogTrace("Write Held {Caller}:{Line}", callerMemberName, callerNo);
                return _noLock;
            }

            BeginReadLock(logger, callerMemberName, callerNo);
            return new DbLock(() =>
            {
                ExitReadLock(logger, callerMemberName, callerNo);
            });
        }

        private static void BeginWriteLock(ILogger logger, IDbCommand? command = null, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Acquire Write {Caller}:{Line}", callerMemberName, callerNo);
            if (!_databaseLock.TryEnterWriteLock(TimeSpan.FromMilliseconds(1000)))
            {
                var blockingQuery = _blockQuery;
                if (!blockingQuery.Printed)
                {
                    _blockQuery = (blockingQuery.Command, blockingQuery.Id, blockingQuery.QueryDate, true);
                    logger.LogInformation("QueryLock: {Id} --- {Query}", blockingQuery.Id, blockingQuery.Command);
                }

                logger.LogInformation("Query congestion detected: '{Id}' since '{Date}'", blockingQuery.Id, blockingQuery.QueryDate);

                _databaseLock.EnterWriteLock();

                logger.LogInformation("Query congestion cleared: '{Id}' for '{Date}'", blockingQuery.Id, DateTimeOffset.Now - blockingQuery.QueryDate);
            }

            _blockQuery = (command?.CommandText ?? "Transaction", Guid.NewGuid(), DateTimeOffset.Now, false);

            logger.LogTrace("Write Aquired {Caller}:{Line}", callerMemberName, callerNo);
        }

        private static void BeginReadLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Aquire Write {Caller}:{Line}", callerMemberName, callerNo);
            _databaseLock.EnterReadLock();
            logger.LogTrace("Read Aquired {Caller}:{Line}", callerMemberName, callerNo);
        }

        private static void EndWriteLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Release Write {Caller}:{Line}", callerMemberName, callerNo);
            _databaseLock.ExitWriteLock();
        }

        private static void ExitReadLock(ILogger logger, [CallerMemberName] string? callerMemberName = null, [CallerLineNumber] int? callerNo = null)
        {
            logger.LogTrace("Release Read {Caller}:{Line}", callerMemberName, callerNo);
            _databaseLock.ExitReadLock();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _action?.Invoke();
        }
    }
#pragma warning restore MT1013 // Releasing lock without guarantee of execution
#pragma warning restore MT1012 // Acquiring lock without guarantee of releasing
}
