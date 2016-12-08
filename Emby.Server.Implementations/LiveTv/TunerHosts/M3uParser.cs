using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class M3uParser
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;

        public M3uParser(ILogger logger, IFileSystem fileSystem, IHttpClient httpClient, IServerApplicationHost appHost)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _appHost = appHost;
        }

        public async Task<List<M3UChannel>> Parse(string url, string channelIdPrefix, string tunerHostId, CancellationToken cancellationToken)
        {
            var urlHash = url.GetMD5().ToString("N");

            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(url, cancellationToken).ConfigureAwait(false)))
            {
                return GetChannels(reader, urlHash, channelIdPrefix, tunerHostId);
            }
        }

        public Task<Stream> GetListingsStream(string url, CancellationToken cancellationToken)
        {
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return _httpClient.Get(new HttpRequestOptions
                {
                    Url = url,
                    CancellationToken = cancellationToken,
                    // Some data providers will require a user agent
                    UserAgent = _appHost.FriendlyName + "/" + _appHost.ApplicationVersion
                });
            }
            return Task.FromResult(_fileSystem.OpenRead(url));
        }

        const string ExtInfPrefix = "#EXTINF:";
        private List<M3UChannel> GetChannels(StreamReader reader, string urlHash, string channelIdPrefix, string tunerHostId)
        {
            var channels = new List<M3UChannel>();
            string line;
            string extInf = "";
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (line.StartsWith(ExtInfPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    extInf = line.Substring(ExtInfPrefix.Length).Trim();
                    _logger.Info("Found m3u channel: {0}", extInf);
                }
                else if (!string.IsNullOrWhiteSpace(extInf) && !line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                {
                    var channel = GetChannelnfo(extInf, tunerHostId, line);
                    channel.Id = channelIdPrefix + urlHash + line.GetMD5().ToString("N");
                    channel.Path = line;
                    channels.Add(channel);
                    extInf = "";
                }
            }
            return channels;
        }

        private M3UChannel GetChannelnfo(string extInf, string tunerHostId, string mediaUrl)
        {
            var channel = new M3UChannel();
            channel.TunerHostId = tunerHostId;

            extInf = extInf.Trim();

            string remaining;
            var attributes = ParseExtInf(extInf, out remaining);
            extInf = remaining;

            string value;
            if (attributes.TryGetValue("tvg-logo", out value))
            {
                channel.ImageUrl = value;
            }

            channel.Name = GetChannelName(extInf, attributes);
            channel.Number = GetChannelNumber(extInf, attributes, mediaUrl);

            return channel;
        }

        private string GetChannelNumber(string extInf, Dictionary<string, string> attributes, string mediaUrl)
        {
            var nameParts = extInf.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts.Last().Trim() : null;

            var numberString = nameParts[0];

            //Check for channel number with the format from SatIp
            int number;
            if (!string.IsNullOrWhiteSpace(nameInExtInf))
            {
                var numberIndex = nameInExtInf.IndexOf('.');
                if (numberIndex > 0)
                {
                    if (int.TryParse(nameInExtInf.Substring(0, numberIndex), out number))
                    {
                        numberString = number.ToString();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }

            if (string.IsNullOrWhiteSpace(numberString) ||
                string.Equals(numberString, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(numberString, "0", StringComparison.OrdinalIgnoreCase))
            {
                string value;
                if (attributes.TryGetValue("tvg-id", out value))
                {
                    numberString = value;
                }
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }

            if (string.IsNullOrWhiteSpace(numberString) ||
                string.Equals(numberString, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(numberString, "0", StringComparison.OrdinalIgnoreCase))
            {
                string value;
                if (attributes.TryGetValue("channel-id", out value))
                {
                    numberString = value;
                }
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }

            if (string.IsNullOrWhiteSpace(numberString) ||
                string.Equals(numberString, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(numberString, "0", StringComparison.OrdinalIgnoreCase))
            {
                numberString = null;
            }

            if (string.IsNullOrWhiteSpace(numberString))
            {
                if (string.IsNullOrWhiteSpace(mediaUrl))
                {
                    numberString = null;
                }
                else
                {
                    numberString = Path.GetFileNameWithoutExtension(mediaUrl.Split('/').Last());

                    double value;
                    if (!double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        numberString = null;
                    }
                }
            }

            return numberString;
        }

        private string GetChannelName(string extInf, Dictionary<string, string> attributes)
        {
            var nameParts = extInf.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts.Last().Trim() : null;

            //Check for channel number with the format from SatIp
            int number;
            if (!string.IsNullOrWhiteSpace(nameInExtInf))
            {
                var numberIndex = nameInExtInf.IndexOf('.');
                if (numberIndex > 0)
                {
                    if (int.TryParse(nameInExtInf.Substring(0, numberIndex), out number))
                    {
                        //channel.Number = number.ToString();
                        nameInExtInf = nameInExtInf.Substring(numberIndex + 1);
                    }
                }
            }

            string name;
            attributes.TryGetValue("tvg-name", out name);

            if (string.IsNullOrWhiteSpace(name))
            {
                name = nameInExtInf;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                attributes.TryGetValue("tvg-id", out name);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = null;
            }

            return name;
        }

        private Dictionary<string, string> ParseExtInf(string line, out string remaining)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var reg = new Regex(@"([a-z0-9\-_]+)=\""([^""]+)\""", RegexOptions.IgnoreCase);
            var matches = reg.Matches(line);
            var minIndex = int.MaxValue;
            foreach (Match match in matches)
            {
                dict[match.Groups[1].Value] = match.Groups[2].Value;
                minIndex = Math.Min(minIndex, match.Index);
            }

            if (minIndex > 0 && minIndex < line.Length)
            {
                line = line.Substring(0, minIndex);
            }

            remaining = line;

            return dict;
        }
    }


    public class M3UChannel : ChannelInfo
    {
        public string Path { get; set; }
    }
}