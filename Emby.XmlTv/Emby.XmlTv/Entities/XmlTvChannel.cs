using System;
using System.Text;

namespace Emby.XmlTv.Entities
{
    public class XmlTvChannel : IEquatable<XmlTvChannel>
    {
        public String Id { get; set; }
        public String DisplayName { get; set; }
        public String Number { get; set; }
        public string Url { get; set; }
        public XmlTvIcon Icon { get; set; }

        public bool Equals(XmlTvChannel other)
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
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return (Id.GetHashCode() * 17);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("{0} - {1} ", Id, DisplayName);

            if (!string.IsNullOrEmpty(Url))
            {
                builder.AppendFormat(" ({0})", Url);
            }

            return builder.ToString();
        }
    }
}