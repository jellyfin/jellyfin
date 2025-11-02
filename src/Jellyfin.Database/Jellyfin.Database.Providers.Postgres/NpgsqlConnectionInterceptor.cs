using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Postgres;

/// <summary>
/// Connection interceptor for Npgsql to handle specific PostgreSQL connection behaviors.
/// </summary>
public class NpgsqlConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ILogger _logger;
    private readonly int? _lockTimeout;
    private readonly bool? _prepare;
    private readonly int? _commandTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="NpgsqlConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lockTimeout">The lock timeout.</param>
    /// <param name="prepare">Whether to prepare.</param>
    /// <param name="commandTimeout">The command timeout.</param>
    public NpgsqlConnectionInterceptor(
        ILogger logger,
        int? lockTimeout = null,
        bool? prepare = null,
        int? commandTimeout = null)
    {
        _logger = logger;
        _lockTimeout = lockTimeout;
        _prepare = prepare;
        _commandTimeout = commandTimeout;
    }

    /// <inheritdoc/>
    public override void ConnectionOpened(
        DbConnection connection,
        ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);

        _logger.LogDebug("PostgreSQL connection opened");

        // Apply connection settings
        if (connection is not Npgsql.NpgsqlConnection npgsqlConnection)
        {
            return;
        }

        ApplyConnectionSettings(npgsqlConnection);
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("PostgreSQL connection opened asynchronously");

        if (connection is not Npgsql.NpgsqlConnection npgsqlConnection)
        {
            return;
        }

        ApplyConnectionSettings(npgsqlConnection);
    }

    private void ApplyConnectionSettings(Npgsql.NpgsqlConnection connection)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Applying PostgreSQL connection settings");
        }

        // Set lock timeout if provided
        if (_lockTimeout.HasValue)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET lock_timeout = {_lockTimeout.Value}ms";
                cmd.ExecuteNonQuery();
                _logger.LogDebug("Set lock_timeout to {LockTimeout}ms", _lockTimeout.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to set lock_timeout: {Exception}", ex.Message);
            }
        }

        // Set command timeout if provided
        if (_commandTimeout.HasValue)
        {
            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = $"SET statement_timeout = {_commandTimeout.Value}ms";
                cmd.ExecuteNonQuery();
                _logger.LogDebug("Set statement_timeout to {CommandTimeout}ms", _commandTimeout.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to set statement_timeout: {Exception}", ex.Message);
            }
        }
    }
}
