using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.MediaEncoding.Hls.Cache;

/// <inheritdoc />
public class CacheDecorator : IKeyframeExtractor
{
    private readonly IKeyframeExtractor _keyframeExtractor;
    private readonly ILogger<CacheDecorator> _logger;
    private readonly string _keyframeExtractorName;
    private static readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private readonly string _keyframeCachePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDecorator"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="keyframeExtractor">An instance of the <see cref="IKeyframeExtractor"/> interface.</param>
    /// <param name="logger">An instance of the <see cref="ILogger{CacheDecorator}"/> interface.</param>
    public CacheDecorator(IApplicationPaths applicationPaths, IKeyframeExtractor keyframeExtractor, ILogger<CacheDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(applicationPaths);
        ArgumentNullException.ThrowIfNull(keyframeExtractor);

        _keyframeExtractor = keyframeExtractor;
        _logger = logger;
        _keyframeExtractorName = keyframeExtractor.GetType().Name;
        // TODO make the dir configurable
        _keyframeCachePath = Path.Combine(applicationPaths.DataPath, "keyframes");
    }

    /// <inheritdoc />
    public bool IsMetadataBased => _keyframeExtractor.IsMetadataBased;

    /// <inheritdoc />
    public bool TryExtractKeyframes(string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        keyframeData = null;
        var cachePath = GetCachePath(_keyframeCachePath, filePath);
        if (TryReadFromCache(cachePath, out var cachedResult))
        {
            keyframeData = cachedResult;
            return true;
        }

        if (!_keyframeExtractor.TryExtractKeyframes(filePath, out var result))
        {
            _logger.LogDebug("Failed to extract keyframes using {ExtractorName}", _keyframeExtractorName);
            return false;
        }

        _logger.LogDebug("Successfully extracted keyframes using {ExtractorName}", _keyframeExtractorName);
        keyframeData = result;
        SaveToCache(cachePath, keyframeData);
        return true;
    }

    private static void SaveToCache(string cachePath, KeyframeData keyframeData)
    {
        var json = JsonSerializer.Serialize(keyframeData, _jsonOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(cachePath) ?? throw new ArgumentException($"Provided path ({cachePath}) is not valid.", nameof(cachePath)));
        File.WriteAllText(cachePath, json);
    }

    private static string GetCachePath(string keyframeCachePath, string filePath)
    {
        var lastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);
        ReadOnlySpan<char> filename = (filePath + "_" + lastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)).GetMD5() + ".json";
        var prefix = filename[..1];

        return Path.Join(keyframeCachePath, prefix, filename);
    }

    private static bool TryReadFromCache(string cachePath, [NotNullWhen(true)] out KeyframeData? cachedResult)
    {
        if (File.Exists(cachePath))
        {
            var bytes = File.ReadAllBytes(cachePath);
            cachedResult = JsonSerializer.Deserialize<KeyframeData>(bytes, _jsonOptions);
            return cachedResult is not null;
        }

        cachedResult = null;
        return false;
    }
}
