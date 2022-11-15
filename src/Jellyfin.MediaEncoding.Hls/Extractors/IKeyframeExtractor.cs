using System.Diagnostics.CodeAnalysis;
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
    /// <param name="filePath">The path to the file.</param>
    /// <param name="keyframeData">The keyframes.</param>
    /// <returns>A value indicating whether the keyframe extraction was successful.</returns>
    bool TryExtractKeyframes(string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData);
}
