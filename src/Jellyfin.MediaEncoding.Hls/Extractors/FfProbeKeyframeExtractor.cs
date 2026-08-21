using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using Microsoft.Extensions.Logging;
using Extractor = Jellyfin.MediaEncoding.Keyframes.FfProbe.FfProbeKeyframeExtractor;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <inheritdoc />
public class FfProbeKeyframeExtractor : IKeyframeExtractor
{
    private readonly IFFRunner _ffRunner;
    private readonly NamingOptions _namingOptions;
    private readonly ILogger<FfProbeKeyframeExtractor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FfProbeKeyframeExtractor"/> class.
    /// </summary>
    /// <param name="ffRunner">An instance of the <see cref="IFFRunner"/> interface.</param>
    /// <param name="namingOptions">An instance of <see cref="NamingOptions"/>.</param>
    /// <param name="logger">An instance of the <see cref="ILogger{FfprobeKeyframeExtractor}"/> interface.</param>
    public FfProbeKeyframeExtractor(IFFRunner ffRunner, NamingOptions namingOptions, ILogger<FfProbeKeyframeExtractor> logger)
    {
        _ffRunner = ffRunner;
        _namingOptions = namingOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsMetadataBased => false;

    /// <inheritdoc />
    public async Task<KeyframeData?> ExtractKeyframesAsync(Guid itemId, string filePath, CancellationToken cancellationToken)
    {
        if (!_namingOptions.VideoFileExtensions.Contains(Path.GetExtension(filePath.AsSpan()), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        KeyframeData? keyframeData = null;

        try
        {
            // The library owns the parsing; this layer only owns getting ffprobe to produce it.
            var result = await _ffRunner.RunAsync(
                new KeyframeScanRequest
                {
                    FilePath = filePath,

                    // Task.Run because Extractor.ParseStream is a blocking read loop
                    Stdout = (stdout, ct) => Task.Run(
                        () => keyframeData = Extractor.ParseStream(new StreamReader(stdout)),
                        ct)
                },
                cancellationToken).ConfigureAwait(false);

            if (!result.Succeeded)
            {
                _logger.LogError(
                    "Extracting keyframes from {FilePath} using ffprobe failed: {Stderr}",
                    filePath,
                    result.Stderr);

                return null;
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not "this file has no keyframes"; letting it surface as null would
            // cache that conclusion and hide why the scan stopped.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extracting keyframes from {FilePath} using ffprobe failed", filePath);
            return null;
        }

        return keyframeData?.KeyframeTicks.Count > 0 ? keyframeData : null;
    }
}
