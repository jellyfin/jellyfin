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

        public static ResolutionOptions Normalize(int maxBitrate,
            string codec,
            int? maxWidth,
            int? maxHeight)
        {
            foreach (var config in Configurations)
            {
                if (maxBitrate <= config.MaxBitrate)
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
    }

    public class ResolutionConfiguration
    {
        public int MaxWidth { get; set; }
        public int MaxBitrate { get; set; }

        public ResolutionConfiguration(int maxWidth, int maxBitrate)
        {
            MaxWidth = maxWidth;
            MaxBitrate = maxBitrate;
        }
    }

    public class ResolutionOptions
    {
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
    }
}
