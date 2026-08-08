using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.MediaEncoding.Keyframes;

namespace Jellyfin.MediaEncoding.Hls.Extractors;

/// <summary>
/// Keyframe extractor.
/// </summary>
public interface IKeyframeExtractor
{
    /// <summary>
    /// Gets a value indicating whether the extractor is based on container metadata.
    /// </summary>
    bool IsMetadataBased { get; }

    /// <summary>
    /// Attempt to extract keyframes.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The keyframes, or <c>null</c> when this extractor cannot handle the file.</returns>
    Task<KeyframeData?> ExtractKeyframesAsync(Guid itemId, string filePath, CancellationToken cancellationToken);
}
