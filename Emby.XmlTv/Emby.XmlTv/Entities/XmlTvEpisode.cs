using System;
using System.Text;

namespace Emby.XmlTv.Entities
{
    public class XmlTvEpisode
    {
        public int? Series { get; set; }
        public int? SeriesCount { get; set; }
        public int? Episode { get; set; }
        public int? EpisodeCount { get; set; }
        public string Title { get; set; }
        public int? Part { get; set; }
        public int? PartCount { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (Series.HasValue || SeriesCount.HasValue)
            {
                builder.AppendFormat("Series {0}", Series.HasValue ? Series.Value.ToString() : "?");
                if (SeriesCount.HasValue)
                {
                    builder.AppendFormat(" of {0}", SeriesCount);
                }
            }

            if (Episode.HasValue || EpisodeCount.HasValue)
            {
                builder.Append(builder.Length > 0 ? ", " : String.Empty);
                builder.AppendFormat("Episode {0}", Episode.HasValue ? Episode.Value.ToString() : "?");
                if (EpisodeCount.HasValue)
                {
                    builder.AppendFormat(" of {0}", EpisodeCount);
                }
            }

            if (Part.HasValue || PartCount.HasValue)
            {
                builder.Append(builder.Length > 0 ? ", " : String.Empty);
                builder.AppendFormat("Part {0}", Part.HasValue ? Part.Value.ToString() : "?");
                if (PartCount.HasValue)
                {
                    builder.AppendFormat(" of {0}", PartCount);
                }
            }

            return builder.ToString();
        }
    }

    
}
