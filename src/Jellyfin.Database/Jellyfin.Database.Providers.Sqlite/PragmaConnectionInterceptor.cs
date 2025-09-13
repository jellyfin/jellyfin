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
    /// <summary>
    /// Initializes a new instance of the <see cref="PragmaConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="cacheSize">Cache size.</param>
    /// <param name="lockingMode">Locking mode.</param>
    /// <param name="journalSizeLimit">Journal Size.</param>
    /// <param name="pageSize">Page Size.</param>
    public PragmaConnectionInterceptor(int? cacheSize, string lockingMode, int? journalSizeLimit, int? pageSize)
    {
        CacheSize = cacheSize;
        LockingMode = lockingMode;
        JournalSizeLimit = journalSizeLimit;
        PageSize = pageSize;
    }

    private int? CacheSize { get; }

    private string LockingMode { get; }

    private int? JournalSizeLimit { get; }

    private int? PageSize { get; }

    private string? InitialCommand { get; set; }

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);

        using (var command = connection.CreateCommand())
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = InitialCommand ??= BuildCommandText();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            command.ExecuteNonQuery();
        }
    }

    private string BuildCommandText()
    {
        var sb = new StringBuilder();
        if (CacheSize.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA cache_size={CacheSize.Value};");
        }

        if (!string.IsNullOrWhiteSpace(LockingMode))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA locking_mode={LockingMode};");
        }

        if (JournalSizeLimit.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA journal_size_limit={JournalSizeLimit};");
        }

        if (PageSize.HasValue)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA page_size={JournalSizeLimit};");
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA synchronous=1;");
        sb.AppendLine(CultureInfo.InvariantCulture, $"PRAGMA temp_store=2;");

        return sb.ToString();
    }
}
