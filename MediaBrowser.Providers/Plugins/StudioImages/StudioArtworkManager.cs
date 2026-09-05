using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Plugins.StudioImages;

/// <summary>
/// Maintains a local copy of the jellyfin-artwork release bundle and answers studio image lookups against it.
/// </summary>
/// <remarks>
/// This is intentionally a static helper rather than a DI-registered service: bundled plugins
/// shipped in-tree are not visible to <c>IPluginServiceRegistrator</c> at the time the DI
/// container is built, so the manager has to be reachable without going through DI.
/// </remarks>
public static class StudioArtworkManager
{
    private const string ReleaseAssetUrl = "https://github.com/jellyfin/jellyfin-artwork/releases/download/latest/release.zip";
    private const string UserAgent = "Jellyfin-Server";

    // Raster first: Jellyfin's image processor (SkiaSharp) can't decode SVG, so any web-UI
    // request that asks for a resize/encode (e.g. fillHeight=240&format=webp) fails when the
    // stored ItemImageInfo.Path points to a .svg. We still keep .svg as a last-resort fallback
    // for studios where the bundle only ships a vector asset.
    private static readonly string[] _imageExtensions = [".webp", ".png", ".jpg", ".svg"];
    private static readonly SemaphoreSlim _refreshGate = new(1, 1);
    private static readonly JsonSerializerOptions _manifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static volatile CachedManifest? _cache;

    private static string ArtworkRoot => Path.Combine(Plugin.Instance.DataFolderPath, "artwork");

    private static string StagingRoot => Path.Combine(Plugin.Instance.DataFolderPath, "artwork.new");

    private static string ReleaseTagFile => Path.Combine(Plugin.Instance.DataFolderPath, "release.tag");

    private static string StudiosDir => Path.Combine(ArtworkRoot, "studios");

    private static string ManifestFile => Path.Combine(ArtworkRoot, "studios.json");

    /// <summary>
    /// Resolves a Jellyfin studio entity to its slug inside the local artwork bundle.
    /// </summary>
    /// <remarks>
    /// Match order: bundled manifest by provider id (e.g. <c>tmdb</c>=14639), then by canonical name
    /// or any <c>aka</c> alias (case-insensitive). Falls back to slugifying the display name when no
    /// manifest is present or no entry matched - preserves behaviour for studios not yet in the bundle.
    /// </remarks>
    /// <param name="displayName">The studio's display name.</param>
    /// <param name="providerIds">The studio's provider id map (e.g. <c>{"Tmdb": "14639"}</c>); may be null.</param>
    /// <returns>The slug to look up under <c>studios/</c>, or <see cref="string.Empty"/> when the name is empty.</returns>
    public static string ResolveStudioSlug(string displayName, IReadOnlyDictionary<string, string>? providerIds)
    {
        var index = GetOrLoadManifestIndex();
        if (index is not null)
        {
            if (providerIds is not null)
            {
                foreach (var pair in providerIds)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        continue;
                    }

                    var key = ManifestIndex.MakeProviderKey(pair.Key, pair.Value);
                    if (index.ByProvider.TryGetValue(key, out var slug))
                    {
                        return slug;
                    }
                }
            }

