using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpHelper
    {
        public static SsdpMessageEventArgs ParseSsdpResponse(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                using (var reader = new StreamReader(ms, Encoding.ASCII))
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

                    return new SsdpMessageEventArgs
                    {
                        Method = method,
                        Headers = headers
                    };
                }
            }
        }
    }
}
