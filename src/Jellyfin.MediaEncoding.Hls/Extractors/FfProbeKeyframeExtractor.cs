using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.Extensions.Logging;
using Extractor = Jellyfin.MediaEncoding.Keyframes.FfProbe.FfProbeKeyframeExtractor;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <inheritdoc />
public class FfProbeKeyframeExtractor : IKeyframeExtractor
{
    private readonly IMediaEncoder _mediaEncoder;
    private readonly NamingOptions _namingOptions;
    private readonly ILogger<FfProbeKeyframeExtractor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfProbeKeyframeExtractor"/> class.
    /// </summary>
    /// <param name="mediaEncoder">An instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="namingOptions">An instance of <see cref="NamingOptions"/>.</param>
    /// <param name="logger">An instance of the <see cref="ILogger{FfprobeKeyframeExtractor}"/> interface.</param>
    public FfProbeKeyframeExtractor(IMediaEncoder mediaEncoder, NamingOptions namingOptions, ILogger<FfProbeKeyframeExtractor> logger)
    {
        _mediaEncoder = mediaEncoder;
        _namingOptions = namingOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsMetadataBased => false;

    /// <inheritdoc />
    public bool TryExtractKeyframes(string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        if (!_namingOptions.VideoFileExtensions.Contains(Path.GetExtension(filePath.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            keyframeData = null;
            return false;
        }

        try
        {
            keyframeData = Extractor.GetKeyframeData(_mediaEncoder.ProbePath, filePath);
            return keyframeData.KeyframeTicks.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extracting keyframes from {FilePath} using ffprobe failed", filePath);
        }

        keyframeData = null;
        return false;
    }
}
