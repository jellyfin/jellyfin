using System.Text;

namespace Jellyfin.XmlTv.Entities
{
    /// <summary>
    /// Describes the rating (certification) applied to a program
    /// </summary>
    /// <remarks>Example XML:
    /// </remarks>
    public class XmlTvRating
    {
        /// <summary>
        /// The literal name of the rating system
        /// </summary>
        /// <example>MPAA</example>
        public string System { get; set; }

        /// <summary>
        /// Describes the rating using the system specificed
        /// </summary>
        /// <example>TV-14</example>
        public string Value { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(Value))
            {
                builder.Append(Value);
            }

            if (!string.IsNullOrEmpty(System))
            {
                builder.AppendFormat(" ({0})", System);
            }

            return builder.ToString();
        }
    }
}
