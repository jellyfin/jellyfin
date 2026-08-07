using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// A locking behavior that will always block any operation while a write is requested. Mimics the old SqliteRepository behavior.
/// </summary>
public partial class PessimisticLockBehavior : IEntityFrameworkCoreLockingBehavior
{
    private readonly ILogger<PessimisticLockBehavior> _logger;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PessimisticLockBehavior"/> class.
    /// </summary>
    /// <param name="logger">The application logger.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public PessimisticLockBehavior(ILogger<PessimisticLockBehavior> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        // The EF command and transaction interceptors own the lock. Wrapping SaveChanges here would
        // hold one outer lock across EF's command pipeline and would miss provider-specific commands.
        saveChanges();
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
        _logger.LogInformation("The database locking mode has been set to: Pessimistic.");
        // Commit, rollback, and failure are covered by transaction interceptors; dispose-only
        // transactions are only visible through EF's diagnostic listener.
        DbLock.EnsureTransactionDisposedListener();
        optionsBuilder.AddInterceptors(new CommandLockingInterceptor(_loggerFactory.CreateLogger<CommandLockingInterceptor>()));
        optionsBuilder.AddInterceptors(new TransactionLockingInterceptor(_loggerFactory.CreateLogger<TransactionLockingInterceptor>()));
    }

    /// <inheritdoc/>
    public async Task OnSaveChangesAsync(JellyfinDbContext context, Func<Task> saveChanges)
    {
        // Keep async continuations outside a thread-affine outer lock. Individual commands still
        // acquire the global pessimistic lock below.
        await saveChanges().ConfigureAwait(false);
    }
}
