using System;
using System.IO;
using Jellyfin.MediaEncoding.Keyframes.FfProbe;
using Jellyfin.MediaEncoding.Keyframes.FfTool;
using Jellyfin.MediaEncoding.Keyframes.Matroska;
using Microsoft.Extensions.Logging;

namespace Jellyfin.MediaEncoding.Keyframes
{
    /// <summary>
    /// Manager class for the set of keyframe extractors.
    /// </summary>
    public class KeyframeExtractor
    {
        private readonly ILogger<KeyframeExtractor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyframeExtractor"/> class.
        /// </summary>
        /// <param name="logger">An instance of the <see cref="ILogger{KeyframeExtractor}"/> interface.</param>
        public KeyframeExtractor(ILogger<KeyframeExtractor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Extracts the keyframe positions from a video file.
        /// </summary>
        /// <param name="filePath">Absolute file path to the media file.</param>
        /// <param name="ffProbePath">Absolute file path to the ffprobe executable.</param>
        /// <param name="ffToolPath">Absolute file path to the fftool executable.</param>
        /// <returns></returns>
        public KeyframeData GetKeyframeData(string filePath, string ffProbePath, string ffToolPath)
        {
            var extension = Path.GetExtension(filePath);
            if (string.Equals(extension, ".mkv", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return MatroskaKeyframeExtractor.GetKeyframeData(filePath);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "{MatroskaKeyframeExtractor} failed to extract keyframes", nameof(MatroskaKeyframeExtractor));
                }
            }

            if (!string.IsNullOrEmpty(ffToolPath))
            {
                return FfToolKeyframeExtractor.GetKeyframeData(ffToolPath, filePath);
            }

            return FfProbeKeyframeExtractor.GetKeyframeData(ffProbePath, filePath);
        }
    }
}
