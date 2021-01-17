using System;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ResolutionNormalizer" />.
    /// </summary>
    public static class ResolutionNormalizer
    {
        private static readonly ResolutionConfiguration[] _configurations =
            new[]
            {
                new ResolutionConfiguration(426, 320000),
                new ResolutionConfiguration(640, 400000),
                new ResolutionConfiguration(720, 950000),
                new ResolutionConfiguration(1280, 2500000),
                new ResolutionConfiguration(1920, 4000000),
                new ResolutionConfiguration(2560, 20000000),
                new ResolutionConfiguration(3840, 35000000)
            };

        /// <summary>
        /// The Normalize.
        /// </summary>
        /// <param name="inputBitrate">An optional input bitrate.</param>
        /// <param name="outputBitrate">An optional output bitrate.</param>
        /// <param name="maxWidth">An optional maximum width.</param>
        /// <param name="maxHeight">An optional maximum height.</param>
        /// <returns>The <see cref="ResolutionOptions"/>.</returns>
        public static ResolutionOptions Normalize(
            int? inputBitrate,
            int outputBitrate,
            int? maxWidth,
            int? maxHeight)
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

            var resolutionConfig = GetResolutionConfiguration(outputBitrate);
            if (resolutionConfig != null)
            {
                var originvalValue = maxWidth;

                maxWidth = Math.Min(resolutionConfig.MaxWidth, maxWidth ?? resolutionConfig.MaxWidth);
                if (!originvalValue.HasValue || originvalValue.Value != maxWidth.Value)
                {
                    maxHeight = null;
                }
            }

            return new ResolutionOptions
            {
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
        }

        /// <summary>
        /// The ScaleBitrate.
        /// </summary>
        /// <param name="bitrate">The bitrate<see cref="int"/>.</param>
        /// <param name="inputVideoCodec">The inputVideoCodec<see cref="string"/>.</param>
        /// <param name="outputVideoCodec">The outputVideoCodec<see cref="string"/>.</param>
        /// <returns>The <see cref="int"/>.</returns>
        public static int ScaleBitrate(int bitrate, string inputVideoCodec, string outputVideoCodec)
        {
            var inputScaleFactor = GetVideoBitrateScaleFactor(inputVideoCodec);
            var outputScaleFactor = GetVideoBitrateScaleFactor(outputVideoCodec);
            var scaleFactor = outputScaleFactor / inputScaleFactor;
            var newBitrate = scaleFactor * bitrate;

            return Convert.ToInt32(newBitrate);
        }

        /// <summary>
        /// The GetResolutionConfiguration.
        /// </summary>
        /// <param name="outputBitrate">The outputBitrate<see cref="int"/>.</param>
        /// <returns>The <see cref="ResolutionConfiguration"/>.</returns>
        private static ResolutionConfiguration? GetResolutionConfiguration(int outputBitrate)
        {
            ResolutionConfiguration? previousOption = null;

            foreach (var config in _configurations)
            {
                if (outputBitrate <= config.MaxBitrate)
                {
                    return previousOption ?? config;
                }

                previousOption = config;
            }

            return null;
        }

        /// <summary>
        /// The GetVideoBitrateScaleFactor.
        /// </summary>
        /// <param name="codec">The codec<see cref="string"/>.</param>
        /// <returns>The <see cref="double"/>.</returns>
        private static double GetVideoBitrateScaleFactor(string codec)
        {
            if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "vp9", StringComparison.OrdinalIgnoreCase))
            {
                return .6;
            }

            return 1;
        }
    }
}
