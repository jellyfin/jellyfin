using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// Injects a series of PRAGMA on each connection starts.
/// </summary>
public class PragmaConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ILogger _logger;
    private readonly int? _cacheSize;
    private readonly string _lockingMode;
    private readonly int? _journalSizeLimit;
    private readonly int _tempStoreMode;
    private readonly int _syncMode;
    private readonly IDictionary<string, string> _customPragma;

    /// <summary>
    /// Initializes a new instance of the <see cref="PragmaConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheSize">Cache size.</param>
    /// <param name="lockingMode">Locking mode.</param>
    /// <param name="journalSizeLimit">Journal Size.</param>
    /// <param name="tempStoreMode">The https://sqlite.org/pragma.html#pragma_temp_store pragma.</param>
    /// <param name="syncMode">The https://sqlite.org/pragma.html#pragma_synchronous pragma.</param>
    /// <param name="customPragma">A list of custom provided Pragma in the list of CustomOptions starting with "#PRAGMA:".</param>
    public PragmaConnectionInterceptor(ILogger logger, int? cacheSize, string lockingMode, int? journalSizeLimit, int tempStoreMode, int syncMode, IDictionary<string, string> customPragma)
    {
        _logger = logger;
        _cacheSize = cacheSize;
        _lockingMode = lockingMode;
        _journalSizeLimit = journalSizeLimit;
        _tempStoreMode = tempStoreMode;
        _syncMode = syncMode;
        _customPragma = customPragma;

        InitialCommand = BuildCommandText();
        _logger.LogInformation("SQLITE connection pragma command set to: \r\n{PragmaCommand}", InitialCommand);
    }

    private string? InitialCommand { get; set; }

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);

        using (var command = connection.CreateCommand())
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = InitialCommand;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            command.ExecuteNonQuery();
        }
    }

    /// <inheritdoc/>
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = InitialCommand;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private string BuildCommandText()
    {
        var sb = new StringBuilder();
        if (_cacheSize.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA cache_size={_cacheSize.Value};");
        }

        if (!string.IsNullOrWhiteSpace(_lockingMode))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA locking_mode={_lockingMode};");
        }

        if (_journalSizeLimit.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA journal_size_limit={_journalSizeLimit};");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA synchronous={_syncMode};");
        sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA temp_store={_tempStoreMode};");

        foreach (var item in _customPragma)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA {item.Key}={item.Value};");
        }

        return sb.ToString();
    }
}
