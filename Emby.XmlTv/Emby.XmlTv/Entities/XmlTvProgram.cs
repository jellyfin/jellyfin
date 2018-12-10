using System;
using System.Collections.Generic;
using System.Text;

namespace Emby.XmlTv.Entities
{
    public class XmlTvProgram : IEquatable<XmlTvProgram>
    {
        public string ChannelId { get; set; }

        public DateTimeOffset StartDate { get; set; }

        public DateTimeOffset EndDate { get; set; }

        public string Title { get; set; }
        
        public string Description { get; set; }
        public string ProgramId { get; set; }
        public string Quality { get; set; }

        public List<string> Categories { get; set; }

        public List<string> Countries { get; set; }

        public DateTimeOffset? PreviouslyShown { get; set; }

        public bool IsPreviouslyShown { get; set; }
        public bool IsNew { get; set; }

        public DateTimeOffset? CopyrightDate { get; set; }

        public XmlTvEpisode Episode { get; set; }

        public List<XmlTvCredit> Credits { get; set; }

        public XmlTvRating Rating { get; set; }

        public float? StarRating { get; set; }

        public XmlTvIcon Icon { get; set; }

        public XmlTvPremiere Premiere { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }
        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public XmlTvProgram()
        {
            Credits = new List<XmlTvCredit>();
            Categories = new List<string>();
            Countries = new List<string>();
            Episode = new XmlTvEpisode();

            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool Equals(XmlTvProgram other)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // If the other is null then return false
            if (other == null)
            {
                return false;
            }

            // Return true if the fields match:
            return ChannelId == other.ChannelId &&
                StartDate == other.StartDate &&
                EndDate == other.EndDate;
        }

        public override int GetHashCode()
        {
            return (ChannelId.GetHashCode() * 17) + (StartDate.GetHashCode() * 17) + (EndDate.GetHashCode() * 17);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("ChannelId: \t\t{0}\r\n", ChannelId);
            builder.AppendFormat("Title: \t\t{0}\r\n", Title);
            builder.AppendFormat("StartDate: \t\t{0}\r\n", StartDate);
            builder.AppendFormat("EndDate: \t\t{0}\r\n", EndDate);
            return builder.ToString();
        }
    }
}
