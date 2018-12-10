using System;
using System.Text;

namespace Emby.XmlTv.Entities
{
    public class XmlTvIcon
    {
        public String Source { get; set; }
        public Int32? Width { get; set; }
        public Int32? Height { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("Source: {0}", Source);
            if (Width.HasValue)
            {
                builder.AppendFormat(", Width: {0}", Width);
            }
            if (Height.HasValue)
            {
                builder.AppendFormat(", Height: {0}", Height);
            }

            return builder.ToString();
        }
    }
}
