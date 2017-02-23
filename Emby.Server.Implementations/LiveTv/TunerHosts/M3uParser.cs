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
using MediaBrowser.Model.Extensions;

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

        public async Task<List<M3UChannel>> Parse(string url, string channelIdPrefix, string tunerHostId, bool enableStreamUrlAsIdentifier, CancellationToken cancellationToken)
        {
            var urlHash = url.GetMD5().ToString("N");

            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(url, cancellationToken).ConfigureAwait(false)))
            {
                return GetChannels(reader, urlHash, channelIdPrefix, tunerHostId, enableStreamUrlAsIdentifier);
            }
        }

        public List<M3UChannel> ParseString(string text, string channelIdPrefix, string tunerHostId)
        {
            var urlHash = "text".GetMD5().ToString("N");

            // Read the file and display it line by line.
            using (var reader = new StringReader(text))
            {
                return GetChannels(reader, urlHash, channelIdPrefix, tunerHostId, false);
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
        private List<M3UChannel> GetChannels(TextReader reader, string urlHash, string channelIdPrefix, string tunerHostId, bool enableStreamUrlAsIdentifier)
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
                    if (string.IsNullOrWhiteSpace(channel.Id) || enableStreamUrlAsIdentifier)
                    {
                        channel.Id = channelIdPrefix + urlHash + line.GetMD5().ToString("N");
                    }
                    else
                    {
                        channel.Id = channelIdPrefix + urlHash + channel.Id.GetMD5().ToString("N");
                    }

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

            string tvgId;
            attributes.TryGetValue("tvg-id", out tvgId);

            string channelId;
            attributes.TryGetValue("channel-id", out channelId);

            channel.TunerChannelId = string.IsNullOrWhiteSpace(tvgId) ? channelId : tvgId;

            var channelIdValues = new List<string>();
            if (!string.IsNullOrWhiteSpace(channelId))
            {
                channelIdValues.Add(channelId);
            }
            if (!string.IsNullOrWhiteSpace(tvgId))
            {
                channelIdValues.Add(tvgId);
            }
            if (channelIdValues.Count > 0)
            {
                channel.Id = string.Join("_", channelIdValues.ToArray());
            }

            return channel;
        }

        private string GetChannelNumber(string extInf, Dictionary<string, string> attributes, string mediaUrl)
        {
            var nameParts = extInf.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts.Last().Trim() : null;

            string numberString = null;

            // Check for channel number with the format from SatIp
            // #EXTINF:0,84. VOX Schweiz
            // #EXTINF:0,84.0 - VOX Schweiz
            if (!string.IsNullOrWhiteSpace(nameInExtInf))
            {
                var numberIndex = nameInExtInf.IndexOf(' ');
                if (numberIndex > 0)
                {
                    var numberPart = nameInExtInf.Substring(0, numberIndex).Trim(new[] { ' ', '.' });

                    double number;
                    if (double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                    {
                        numberString = numberPart;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }

            if (!IsValidChannelNumber(numberString))
            {
                string value;
                if (attributes.TryGetValue("tvg-id", out value))
                {
                    double doubleValue;
                    if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out doubleValue))
                    {
                        numberString = value;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }

            if (!IsValidChannelNumber(numberString))
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

            if (!IsValidChannelNumber(numberString))
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

                    if (!IsValidChannelNumber(numberString))
                    {
                        numberString = null;
                    }
                }
            }

            return numberString;
        }

        private bool IsValidChannelNumber(string numberString)
        {
            if (string.IsNullOrWhiteSpace(numberString) ||
                string.Equals(numberString, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(numberString, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            double value;
            if (!double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return true;
        }

        private string GetChannelName(string extInf, Dictionary<string, string> attributes)
        {
            var nameParts = extInf.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts.Last().Trim() : null;

            // Check for channel number with the format from SatIp
            // #EXTINF:0,84. VOX Schweiz
            // #EXTINF:0,84.0 - VOX Schweiz
            if (!string.IsNullOrWhiteSpace(nameInExtInf))
            {
                var numberIndex = nameInExtInf.IndexOf(' ');
                if (numberIndex > 0)
                {
                    var numberPart = nameInExtInf.Substring(0, numberIndex).Trim(new[] { ' ', '.' });

                    double number;
                    if (double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
                    {
                        //channel.Number = number.ToString();
                        nameInExtInf = nameInExtInf.Substring(numberIndex + 1).Trim(new[] { ' ', '-' });
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

            remaining = line;

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;

                dict[match.Groups[1].Value] = match.Groups[2].Value;
                remaining = remaining.Replace(key + "=\"" + value + "\"", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return dict;
        }
    }


    public class M3UChannel : ChannelInfo
    {
        public string Path { get; set; }
    }
}