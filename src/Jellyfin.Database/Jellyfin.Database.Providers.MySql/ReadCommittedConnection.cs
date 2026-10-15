using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Microting.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Microting.EntityFrameworkCore.MySql.Storage.Internal;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// The connection of EF Core for MySQL and MariaDB, which starts a transaction that names no isolation level at read
/// committed.
/// </summary>
/// <remarks>
/// <para>
/// MySqlConnector starts a transaction that names no level at repeatable read, the default of InnoDB, and it sets the
/// level of the session to do it. So the level that <see cref="SessionSettingsInterceptor"/> gives the session when it
/// opens holds neither in such a transaction nor after it. EF Core starts its own transactions, for a save and for a
/// migration, without a level, and so does the code of Jellyfin.
/// </para>
/// <para>
/// The level is chosen here, before EF Core tells the interceptors that a transaction is starting, because they all see
/// the level of that event and cannot change it. The optimistic locking behaviour starts the transaction itself with
/// that level, and runs after the interceptors of the provider, so an interceptor of the provider could neither change
/// the level it uses nor start the transaction in its place. A level that is named is kept.
/// </para>
/// </remarks>
#pragma warning disable EF1001 // The connection of the provider and its dependencies are internal and have no public counterpart.
internal sealed class ReadCommittedConnection : MySqlRelationalConnection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadCommittedConnection"/> class.
    /// </summary>
    /// <param name="dependencies">The dependencies of a relational connection.</param>
    /// <param name="mySqlConnectionStringOptionsValidator">The validator of the connection string options.</param>
    /// <param name="mySqlSingletonOptions">The options of the provider.</param>
    public ReadCommittedConnection(
        RelationalConnectionDependencies dependencies,
        IMySqlConnectionStringOptionsValidator mySqlConnectionStringOptionsValidator,
        IMySqlOptions mySqlSingletonOptions)
        : base(dependencies, mySqlConnectionStringOptionsValidator, mySqlSingletonOptions)
    {
    }

    /// <inheritdoc/>
    public override IDbContextTransaction BeginTransaction(IsolationLevel isolationLevel)
        => base.BeginTransaction(Resolve(isolationLevel));

    /// <inheritdoc/>
    public override Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        => base.BeginTransactionAsync(Resolve(isolationLevel), cancellationToken);

    /// <summary>
    /// Gets the level a transaction is started at.
    /// </summary>
    /// <param name="isolationLevel">The level that was asked for.</param>
    /// <returns>Read committed when no level was named, otherwise the level that was.</returns>
    internal static IsolationLevel Resolve(IsolationLevel isolationLevel)
        => isolationLevel == IsolationLevel.Unspecified ? IsolationLevel.ReadCommitted : isolationLevel;
}
#pragma warning restore EF1001
