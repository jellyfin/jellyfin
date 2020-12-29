#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class M3uParser
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerApplicationHost _appHost;

        public M3uParser(ILogger logger, IHttpClientFactory httpClientFactory, IServerApplicationHost appHost)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _appHost = appHost;
        }

        public async Task<List<ChannelInfo>> Parse(TunerHostInfo info, string channelIdPrefix, CancellationToken cancellationToken)
        {
            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(info, cancellationToken).ConfigureAwait(false)))
            {
                return GetChannels(reader, channelIdPrefix, info.Id);
            }
        }

        public List<ChannelInfo> ParseString(string text, string channelIdPrefix, string tunerHostId)
        {
            // Read the file and display it line by line.
            using (var reader = new StringReader(text))
            {
                return GetChannels(reader, channelIdPrefix, tunerHostId);
            }
        }

        public async Task<Stream> GetListingsStream(TunerHostInfo info, CancellationToken cancellationToken)
        {
            if (info.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, info.Url);
                if (!string.IsNullOrEmpty(info.UserAgent))
                {
                    requestMessage.Headers.UserAgent.TryParseAdd(info.UserAgent);
                }

                var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .SendAsync(requestMessage, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            }

            return File.OpenRead(info.Url);
        }

        private const string ExtInfPrefix = "#EXTINF:";

        private List<ChannelInfo> GetChannels(TextReader reader, string channelIdPrefix, string tunerHostId)
        {
            var channels = new List<ChannelInfo>();
            string line;
            string extInf = string.Empty;

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
                    _logger.LogInformation("Found m3u channel: {0}", extInf);
                }
                else if (!string.IsNullOrWhiteSpace(extInf) && !line.StartsWith('#'))
                {
                    var channel = GetChannelnfo(extInf, tunerHostId, line);
                    if (string.IsNullOrWhiteSpace(channel.Id))
                    {
                        channel.Id = channelIdPrefix + line.GetMD5().ToString("N", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        channel.Id = channelIdPrefix + channel.Id.GetMD5().ToString("N", CultureInfo.InvariantCulture);
                    }

                    channel.Path = line;
                    channels.Add(channel);
                    extInf = string.Empty;
                }
            }

            return channels;
        }

        private ChannelInfo GetChannelnfo(string extInf, string tunerHostId, string mediaUrl)
        {
            var channel = new ChannelInfo()
            {
                TunerHostId = tunerHostId
            };

            extInf = extInf.Trim();

            var attributes = ParseExtInf(extInf, out string remaining);
            extInf = remaining;

            if (attributes.TryGetValue("tvg-logo", out string value))
            {
                channel.ImageUrl = value;
            }

            channel.Name = GetChannelName(extInf, attributes);
            channel.Number = GetChannelNumber(extInf, attributes, mediaUrl);

            attributes.TryGetValue("tvg-id", out string tvgId);

            attributes.TryGetValue("channel-id", out string channelId);

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
                channel.Id = string.Join("_", channelIdValues);
            }

            return channel;
        }

        private string GetChannelNumber(string extInf, Dictionary<string, string> attributes, string mediaUrl)
        {
            var nameParts = extInf.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts[^1].AsSpan().Trim() : ReadOnlySpan<char>.Empty;

            string numberString = null;
            string attributeValue;

            if (attributes.TryGetValue("tvg-chno", out attributeValue))
            {
                if (double.TryParse(attributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    numberString = attributeValue;
                }
            }

            if (!IsValidChannelNumber(numberString))
            {
                if (attributes.TryGetValue("tvg-id", out attributeValue))
                {
                    if (double.TryParse(attributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        numberString = attributeValue;
                    }
                    else if (attributes.TryGetValue("channel-id", out attributeValue))
                    {
                        if (double.TryParse(attributeValue, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                        {
                            numberString = attributeValue;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(numberString))
                {
                    // Using this as a fallback now as this leads to Problems with channels like "5 USA"
                    // where 5 isn't ment to be the channel number
                    // Check for channel number with the format from SatIp
                    // #EXTINF:0,84. VOX Schweiz
                    // #EXTINF:0,84.0 - VOX Schweiz
                    if (!nameInExtInf.IsEmpty && !nameInExtInf.IsWhiteSpace())
                    {
                        var numberIndex = nameInExtInf.IndexOf(' ');
                        if (numberIndex > 0)
                        {
                            var numberPart = nameInExtInf.Slice(0, numberIndex).Trim(new[] { ' ', '.' });

                            if (double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                            {
                                numberString = numberPart.ToString();
                            }
                        }
                    }
                }
            }

            if (!IsValidChannelNumber(numberString))
            {
                numberString = null;
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                numberString = numberString.Trim();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(mediaUrl))
                {
                    numberString = null;
                }
                else
                {
                    try
                    {
                        numberString = Path.GetFileNameWithoutExtension(mediaUrl.Split('/')[^1]);

                        if (!IsValidChannelNumber(numberString))
                        {
                            numberString = null;
                        }
                    }
                    catch
                    {
                        // Seeing occasional argument exception here
                        numberString = null;
                    }
                }
            }

            return numberString;
        }

        private static bool IsValidChannelNumber(string numberString)
        {
            if (string.IsNullOrWhiteSpace(numberString) ||
                string.Equals(numberString, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(numberString, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!double.TryParse(numberString, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }

            return true;
        }

        private static string GetChannelName(string extInf, Dictionary<string, string> attributes)
        {
            var nameParts = extInf.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts[^1].Trim() : null;

            // Check for channel number with the format from SatIp
            // #EXTINF:0,84. VOX Schweiz
            // #EXTINF:0,84.0 - VOX Schweiz
            if (!string.IsNullOrWhiteSpace(nameInExtInf))
            {
                var numberIndex = nameInExtInf.IndexOf(' ');
                if (numberIndex > 0)
                {
                    var numberPart = nameInExtInf.Substring(0, numberIndex).Trim(new[] { ' ', '.' });

                    if (double.TryParse(numberPart, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    {
                        // channel.Number = number.ToString();
                        nameInExtInf = nameInExtInf.Substring(numberIndex + 1).Trim(new[] { ' ', '-' });
                    }
                }
            }

            attributes.TryGetValue("tvg-name", out string name);

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

        private static Dictionary<string, string> ParseExtInf(string line, out string remaining)
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
}
