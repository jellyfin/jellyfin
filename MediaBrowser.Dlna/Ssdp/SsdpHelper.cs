using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Dlna.Ssdp
{
    public class SsdpHelper
    {
        /// <summary>
        /// Parses the socket response into a location Uri for the DeviceDescription.xml.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static Dictionary<string,string> ParseSsdpResponse(byte[] data)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new StreamReader(new MemoryStream(data), Encoding.ASCII))
            {
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        break;
                    }
                    var parts = line.Split(new[] { ':' }, 2);

                    if (parts.Length == 2)
                    {
                        headers[parts[0]] = parts[1].Trim();
                    }
                }
            }
            
            return headers;
        }
    }
}
