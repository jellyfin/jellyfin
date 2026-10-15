using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Checks the version of the server and sets the session state that the queries and the data of Jellyfin rely on,
/// every time a connection is opened.
/// </summary>
/// <remarks>
/// <para>
/// A connection that comes out of the pool is reset to the defaults of the server, so the settings have to be applied
/// on every open and cannot be stored on the server for the session.
/// </para>
/// <para>
/// The SQL mode is strict for every table, so that a value that does not fit is rejected instead of cut off, with no
/// engine substitution. Division by zero is an error. It does not hold <c>NO_BACKSLASH_ESCAPES</c>, because the
/// backslash is the escape character of every <c>LIKE</c> pattern, and it does not hold <c>NO_AUTO_VALUE_ON_ZERO</c>,
/// which would turn an id of zero into a value. <c>ONLY_FULL_GROUP_BY</c> and the zero date modes of MySQL are not set
/// either, so that both servers run with one mode whatever their defaults are. Isolation is read committed, as on
/// PostgreSQL, and the time zone of the session is UTC so that server side date functions do not depend on the server.
/// </para>
/// <para>
/// MySqlConnector sets the isolation level of the session again for every transaction it starts, so the level set here
/// holds for statements outside a transaction. <see cref="ReadCommittedConnection"/> keeps it for the transactions.
/// </para>
/// </remarks>
internal sealed class SessionSettingsInterceptor : DbConnectionInterceptor
{
    internal const string Statement =
        "SET SESSION transaction_isolation='READ-COMMITTED', "
        + "sql_mode='STRICT_ALL_TABLES,NO_ENGINE_SUBSTITUTION,ERROR_FOR_DIVISION_BY_ZERO', "
        + "time_zone='+00:00'";

    private readonly bool _refuseUnreadableVersion;

    private SessionSettingsInterceptor(bool refuseUnreadableVersion)
    {
        _refuseUnreadableVersion = refuseUnreadableVersion;
    }

    /// <summary>
    /// Gets the interceptor for a server that was not named in the configuration. It refuses a server whose version it
    /// cannot read, as it then cannot tell whether the server is supported. Every context options object that does not
    /// name its server holds this one instance, so that EF Core builds one internal service provider for them.
    /// </summary>
    public static SessionSettingsInterceptor Instance { get; } = new(refuseUnreadableVersion: true);

    /// <summary>
    /// Gets the interceptor for a server that was named in the configuration, which is trusted when the version it
    /// reports cannot be read, as a proxy or a fork may report its own.
    /// </summary>
    public static SessionSettingsInterceptor ForConfiguredServer { get; } = new(refuseUnreadableVersion: false);

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ThrowIfUnsupported(connection);
        using var command = connection.CreateCommand();
        command.CommandText = Statement;
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        ThrowIfUnsupported(connection);
        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
            command.CommandText = Statement;
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void ThrowIfUnsupported(DbConnection connection)
    {
        if (!ServerInfo.TryParseReported(connection.ServerVersion, out var server))
        {
            if (_refuseUnreadableVersion)
            {
                throw new InvalidOperationException(ServerInfo.UnreadableMessage(connection.ServerVersion));
            }

            return;
        }

        if (!server!.MeetsFloor)
        {
            throw new InvalidOperationException(server.UnsupportedMessage);
        }
    }
}
