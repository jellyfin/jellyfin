using System;

namespace MediaBrowser.Model.Dlna
{
    public class ResolutionNormalizer
    {
        private static readonly ResolutionConfiguration[] Configurations = 
            new []
            {
                new ResolutionConfiguration(426, 320000),
                new ResolutionConfiguration(640, 400000),
                new ResolutionConfiguration(720, 950000),
                new ResolutionConfiguration(1280, 2500000),
                new ResolutionConfiguration(1920, 4000000),
                new ResolutionConfiguration(3840, 35000000)
            };

        public static ResolutionOptions Normalize(int? inputBitrate,
            int? inputWidth,
            int? inputHeight,
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

            //if (maxWidth.HasValue && inputWidth.HasValue && maxWidth >= inputWidth.Value)
            //{
            //    maxWidth = null;
            //    maxHeight = null;
            //}

            //if (maxHeight.HasValue && inputHeight.HasValue && maxHeight >= inputHeight.Value)
            //{
            //    maxWidth = null;
            //    maxHeight = null;
            //}

            return new ResolutionOptions
            {
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
        }

        private static ResolutionConfiguration GetResolutionConfiguration(int outputBitrate)
        {
            ResolutionConfiguration previousOption = null;

            foreach (var config in Configurations)
            {
                if (outputBitrate <= config.MaxBitrate)
                {
                    return previousOption ?? config;
                }

                previousOption = config;
            }

            return null;
        }
    }
}
