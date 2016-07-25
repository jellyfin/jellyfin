using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dlna
{
    public class ResolutionNormalizer
    {
        private static readonly List<ResolutionConfiguration> Configurations = 
            new List<ResolutionConfiguration>
            {
                new ResolutionConfiguration(426, 320000),
                new ResolutionConfiguration(640, 400000),
                new ResolutionConfiguration(720, 950000),
                new ResolutionConfiguration(1280, 2500000)
            };

        public static ResolutionOptions Normalize(int? inputBitrate,
			int outputBitrate,
			string inputCodec,
            string outputCodec,
            int? maxWidth,
            int? maxHeight)
        {
			// If the bitrate isn't changing, then don't downlscale the resolution
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

			foreach (var config in Configurations)
            {
				if (outputBitrate <= config.MaxBitrate)
                {
                    var originvalValue = maxWidth;

                    maxWidth = Math.Min(config.MaxWidth, maxWidth ?? config.MaxWidth);
                    if (!originvalValue.HasValue || originvalValue.Value != maxWidth.Value)
                    {
                        maxHeight = null;
                    }

                    break;
                }
            }

            return new ResolutionOptions
            {
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
        }

        private static double GetVideoBitrateScaleFactor(string codec)
        {
            if (string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                return .5;
            }
            return 1;
        }

        public static int ScaleBitrate(int bitrate, string inputVideoCodec, string outputVideoCodec)
        {
            var inputScaleFactor = GetVideoBitrateScaleFactor(inputVideoCodec);
            var outputScaleFactor = GetVideoBitrateScaleFactor(outputVideoCodec);
            var scaleFactor = outputScaleFactor/inputScaleFactor;
            var newBitrate = scaleFactor*bitrate;

            return Convert.ToInt32(newBitrate);
        }
    }
}
