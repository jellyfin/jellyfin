using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyfin.LiveTv.Listings.SchedulesDirectDtos;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings;

/// <summary>
/// Caches a lineup's channel map, keyed by the "modified" date Schedules Direct reports for it.
/// </summary>
/// <remarks>
/// The spec's steady-state flow is to compare the lineup's modified date in /status against the
/// copy the client holds and only download the lineup again when the server's is newer.
/// </remarks>
public class SchedulesDirectLineupCache
{
    private readonly ILogger _logger;
    private readonly string _cacheRoot;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulesDirectLineupCache"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">The application paths.</param>
    public SchedulesDirectLineupCache(ILogger logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _cacheRoot = Path.Combine(appPaths.CachePath, "schedulesdirect", "lineups");
    }

    /// <summary>
    /// Reads the cached lineup if it was stored under the same modified date.
    /// </summary>
    /// <param name="lineupId">The lineup id.</param>
    /// <param name="modified">The modified date Schedules Direct currently reports.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached lineup, or <c>null</c> when it is missing or stale.</returns>
    public async Task<ChannelDto?> GetAsync(string lineupId, DateTime modified, CancellationToken cancellationToken)
    {
        var path = GetPath(lineupId, modified);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<ChannelDto>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogDebug(ex, "Discarding unreadable Schedules Direct lineup cache entry {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Stores a freshly downloaded lineup and drops the copies it supersedes.
    /// </summary>
    /// <param name="lineupId">The lineup id.</param>
    /// <param name="modified">The modified date Schedules Direct reports for it.</param>
    /// <param name="lineup">The lineup to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    public async Task SetAsync(string lineupId, DateTime modified, ChannelDto lineup, CancellationToken cancellationToken)
    {
        var path = GetPath(lineupId, modified);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var stream = File.Create(path);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, lineup, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }

            foreach (var stale in Directory.EnumerateFiles(Path.GetDirectoryName(path)!, "*.json"))
            {
                if (!string.Equals(stale, path, StringComparison.Ordinal))
                {
                    File.Delete(stale);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Failed to cache Schedules Direct lineup {Path}", path);
        }
    }

    private static string GetSafeName(string value)
        => string.Create(value.Length, value, (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                span[i] = char.IsAsciiLetterOrDigit(source[i]) ? source[i] : '_';
            }
        });

    private string GetPath(string lineupId, DateTime modified)
        => Path.Combine(
            _cacheRoot,
            GetSafeName(lineupId),
            modified.ToUniversalTime().ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + ".json");
}
