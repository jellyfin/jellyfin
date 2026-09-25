using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.Listings;

/// <summary>
/// Caches one station's schedule for one day, keyed by the md5 Schedules Direct reports for it.
/// </summary>
/// <remarks>
/// The API requires clients to ask /schedules/md5 what changed and re-download only that. A day
/// whose md5 still matches needs no /schedules, /programs or /metadata/programs request at all,
/// which is what keeps a guide refresh inside the account's daily quotas.
/// </remarks>
public class SchedulesDirectScheduleCache
{
    private readonly ILogger _logger;
    private readonly string _cacheRoot;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchedulesDirectScheduleCache"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="appPaths">The application paths.</param>
    public SchedulesDirectScheduleCache(ILogger logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _cacheRoot = Path.Combine(appPaths.CachePath, "schedulesdirect", "schedules");
    }

    /// <summary>
    /// Reads the cached day if it was stored under the same md5.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <param name="date">The date, formatted yyyy-MM-dd.</param>
    /// <param name="md5">The md5 Schedules Direct currently reports, or null if unknown.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached entry, or <c>null</c> when it is missing or stale.</returns>
    public async Task<SchedulesDirectCachedDay?> GetAsync(string stationId, string date, string? md5, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(md5))
        {
            return null;
        }

        var path = GetPath(stationId, date);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var stream = File.OpenRead(path);
            await using (stream.ConfigureAwait(false))
            {
                var cached = await JsonSerializer.DeserializeAsync<SchedulesDirectCachedDay>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
                return string.Equals(cached?.Md5, md5, StringComparison.Ordinal) ? cached : null;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogDebug(ex, "Discarding unreadable Schedules Direct cache entry {Path}", path);
            return null;
        }
    }

    /// <summary>
    /// Stores a freshly downloaded day.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <param name="date">The date, formatted yyyy-MM-dd.</param>
    /// <param name="day">The day to store.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the write.</returns>
    public async Task SetAsync(string stationId, string date, SchedulesDirectCachedDay day, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(day.Md5))
        {
            // Without an md5 there is nothing to validate the entry against later.
            return;
        }

        var path = GetPath(stationId, date);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var stream = File.Create(path);
            await using (stream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(stream, day, _jsonOptions, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogWarning(ex, "Failed to cache Schedules Direct schedule {Path}", path);
        }
    }

    /// <summary>
    /// Deletes cached days that are no longer requested, so the cache cannot grow without bound.
    /// </summary>
    /// <param name="stationId">The station id.</param>
    /// <param name="keepDates">The dates to keep.</param>
    public void Prune(string stationId, IEnumerable<string> keepDates)
    {
        var directory = Path.Combine(_cacheRoot, GetSafeName(stationId));
        if (!Directory.Exists(directory))
        {
            return;
        }

        var keep = new HashSet<string>(keepDates.Select(GetSafeName), StringComparer.Ordinal);

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
            {
                if (!keep.Contains(Path.GetFileNameWithoutExtension(file)))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to prune Schedules Direct cache in {Directory}", directory);
        }
    }

    /// <summary>
    /// Deletes entries that have not been touched for a while, so that stations which have left
    /// the lineup do not keep their cache forever.
    /// </summary>
    /// <param name="maxAge">The age past which an untouched entry is deleted.</param>
    public void PruneStale(TimeSpan maxAge)
    {
        if (!Directory.Exists(_cacheRoot))
        {
            return;
        }

        var cutoff = DateTime.UtcNow - maxAge;

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(_cacheRoot))
            {
                foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                    {
                        File.Delete(file);
                    }
                }

                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Failed to prune stale Schedules Direct cache entries in {Directory}", _cacheRoot);
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

    private string GetPath(string stationId, string date)
        => Path.Combine(_cacheRoot, GetSafeName(stationId), GetSafeName(date) + ".json");
}
