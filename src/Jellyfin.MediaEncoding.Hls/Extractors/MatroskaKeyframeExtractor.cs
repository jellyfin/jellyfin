using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.MediaEncoding.Keyframes;
using Microsoft.Extensions.Logging;
using Extractor = Jellyfin.MediaEncoding.Keyframes.Matroska.MatroskaKeyframeExtractor;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <inheritdoc />
public class MatroskaKeyframeExtractor : IKeyframeExtractor
{
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
    public Task<KeyframeData?> ExtractKeyframesAsync(Guid itemId, string filePath, CancellationToken cancellationToken)
    {
        if (!filePath.AsSpan().EndsWith(".mkv", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<KeyframeData?>(null);
        }

        try
        {
            // Reading the container index is pure file work, so there is nothing to await here.
            var keyframeData = Extractor.GetKeyframeData(filePath);
            return Task.FromResult(keyframeData.KeyframeTicks.Count > 0 ? keyframeData : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Extracting keyframes from {FilePath} using matroska metadata failed", filePath);
        }

        return Task.FromResult<KeyframeData?>(null);
    }
}