            if (!string.IsNullOrEmpty(displayName)
                && index.ByName.TryGetValue(displayName, out var nameSlug))
            {
                return nameSlug;
            }
        }

        return StudiosImageProvider.Slugify(displayName);
    }

    /// <summary>
    /// Looks up a local image file for the given studio slug and image kind (e.g. "thumb", "primary", "logo").
    /// </summary>
    /// <param name="slug">The kebab-case studio machine-name (use <see cref="StudiosImageProvider.Slugify"/>).</param>
    /// <param name="imageKind">The image kind such as "thumb" or "primary".</param>
    /// <param name="path">The resolved absolute path on disk, when found.</param>
    /// <returns><c>true</c> if a matching file exists, otherwise <c>false</c>.</returns>
    public static bool TryGetStudioImagePath(string slug, string imageKind, out string? path)
    {
        path = null;
        if (string.IsNullOrEmpty(slug) || string.IsNullOrEmpty(imageKind))
        {
            return false;
        }

        // The bundle shards studios by the first character of the slug, e.g.
        // studios/a/abema/, studios/2/24-frames/. Slugify guarantees the first
        // character is lowercase ASCII letter-or-digit, so it's directly usable
        // as a shard key without further normalisation.
        var studioDir = Path.Combine(StudiosDir, slug[..1], slug);
        if (!Directory.Exists(studioDir))
        {
            return false;
        }

        foreach (var ext in _imageExtensions)
        {
            var candidate = Path.Combine(studioDir, imageKind + ext);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Looks up the bundle's top-level placeholder image for the given image kind.
    /// </summary>
    /// <param name="imageKind">The image kind such as "primary" or "thumb".</param>
    /// <param name="path">The resolved absolute path on disk, when found.</param>
    /// <returns><c>true</c> if a placeholder file exists, otherwise <c>false</c>.</returns>
    public static bool TryGetPlaceholderImagePath(string imageKind, out string? path)
    {
        path = null;
        if (string.IsNullOrEmpty(imageKind) || !Directory.Exists(ArtworkRoot))
        {
            return false;
        }

        var fileStem = "placeholder-" + imageKind;
        foreach (var ext in _imageExtensions)
        {
            var candidate = Path.Combine(ArtworkRoot, fileStem + ext);
            if (File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Downloads the latest jellyfin-artwork release bundle and replaces the local copy when changed.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when a new bundle was applied; <c>false</c> when the local copy was already up to date.</returns>
    public static async Task<bool> EnsureUpToDateAsync(IHttpClientFactory httpClientFactory, ILogger logger, CancellationToken cancellationToken)
    {
        await _refreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleaseAssetUrl);
            request.Headers.UserAgent.ParseAdd(UserAgent);

            var currentVersion = ReadCurrentTag();
            var haveLocal = !string.IsNullOrEmpty(currentVersion) && Directory.Exists(StudiosDir);
            if (haveLocal && TryParseConditional(currentVersion!, out var etag, out var lastModified))
            {
                if (etag is not null)
                {
                    request.Headers.IfNoneMatch.Add(etag);
                }

                if (lastModified is not null)
                {
                    request.Headers.IfModifiedSince = lastModified;
                }
            }

            var httpClient = httpClientFactory.CreateClient(NamedClient.Default);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                logger.LogDebug("jellyfin-artwork release.zip not modified; nothing to do");
                return false;
            }

            response.EnsureSuccessStatusCode();

            var newVersion = BuildVersionToken(response);
            logger.LogInformation("Refreshing jellyfin-artwork release.zip (version: {Version})", newVersion);
            await DownloadAndStageAsync(response, logger, cancellationToken).ConfigureAwait(false);
            PromoteStaging();
            WriteCurrentTag(newVersion);
            _cache = null;
            logger.LogInformation("jellyfin-artwork updated");
            return true;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static async Task DownloadAndStageAsync(HttpResponseMessage response, ILogger logger, CancellationToken cancellationToken)
    {
        if (Directory.Exists(StagingRoot))
        {
            Directory.Delete(StagingRoot, recursive: true);
        }

        Directory.CreateDirectory(StagingRoot);

        // Buffer the response body to a temp file so ZipArchive can seek over it.
        var tempZip = Path.Combine(Plugin.Instance.DataFolderPath, "release.download.zip");
        try
        {
            var network = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (network.ConfigureAwait(false))
            {
                var fs = File.Create(tempZip);
                await using (fs.ConfigureAwait(false))
                {
                    await network.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }
            }

            var zipStream = File.OpenRead(tempZip);
            await using (zipStream.ConfigureAwait(false))
            {
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExtractEntry(entry, StagingRoot);
                }
            }
        }
        finally
        {
            try
            {
                if (File.Exists(tempZip))
                {
                    File.Delete(tempZip);
                }
            }
            catch (IOException ex)
            {
                logger.LogDebug(ex, "Failed to delete temporary zip {Path}", tempZip);
            }
        }
    }

    private static void ExtractEntry(ZipArchiveEntry entry, string targetRoot)
    {
        if (string.IsNullOrEmpty(entry.FullName))
        {
            return;
        }

        var fullTargetRoot = Path.GetFullPath(targetRoot) + Path.DirectorySeparatorChar;
        var destPath = Path.GetFullPath(Path.Combine(targetRoot, entry.FullName));
        if (!destPath.StartsWith(fullTargetRoot, StringComparison.Ordinal))
        {
            return; // zip-slip guard: entry resolves outside the target directory
        }

        if (string.IsNullOrEmpty(entry.Name))
        {
            Directory.CreateDirectory(destPath);
            return;
        }

        var destDir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        entry.ExtractToFile(destPath, overwrite: true);
    }

    private static void PromoteStaging()
    {
        var backup = ArtworkRoot + ".old";
        if (Directory.Exists(backup))
        {
            Directory.Delete(backup, recursive: true);
        }

        if (Directory.Exists(ArtworkRoot))
        {
            Directory.Move(ArtworkRoot, backup);
        }

        Directory.Move(StagingRoot, ArtworkRoot);

        if (Directory.Exists(backup))
        {
            Directory.Delete(backup, recursive: true);
        }
    }

    private static string BuildVersionToken(HttpResponseMessage response)
    {
        var etag = response.Headers.ETag?.ToString();
        var lastModified = response.Content.Headers.LastModified;
        if (!string.IsNullOrEmpty(etag) && lastModified is not null)
        {
            return etag + "|" + lastModified.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrEmpty(etag))
        {
            return etag;
        }

        if (lastModified is not null)
        {
            return "@" + lastModified.Value.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
        }

        return "downloaded:" + DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }

    private static bool TryParseConditional(string stored, out EntityTagHeaderValue? etag, out DateTimeOffset? lastModified)
    {
        etag = null;
        lastModified = null;

        if (string.IsNullOrEmpty(stored))
        {
            return false;
        }

        var pipe = stored.IndexOf('|', StringComparison.Ordinal);
        if (pipe >= 0)
        {
            var etagPart = stored[..pipe];
            var modifiedPart = stored[(pipe + 1)..];
            if (!EntityTagHeaderValue.TryParse(etagPart, out etag))
            {
                etag = null;
            }

            if (DateTimeOffset.TryParse(modifiedPart, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                lastModified = parsed;
            }

            return etag is not null || lastModified is not null;
        }

        if (stored.StartsWith('@'))
        {
            if (DateTimeOffset.TryParse(stored[1..], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
            {
                lastModified = parsed;
                return true;
            }

            return false;
        }

        if (stored.StartsWith("downloaded:", StringComparison.Ordinal))
        {
            // No conditional info available; force re-download.
            return false;
        }

        return EntityTagHeaderValue.TryParse(stored, out etag);
    }

    private static string? ReadCurrentTag()
    {
        if (!File.Exists(ReleaseTagFile))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(ReleaseTagFile).Trim();
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void WriteCurrentTag(string tag)
    {
        Directory.CreateDirectory(Plugin.Instance.DataFolderPath);
        File.WriteAllText(ReleaseTagFile, tag);
    }

    private static ManifestIndex GetOrLoadManifestIndex()
    {
        // Stat the manifest once per call to detect bundle changes that didn't go through
        // EnsureUpToDateAsync.
        var fileInfo = new FileInfo(ManifestFile);
        var exists = fileInfo.Exists;
        var mtime = exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue;
        var length = exists ? fileInfo.Length : 0L;

        var current = _cache;
        if (current is not null && current.Mtime == mtime && current.Length == length)
        {
            return current.Index;
        }

        var index = LoadManifestIndex(logger: null) ?? ManifestIndex.Empty;
        _cache = new CachedManifest(index, mtime, length);
        return index;
    }

    private static ManifestIndex? LoadManifestIndex(ILogger? logger)
    {
        if (!File.Exists(ManifestFile))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(ManifestFile);
            var entries = JsonSerializer.Deserialize<List<ManifestEntry>>(stream, _manifestJsonOptions);
            if (entries is null || entries.Count == 0)
            {
                return ManifestIndex.Empty;
            }

            return ManifestIndex.Build(entries);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            logger?.LogWarning(ex, "Failed to load studios.json manifest at {Path}", ManifestFile);
            return ManifestIndex.Empty;
        }
    }

    private sealed class ManifestIndex
    {
        public static readonly ManifestIndex Empty = new(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

        public ManifestIndex(Dictionary<string, string> byProvider, Dictionary<string, string> byName)
        {
            ByProvider = byProvider;
            ByName = byName;
        }

        public Dictionary<string, string> ByProvider { get; }

        public Dictionary<string, string> ByName { get; }

        public static string MakeProviderKey(string providerName, string id)
            => providerName + ":" + id;

        public static ManifestIndex Build(List<ManifestEntry> entries)
        {
            var byProvider = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                // Placeholders are intentionally indexed too: their bundle slug is the only way
                // to resolve studios whose name contains characters the slugifier cannot reduce
                // to the ASCII subset the bundle layout uses (Polish ł, CJK glyphs, etc.).
                var slug = StudiosImageProvider.Slugify(entry.Name);
                if (string.IsNullOrEmpty(slug))
                {
                    continue;
                }

                // First-wins: don't let later duplicates clobber earlier ones.
                byName.TryAdd(entry.Name, slug);
                if (entry.Aka is not null)
                {
                    foreach (var alias in entry.Aka)
                    {
                        if (!string.IsNullOrEmpty(alias))
                        {
                            byName.TryAdd(alias, slug);
                        }
                    }
                }

                if (entry.Providers is not null)
                {
                    foreach (var provider in entry.Providers)
                    {
                        if (string.IsNullOrEmpty(provider.ProviderName) || string.IsNullOrEmpty(provider.Id))
                        {
                            continue;
                        }

                        byProvider.TryAdd(MakeProviderKey(provider.ProviderName, provider.Id), slug);
                    }
                }
            }

            return new ManifestIndex(byProvider, byName);
        }
    }

    private sealed class ManifestEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("aka")]
        public List<string>? Aka { get; set; }

        [JsonPropertyName("placeholder")]
        public bool Placeholder { get; set; }

        [JsonPropertyName("providers")]
        public List<ManifestProvider>? Providers { get; set; }
    }

    private sealed class ManifestProvider
    {
        [JsonPropertyName("provider_name")]
        public string? ProviderName { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed record CachedManifest(ManifestIndex Index, DateTime Mtime, long Length);
}
