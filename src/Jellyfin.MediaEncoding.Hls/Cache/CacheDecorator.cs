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

namespace Jellyfin.MediaEncoding.Hls.Cache;

/// <inheritdoc />
public class CacheDecorator : IKeyframeExtractor
{
    private readonly IKeyframeExtractor _keyframeExtractor;
    private static readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private readonly string _keyframeCachePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDecorator"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="keyframeExtractor">An instance of the <see cref="IKeyframeExtractor"/> interface.</param>
    public CacheDecorator(IApplicationPaths applicationPaths, IKeyframeExtractor keyframeExtractor)
    {
        _keyframeExtractor = keyframeExtractor;
        ArgumentNullException.ThrowIfNull(applicationPaths);

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
            return false;
        }

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
            return cachedResult != null;
        }

        cachedResult = null;
        return false;
    }
}
