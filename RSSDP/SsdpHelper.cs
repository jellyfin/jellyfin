using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Text;

namespace RSSDP
{
    public class SsdpHelper
    {
        private readonly ITextEncoding _encoding;

        public SsdpHelper(ITextEncoding encoding)
        {
            _encoding = encoding;
        }

        public SsdpMessageInfo ParseSsdpResponse(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new StreamReader(ms, _encoding.GetASCIIEncoding()))
                {
                    var proto = (reader.ReadLine() ?? string.Empty).Trim();
                    var method = proto.Split(new[] { ' ' }, 2)[0];
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line))
                        {
                            break;
                        }
                        var parts = line.Split(new[] { ':' }, 2);

                        if (parts.Length >= 2)
                        {
                            headers[parts[0]] = parts[1].Trim();
                        }
                    }

                    return new SsdpMessageInfo
                    {
                        Method = method,
                        Headers = headers,
                        Message = data
                    };
                }
            }
        }

        public static string BuildMessage(string header, Dictionary<string, string> values)
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

    public class SsdpMessageInfo
    {
        public string Method { get; set; }

        public IpEndPointInfo EndPoint { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public IpEndPointInfo LocalEndPoint { get; set; }
        public byte[] Message { get; set; }

        public SsdpMessageInfo()
        {
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
