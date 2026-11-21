using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Jellyfin.MediaEncoding.Keyframes;
using Microsoft.Extensions.Logging;
using Extractor = Jellyfin.MediaEncoding.Keyframes.Mp4.Mp4KeyframeExtractor;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <inheritdoc />
public class Mp4KeyframeExtractor : IKeyframeExtractor
{
    private static readonly string[] _extensions = [".mp4", ".m4v", ".mov"];

    private readonly ILogger<Mp4KeyframeExtractor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mp4KeyframeExtractor"/> class.
    /// </summary>
    /// <param name="logger">An instance of the <see cref="ILogger{Mp4KeyframeExtractor}"/> interface.</param>
    public Mp4KeyframeExtractor(ILogger<Mp4KeyframeExtractor> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsMetadataBased => true;

    /// <inheritdoc />
    public bool TryExtractKeyframes(Guid itemId, string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        if (!_extensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
        {
            keyframeData = null;
            return false;
        }

        try
        {
            keyframeData = Extractor.GetKeyframeData(filePath);
            return keyframeData.KeyframeTicks.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extracting keyframes from {FilePath} using mp4 metadata failed", filePath);
        }

        keyframeData = null;
        return false;
    }
}
