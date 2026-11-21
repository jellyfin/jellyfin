using System;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.MediaEncoding.Keyframes;
using Microsoft.Extensions.Logging;
using Extractor = Jellyfin.MediaEncoding.Keyframes.Matroska.MatroskaKeyframeExtractor;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <inheritdoc />
public class MatroskaKeyframeExtractor : IKeyframeExtractor
{
    private const int MinKeyframesForIntervalCheck = 10;
    private static readonly long _minAverageKeyframeIntervalTicks = TimeSpan.FromMilliseconds(250).Ticks;

    private readonly ILogger<MatroskaKeyframeExtractor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MatroskaKeyframeExtractor"/> class.
    /// </summary>
    /// <param name="logger">An instance of the <see cref="ILogger{MatroskaKeyframeExtractor}"/> interface.</param>
    public MatroskaKeyframeExtractor(ILogger<MatroskaKeyframeExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsMetadataBased => true;

    /// <inheritdoc />
    public bool TryExtractKeyframes(Guid itemId, string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        if (!filePath.AsSpan().EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
        {
            keyframeData = null;
            return false;
        }

        try
        {
            var extracted = Extractor.GetKeyframeData(filePath);
            if (!HasPlausibleKeyframeInterval(extracted, out var averageIntervalTicks))
            {
                _logger.LogWarning(
                    "Matroska cues in {FilePath} appear to index non-keyframes (average interval {AverageIntervalMs:F1} ms across {CueCount} cue points); skipping metadata-based keyframe extraction",
                    filePath,
                    TimeSpan.FromTicks(averageIntervalTicks).TotalMilliseconds,
                    extracted.KeyframeTicks.Count);
                keyframeData = null;
                return false;
            }

            keyframeData = extracted;
            return keyframeData.KeyframeTicks.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extracting keyframes from {FilePath} using matroska metadata failed", filePath);
        }

        keyframeData = null;
        return false;
    }

    internal static bool HasPlausibleKeyframeInterval(KeyframeData keyframeData, out long averageIntervalTicks)
    {
        var count = keyframeData.KeyframeTicks.Count;
        if (count < MinKeyframesForIntervalCheck)
        {
            averageIntervalTicks = 0;
            return true;
        }

        var spanTicks = keyframeData.KeyframeTicks[^1] - keyframeData.KeyframeTicks[0];
        averageIntervalTicks = spanTicks / (count - 1);
        return averageIntervalTicks >= _minAverageKeyframeIntervalTicks;
    }
}
