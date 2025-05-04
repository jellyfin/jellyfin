using System;

namespace Jellyfin.MediaEncoding.Keyframes.FfTool;

/// <summary>
/// FfTool based keyframe extractor.
/// </summary>
public static class FfToolKeyframeExtractor
{
    /// <summary>
    /// Extracts the keyframes using the fftool executable at the specified path.
    /// </summary>
    /// <param name="ffToolPath">The path to the fftool executable.</param>
    /// <param name="filePath">The file path.</param>
    /// <returns>An instance of <see cref="KeyframeData"/>.</returns>
    public static KeyframeData GetKeyframeData(string ffToolPath, string filePath) => throw new NotImplementedException();
}
