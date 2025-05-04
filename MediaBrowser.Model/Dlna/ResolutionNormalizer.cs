#nullable disable
#pragma warning disable CS1591

using System;
using System.Linq;

namespace MediaBrowser.Model.Dlna
{
    public static class ResolutionNormalizer
    {
        // Please note: all bitrate here are in the scale of SDR h264 bitrate at 30fps
        private static readonly ResolutionConfiguration[] _configurations =
        [
            new ResolutionConfiguration(416, 365000),
            new ResolutionConfiguration(640, 730000),
            new ResolutionConfiguration(768, 1100000),
            new ResolutionConfiguration(960, 3000000),
            new ResolutionConfiguration(1280, 6000000),
            new ResolutionConfiguration(1920, 13500000),
            new ResolutionConfiguration(2560, 28000000),
            new ResolutionConfiguration(3840, 50000000)
        ];

        public static ResolutionOptions Normalize(
            int? inputBitrate,
            int outputBitrate,
            int h264EquivalentOutputBitrate,
            int? maxWidth,
            int? maxHeight,
            float? targetFps,
            bool isHdr = false) // We are not doing HDR transcoding for now, leave for future use
        {
            // If the bitrate isn't changing, then don't downscale the resolution
            if (inputBitrate.HasValue && outputBitrate >= inputBitrate.Value)
            {
                if (maxWidth.HasValue || maxHeight.HasValue)
                {
                    return new ResolutionOptions
                    {
                        MaxWidth = maxWidth,
                        MaxHeight = maxHeight
                    };
                }
            }

            var referenceBitrate = h264EquivalentOutputBitrate * (30.0f / (targetFps ?? 30.0f));

            if (isHdr)
            {
                referenceBitrate *= 0.8f;
            }

            var resolutionConfig = GetResolutionConfiguration(Convert.ToInt32(referenceBitrate));

            if (resolutionConfig is null)
            {
                return new ResolutionOptions { MaxWidth = maxWidth, MaxHeight = maxHeight };
            }

            var originWidthValue = maxWidth;

            maxWidth = Math.Min(resolutionConfig.MaxWidth, maxWidth ?? resolutionConfig.MaxWidth);
            if (!originWidthValue.HasValue || originWidthValue.Value != maxWidth.Value)
            {
                maxHeight = null;
            }

            return new ResolutionOptions
            {
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
        }

        private static ResolutionConfiguration GetResolutionConfiguration(int outputBitrate)
        {
            return _configurations.FirstOrDefault(config => outputBitrate <= config.MaxBitrate);
        }
    }
}
