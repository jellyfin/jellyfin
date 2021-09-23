using System;

namespace Jellyfin.MediaEncoding.Keyframes.FfProbe
{
    /// <summary>
    /// FfProbe based keyframe extractor.
    /// </summary>
    public static class FfProbeKeyframeExtractor
    {
        /// <summary>
        /// Extracts the keyframes using the ffprobe executable at the specified path.
        /// </summary>
        /// <param name="ffProbePath">The path to the ffprobe executable.</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>An instance of <see cref="KeyframeData"/>.</returns>
        public static KeyframeData GetKeyframeData(string ffProbePath, string filePath) => throw new NotImplementedException();
    }
}
