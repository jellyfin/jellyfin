using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Refuses a server that is not supported, and sets the isolation level of the session to read committed, every time a
/// connection is opened.
/// </summary>
/// <remarks>
/// <para>
/// A connection that comes out of the pool of SqlClient keeps the isolation level its last user set, for a transaction or
/// with <c>SET TRANSACTION ISOLATION LEVEL</c>, and a statement outside a transaction runs at that level. So the level is
/// set again on every open. A transaction that names no level needs nothing of the provider: SqlClient names read
/// committed for it, also when an earlier transaction of the same open connection left the session at another level.
/// </para>
/// <para>
/// The version is the one the server reports when the connection is made. The edition of the engine is asked once for
/// each server in the process, as Azure SQL Database reports version 12 whatever it runs, and the version Azure SQL
/// Managed Instance reports depends on how it is updated. The edition of a server never changes, so it is kept by the
/// name of the server.
/// </para>
/// </remarks>
internal sealed class SessionSettingsInterceptor : DbConnectionInterceptor
{
    /// <summary>
    /// The statement that sets the session up.
    /// </summary>
    internal const string Statement = "SET TRANSACTION ISOLATION LEVEL READ COMMITTED";

    /// <summary>
    /// The query for the edition of the engine.
    /// </summary>
    internal const string EditionQuery = "SELECT CONVERT(int, SERVERPROPERTY('EngineEdition'))";

    /// <summary>
    /// The oldest supported major version, that of SQL Server 2022.
    /// </summary>
    internal const int OldestSupportedMajorVersion = 16;

    // The editions of the engine that are services of Azure, not a SQL Server that Jellyfin is tested on: Azure SQL
    // Database, the dedicated SQL pool of Azure Synapse, Azure SQL Managed Instance, and the serverless SQL pool of Azure
    // Synapse.
    private const int AzureSqlDatabase = 5;
    private const int AzureSynapseDedicatedPool = 6;
    private const int AzureSqlManagedInstance = 8;
    private const int AzureSynapseServerlessPool = 11;

    private static readonly ConcurrentDictionary<string, int?> _editions = new(StringComparer.Ordinal);

    private SessionSettingsInterceptor()
    {
    }

    /// <summary>
    /// What a server is to Jellyfin.
    /// </summary>
    internal enum ServerSupport
    {
        /// <summary>A supported SQL Server.</summary>
        Supported,

        /// <summary>A SQL Server older than the oldest supported version.</summary>
        TooOld,

        /// <summary>A service of Azure, which is not supported: Azure SQL Database, Azure SQL Managed Instance or Azure Synapse.</summary>
        Unsupported,

        /// <summary>A server whose version cannot be read.</summary>
        Unreadable
    }

    /// <summary>
    /// Gets the interceptor. Every context options object holds this one instance.
    /// </summary>
    public static SessionSettingsInterceptor Instance { get; } = new();

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var edition = _editions.GetOrAdd(connection.DataSource, _ => ReadEdition(connection));
        ThrowIfUnsupported(edition, connection.ServerVersion);
        using var command = connection.CreateCommand();
        command.CommandText = Statement;
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        if (!_editions.TryGetValue(connection.DataSource, out var edition))
        {
            edition = await ReadEditionAsync(connection, cancellationToken).ConfigureAwait(false);
            _editions.TryAdd(connection.DataSource, edition);
        }

        ThrowIfUnsupported(edition, connection.ServerVersion);
        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = Statement;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Decides whether Jellyfin supports a server.
    /// </summary>
    /// <param name="engineEdition">The edition of the engine, as <c>SERVERPROPERTY('EngineEdition')</c> gives it.</param>
    /// <param name="serverVersion">The version the server reports, for example <c>16.00.4295</c>.</param>
    /// <returns>Whether the server is supported, and why not.</returns>
    internal static ServerSupport Decide(int? engineEdition, string? serverVersion)
    {
        if (engineEdition is AzureSqlDatabase or AzureSynapseDedicatedPool or AzureSqlManagedInstance or AzureSynapseServerlessPool)
        {
            return ServerSupport.Unsupported;
        }

        var major = serverVersion?.Split('.', 2)[0];
        if (!int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var version))
        {
            return ServerSupport.Unreadable;
        }

        return version < OldestSupportedMajorVersion ? ServerSupport.TooOld : ServerSupport.Supported;
    }

    /// <summary>
    /// Throws when Jellyfin does not support a server.
    /// </summary>
    /// <param name="engineEdition">The edition of the engine.</param>
    /// <param name="serverVersion">The version the server reports.</param>
    internal static void ThrowIfUnsupported(int? engineEdition, string? serverVersion)
    {
        switch (Decide(engineEdition, serverVersion))
        {
            case ServerSupport.Unsupported:
                throw new InvalidOperationException(
                    "Azure SQL Database, Azure SQL Managed Instance and Azure Synapse are not supported: Jellyfin is not tested on them. Use SQL Server 2022 (16.0) or later.");
            case ServerSupport.TooOld:
                throw new InvalidOperationException($"SQL Server {serverVersion} is not supported. Jellyfin needs SQL Server 2022 (16.0) or later.");
            case ServerSupport.Unreadable:
                throw new InvalidOperationException($"The version '{serverVersion}' that the database server reports cannot be read, so it is not known whether it is supported.");
        }
    }

    private static int? ReadEdition(DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = EditionQuery;
        return command.ExecuteScalar() is int edition ? edition : null;
    }

    private static async Task<int?> ReadEditionAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = EditionQuery;
            return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is int edition ? edition : null;
        }
    }
}
