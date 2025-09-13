using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// Injects a series of PRAGMA on each connection starts.
/// </summary>
public class PragmaConnectionInterceptor : DbConnectionInterceptor
{
    private readonly int? _cacheSize;
    private readonly string _lockingMode;
    private readonly int? _journalSizeLimit;
    private readonly int? _pageSize;
    private readonly int _tempStoreMode;
    private readonly int _syncMode;
    private readonly IDictionary<string, string> _customPragma;

    /// <summary>
    /// Initializes a new instance of the <see cref="PragmaConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="cacheSize">Cache size.</param>
    /// <param name="lockingMode">Locking mode.</param>
    /// <param name="journalSizeLimit">Journal Size.</param>
    /// <param name="pageSize">Page Size.</param>
    /// <param name="tempStoreMode">The https://sqlite.org/pragma.html#pragma_temp_store pragma.</param>
    /// <param name="syncMode">The https://sqlite.org/pragma.html#pragma_synchronous pragma.</param>
    /// <param name="customPragma">A list of custom provided Pragma in the list of CustomOptions starting with "#PRAGMA:".</param>
    public PragmaConnectionInterceptor(int? cacheSize, string lockingMode, int? journalSizeLimit, int? pageSize, int tempStoreMode, int syncMode, IDictionary<string, string> customPragma)
    {
        _cacheSize = cacheSize;
        _lockingMode = lockingMode;
        _journalSizeLimit = journalSizeLimit;
        _pageSize = pageSize;
        _tempStoreMode = tempStoreMode;
        _syncMode = syncMode;
        _customPragma = customPragma;

        InitialCommand ??= BuildCommandText();
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

        if (_pageSize.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA page_size={_pageSize};");
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
