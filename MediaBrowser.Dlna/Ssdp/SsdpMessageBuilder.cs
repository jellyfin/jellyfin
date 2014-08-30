using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpMessageBuilder
    {
        public string BuildMessage(string header, Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            const string argFormat = "{0}: {1}\r\n";

            builder.AppendFormat("{0}\r\n", header);

            foreach (var pair in values)
            {
                builder.AppendFormat(argFormat, pair.Key, pair.Value);
            }

            builder.Append("\r\n");

            return builder.ToString();
        }
    }
}
