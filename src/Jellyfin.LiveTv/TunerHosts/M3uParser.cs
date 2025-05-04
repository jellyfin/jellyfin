#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.TunerHosts
{
    public partial class M3uParser
    {
        private const string ExtInfPrefix = "#EXTINF:";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public M3uParser(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [GeneratedRegex(@"([a-z0-9\-_]+)=\""([^""]+)\""", RegexOptions.IgnoreCase, "en-US")]
        private static partial Regex KeyValueRegex();

        public async Task<List<ChannelInfo>> Parse(TunerHostInfo info, string channelIdPrefix, CancellationToken cancellationToken)
        {
            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(info, cancellationToken).ConfigureAwait(false)))
            {
                return await GetChannelsAsync(reader, channelIdPrefix, info.Id).ConfigureAwait(false);
            }
        }

        public async Task<Stream> GetListingsStream(TunerHostInfo info, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(info);

            if (!info.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return AsyncFile.OpenRead(info.Url);
            }

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, info.Url);
            if (!string.IsNullOrEmpty(info.UserAgent))
            {
                requestMessage.Headers.UserAgent.TryParseAdd(info.UserAgent);
            }

            // Set HttpCompletionOption.ResponseHeadersRead to prevent timeouts on larger files
            var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task<List<ChannelInfo>> GetChannelsAsync(TextReader reader, string channelIdPrefix, string tunerHostId)
        {
            var channels = new List<ChannelInfo>();
            string extInf = string.Empty;

            await foreach (var line in reader.ReadAllLinesAsync().ConfigureAwait(false))
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    continue;
                }

                if (trimmedLine.StartsWith("#EXTM3U", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trimmedLine.StartsWith(ExtInfPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    extInf = trimmedLine.Substring(ExtInfPrefix.Length).Trim();
                }
                else if (!string.IsNullOrWhiteSpace(extInf) && !trimmedLine.StartsWith('#'))
                {
                    var channel = GetChannelInfo(extInf, tunerHostId, trimmedLine);
                    channel.Id = channelIdPrefix + trimmedLine.GetMD5().ToString("N", CultureInfo.InvariantCulture);

                    channel.Path = trimmedLine;
                    channels.Add(channel);
                    _logger.LogInformation("Parsed channel: {ChannelName}", channel.Name);
                    extInf = string.Empty;
                }
            }

            return channels;
        }

        private ChannelInfo GetChannelInfo(string extInf, string tunerHostId, string mediaUrl)
        {
            var channel = new ChannelInfo()
            {
                TunerHostId = tunerHostId
            };

            extInf = extInf.Trim();

            var attributes = ParseExtInf(extInf, out string remaining);
            extInf = remaining;

            if (attributes.TryGetValue("tvg-logo", out string tvgLogo))
            {
                channel.ImageUrl = tvgLogo;
            }
            else if (attributes.TryGetValue("logo", out string logo))
            {
                channel.ImageUrl = logo;
            }

            if (attributes.TryGetValue("group-title", out string groupTitle))
            {
                channel.ChannelGroup = groupTitle;
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
                channel.Id = string.Join('_', channelIdValues);
            }

            return channel;
        }

        private string GetChannelNumber(string extInf, Dictionary<string, string> attributes, string mediaUrl)
        {
            var nameParts = extInf.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var nameInExtInf = nameParts.Length > 1 ? nameParts[^1].AsSpan().Trim() : ReadOnlySpan<char>.Empty;

            string numberString = null;

            if (attributes.TryGetValue("tvg-chno", out var attributeValue)
                && double.TryParse(attributeValue, CultureInfo.InvariantCulture, out _))
            {
                numberString = attributeValue;
            }

            if (!IsValidChannelNumber(numberString))
            {
                if (attributes.TryGetValue("tvg-id", out attributeValue))
                {
                    if (double.TryParse(attributeValue, CultureInfo.InvariantCulture, out _))
                    {
                        numberString = attributeValue;
                    }
                    else if (attributes.TryGetValue("channel-id", out attributeValue)
                        && double.TryParse(attributeValue, CultureInfo.InvariantCulture, out _))
                    {
                        numberString = attributeValue;
                    }
                }

                if (string.IsNullOrWhiteSpace(numberString))
                {
                    // Using this as a fallback now as this leads to Problems with channels like "5 USA"
                    // where 5 isn't meant to be the channel number
                    // Check for channel number with the format from SatIp
                    // #EXTINF:0,84. VOX Schweiz
                    // #EXTINF:0,84.0 - VOX Schweiz
                    if (!nameInExtInf.IsEmpty && !nameInExtInf.IsWhiteSpace())
                    {
                        var numberIndex = nameInExtInf.IndexOf(' ');
                        if (numberIndex > 0)
                        {
                            var numberPart = nameInExtInf.Slice(0, numberIndex).Trim(new[] { ' ', '.' });

                            if (double.TryParse(numberPart, CultureInfo.InvariantCulture, out _))
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
                        numberString = Path.GetFileNameWithoutExtension(mediaUrl.AsSpan().RightPart('/')).ToString();

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
            if (string.IsNullOrWhiteSpace(numberString)
                || string.Equals(numberString, "-1", StringComparison.Ordinal)
                || string.Equals(numberString, "0", StringComparison.Ordinal))
            {
                return false;
            }

            return double.TryParse(numberString, CultureInfo.InvariantCulture, out _);
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
                var numberIndex = nameInExtInf.IndexOf(' ', StringComparison.Ordinal);
                if (numberIndex > 0)
                {
                    var numberPart = nameInExtInf.AsSpan(0, numberIndex).Trim(new[] { ' ', '.' });

                    if (double.TryParse(numberPart, CultureInfo.InvariantCulture, out _))
                    {
                        // channel.Number = number.ToString();
                        nameInExtInf = nameInExtInf.AsSpan(numberIndex + 1).Trim(new[] { ' ', '-' }).ToString();
                    }
                }
            }

            string name = nameInExtInf;

            if (string.IsNullOrWhiteSpace(name))
            {
                attributes.TryGetValue("tvg-name", out name);
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

            var matches = KeyValueRegex().Matches(line);

            remaining = line;

            foreach (Match match in matches)
            {
                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;

                dict[key] = value;
                remaining = remaining.Replace(key + "=\"" + value + "\"", string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return dict;
        }
    }
}
