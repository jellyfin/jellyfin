using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.Sqlite;

/// <summary>
/// Injects a series of PRAGMA on each connection open.
/// Supports optional extra PRAGMAs loaded from a well-known file and executes them per-connection (pool-safe).
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

    private readonly IReadOnlyList<string> _extraPragmaStatements;

    // Log the effective pragma values once per process start (avoid noisy logs)
    private static int _pragmaVerifyLogged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PragmaConnectionInterceptor"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="cacheSize">The PRAGMA cache_size value to apply (if any).</param>
    /// <param name="lockingMode">The PRAGMA locking_mode value to apply (if any).</param>
    /// <param name="journalSizeLimit">The PRAGMA journal_size_limit value to apply (if any).</param>
    /// <param name="tempStoreMode">The PRAGMA temp_store value to apply.</param>
    /// <param name="syncMode">The PRAGMA synchronous value to apply.</param>
    /// <param name="customPragma">Additional custom pragmas.</param>
    public PragmaConnectionInterceptor(
        ILogger logger,
        int? cacheSize,
        string lockingMode,
        int? journalSizeLimit,
        int tempStoreMode,
        int syncMode,
        IDictionary<string, string> customPragma)
    {
        _logger = logger;
        _cacheSize = cacheSize;
        _lockingMode = lockingMode;
        _journalSizeLimit = journalSizeLimit;
        _tempStoreMode = tempStoreMode;
        _syncMode = syncMode;
        _customPragma = customPragma;

        InitialCommand = BuildCommandText();

        // Load extra pragmas from env/file/auto-discovered default file. Executed per-connection (pool-safe).
        _extraPragmaStatements = LoadExtraPragmas(_logger);

        _logger.LogInformation("SQLITE connection pragma command set to: \r\n{PragmaCommand}", InitialCommand);

        if (_extraPragmaStatements.Count > 0)
        {
            _logger.LogInformation(
                "SQLITE extra pragma statements enabled ({Count}). Will execute on each connection open: {Pragmas}",
                _extraPragmaStatements.Count,
                string.Join(" ", _extraPragmaStatements));
        }
    }

    private string? InitialCommand { get; set; }

    /// <summary>
    /// Called when a database connection has been opened.
    /// </summary>
    /// <param name="connection">The opened connection.</param>
    /// <param name="eventData">Event data.</param>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);

        ExecuteInitialCommand(connection);
        ExecuteExtraPragmas(connection);
    }

    /// <summary>
    /// Called when a database connection has been opened (async).
    /// </summary>
    /// <param name="connection">The opened connection.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);

        await ExecuteInitialCommandAsync(connection, cancellationToken).ConfigureAwait(false);
        await ExecuteExtraPragmasAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private void ExecuteInitialCommand(DbConnection connection)
    {
        using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = InitialCommand;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
        command.ExecuteNonQuery();
    }

    private async Task ExecuteInitialCommandAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        await using (command.ConfigureAwait(false))
        {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = InitialCommand;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void ExecuteExtraPragmas(DbConnection connection)
    {
        if (_extraPragmaStatements.Count == 0)
        {
            return;
        }

        foreach (var stmt in _extraPragmaStatements)
        {
            using var cmd = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            cmd.CommandText = stmt;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            cmd.ExecuteNonQuery();
        }

        // Definitive proof: log effective values from the SAME connection after applying
        LogEffectivePragmasOnce(connection);
    }

    private async Task ExecuteExtraPragmasAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (_extraPragmaStatements.Count == 0)
        {
            return;
        }

        foreach (var stmt in _extraPragmaStatements)
        {
            var cmd = connection.CreateCommand();
            await using (cmd.ConfigureAwait(false))
            {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
                cmd.CommandText = stmt;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // Definitive proof: log effective values from the SAME connection after applying
        await LogEffectivePragmasOnceAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    private void LogEffectivePragmasOnce(DbConnection connection)
    {
        if (Interlocked.Exchange(ref _pragmaVerifyLogged, 1) != 0)
        {
            return;
        }

        try
        {
            string? Get(string pragma)
            {
                using var cmd = connection.CreateCommand();
#pragma warning disable CA2100
                cmd.CommandText = $"PRAGMA {pragma};";
#pragma warning restore CA2100
                var val = cmd.ExecuteScalar();
                return val?.ToString();
            }

            _logger.LogInformation(
                "SQLite PRAGMA verify (same Jellyfin connection, after apply): journal_mode={JournalMode}, synchronous={Synchronous}, temp_store={TempStore}, cache_size={CacheSize}, mmap_size={MmapSize}, cache_spill={CacheSpill}, threads={Threads}, wal_autocheckpoint={WalAutoCheckpoint}",
                Get("journal_mode"),
                Get("synchronous"),
                Get("temp_store"),
                Get("cache_size"),
                Get("mmap_size"),
                Get("cache_spill"),
                Get("threads"),
                Get("wal_autocheckpoint"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite PRAGMA verify failed");
        }
    }

    private async Task LogEffectivePragmasOnceAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _pragmaVerifyLogged, 1) != 0)
        {
            return;
        }

        try
        {
            async Task<string?> GetAsync(string pragma)
            {
                var cmd = connection.CreateCommand();
                await using (cmd.ConfigureAwait(false))
                {
#pragma warning disable CA2100
                    cmd.CommandText = $"PRAGMA {pragma};";
#pragma warning restore CA2100
                    var val = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    return val?.ToString();
                }
            }

            _logger.LogInformation(
                "SQLite PRAGMA verify (same Jellyfin connection, after apply): journal_mode={JournalMode}, synchronous={Synchronous}, temp_store={TempStore}, cache_size={CacheSize}, mmap_size={MmapSize}, cache_spill={CacheSpill}, threads={Threads}, wal_autocheckpoint={WalAutoCheckpoint}",
                await GetAsync("journal_mode").ConfigureAwait(false),
                await GetAsync("synchronous").ConfigureAwait(false),
                await GetAsync("temp_store").ConfigureAwait(false),
                await GetAsync("cache_size").ConfigureAwait(false),
                await GetAsync("mmap_size").ConfigureAwait(false),
                await GetAsync("cache_spill").ConfigureAwait(false),
                await GetAsync("threads").ConfigureAwait(false),
                await GetAsync("wal_autocheckpoint").ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SQLite PRAGMA verify failed");
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

    /// <summary>
    /// Loads extra PRAGMA statements from:
    /// - env var: JELLYFIN_SQLITE_PRAGMAS (semicolon separated)
    /// - env var: JELLYFIN_SQLITE_PRAGMAS_FILE (path to a file)
    /// - auto-discovered default files near the XML config directory and data directory
    /// </summary>
    private static IReadOnlyList<string> LoadExtraPragmas(ILogger logger)
    {
        // 1) Direct env var (highest priority)
        var raw = Environment.GetEnvironmentVariable("JELLYFIN_SQLITE_PRAGMAS");
        if (!string.IsNullOrWhiteSpace(raw))
        {
            logger.LogInformation("Loading SQLite extra PRAGMAs from env var JELLYFIN_SQLITE_PRAGMAS.");
            return ParsePragmaStatements(logger, raw);
        }

        // 2) Env var file path override
        var filePath = Environment.GetEnvironmentVariable("JELLYFIN_SQLITE_PRAGMAS_FILE");
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var fromFile = TryReadFile(logger, filePath, logWhenMissing: true);
            if (!string.IsNullOrWhiteSpace(fromFile))
            {
                logger.LogInformation("Loading SQLite extra PRAGMAs from JELLYFIN_SQLITE_PRAGMAS_FILE: {Path}", filePath);
                return ParsePragmaStatements(logger, fromFile);
            }

            return Array.Empty<string>();
        }

        // 3) Auto-discover: prioritize CONFIG dir (where XML lives), then DATA dir, then well-known fallbacks
        foreach (var candidate in GetDefaultPragmaFileCandidates())
        {
            var fromFile = TryReadFile(logger, candidate, logWhenMissing: false);
            if (!string.IsNullOrWhiteSpace(fromFile))
            {
                logger.LogInformation("Loading SQLite extra PRAGMAs from default file: {Path}", candidate);
                return ParsePragmaStatements(logger, fromFile);
            }
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Finds candidates in the most compatible order:
    /// 1) JELLYFIN_CONFIG_DIR (official image: /config/config) where XML config lives
    /// 2) JELLYFIN_DATA_DIR   (official image: /config)
    /// 3) Common container paths (linuxserver/custom): /config/config, /conf/config, /config.
    /// </summary>
    private static IEnumerable<string> GetDefaultPragmaFileCandidates()
    {
        // Prefer config dir first (the "XML folder")
        var configDir = Environment.GetEnvironmentVariable("JELLYFIN_CONFIG_DIR");
        if (!string.IsNullOrWhiteSpace(configDir))
        {
            foreach (var p in CandidatesInDir(configDir))
            {
                yield return p;
            }

            // Also check parent of configDir (common: /config/config -> /config)
            var parent = SafeGetParent(configDir);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                foreach (var p in CandidatesInDir(parent))
                {
                    yield return p;
                }
            }
        }

        // Then data dir
        var dataDir = Environment.GetEnvironmentVariable("JELLYFIN_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(dataDir))
        {
            foreach (var p in CandidatesInDir(dataDir))
            {
                yield return p;
            }
        }

        // Finally, known defaults for popular images / layouts
        foreach (var p in CandidatesInDir("/config/config"))
        {
            yield return p;
        }

        foreach (var p in CandidatesInDir("/conf/config")) // in case you truly have /conf/config
        {
            yield return p;
        }

        foreach (var p in CandidatesInDir("/config"))
        {
            yield return p;
        }
    }

    private static IEnumerable<string> CandidatesInDir(string dir)
    {
        // Keep it simple: allow either "pragmas.sql" or "sqlite-pragmas.sql"
        yield return Path.Combine(dir, "pragmas.sql");
        yield return Path.Combine(dir, "sqlite-pragmas.sql");
    }

    private static string? SafeGetParent(string path)
    {
        try
        {
            return Directory.GetParent(path)?.FullName;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryReadFile(ILogger logger, string path, bool logWhenMissing)
    {
        try
        {
            if (!File.Exists(path))
            {
                if (logWhenMissing)
                {
                    logger.LogWarning("SQLite PRAGMA file was set but does not exist: {Path}", path);
                }

                return null;
            }

            var info = new FileInfo(path);
            const long maxBytes = 256 * 1024; // 256KB guardrail
            if (info.Length > maxBytes)
            {
                logger.LogWarning(
                    "SQLite PRAGMA file is too large ({Size} bytes). Max allowed is {Max} bytes. Ignoring: {Path}",
                    info.Length,
                    maxBytes,
                    path);
                return null;
            }

            return File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read SQLite PRAGMA file: {Path}", path);
            return null;
        }
    }

    private static IReadOnlyList<string> ParsePragmaStatements(ILogger logger, string raw)
    {
        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Where(l => !IsFullLineComment(l))
            .ToArray();

        var normalized = string.Join("\n", lines);

        var parts = normalized.Split(';')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        var statements = new List<string>(parts.Count);
        foreach (var p in parts)
        {
            var stmt = p.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase) ? p : $"PRAGMA {p}";

            if (!stmt.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Ignoring non-PRAGMA SQLite statement from extra pragmas: {Statement}", p);
                continue;
            }

            if (!stmt.EndsWith(';'))
            {
                stmt += ";";
            }

            statements.Add(stmt);
        }

        return statements
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsFullLineComment(string line)
    {
        if (line.StartsWith('#'))
        {
            return true;
        }

        return line.Length >= 2 && line[0] == '-' && line[1] == '-';
    }
}
