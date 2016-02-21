using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public class M3uParser
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;

        public M3uParser(ILogger logger, IFileSystem fileSystem, IHttpClient httpClient)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public async Task<List<M3UChannel>> Parse(string url, string channelIdPrefix, CancellationToken cancellationToken)
        {
            var urlHash = url.GetMD5().ToString("N");

            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(url, cancellationToken).ConfigureAwait(false)))
            {
                return GetChannels(reader, urlHash, channelIdPrefix);
            }
        }

        public Task<Stream> GetListingsStream(string url, CancellationToken cancellationToken)
        {
            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return _httpClient.Get(url, cancellationToken);
            }
            return Task.FromResult(_fileSystem.OpenRead(url));
        }

        private List<M3UChannel> GetChannels(StreamReader reader, string urlHash, string channelIdPrefix)
        {
            var channels = new List<M3UChannel>();

            string channnelName = null;
            string channelNumber = null;
            string line;
            string imageUrl = null;
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

                if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                {
                    line = line.Substring(8);
                    _logger.Info("Found m3u channel: {0}", line);
                    var parts = line.Split(new[] { ',' }, 2);
                    channelNumber = parts[0].Trim().Split(' ')[0] ?? "0";
                    channnelName = FindProperty("tvg-name", line, parts[1]);
                    imageUrl = FindProperty("tvg-logo", line, null);
                }
                else if (!string.IsNullOrWhiteSpace(channelNumber))
                {
                    channels.Add(new M3UChannel
                    {
                        Name = channnelName,
                        Number = channelNumber,
                        Id = channelIdPrefix + urlHash + line.GetMD5().ToString("N"),
                        ImageUrl = imageUrl
                    });

                    imageUrl = null;
                    channelNumber = null;
                    channnelName = null;
                }
            }
            return channels;
        }
        public string FindProperty(string property, string properties, string defaultResult = "")
        {
            var reg = new Regex(@"([a-z0-9\-_]+)=\""([^""]+)\""", RegexOptions.IgnoreCase);
            var matches = reg.Matches(properties);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value == property)
                {
                    return match.Groups[2].Value;
                }
            }
            return defaultResult;
        }
    }


    public class M3UChannel : ChannelInfo
    {
        public string Path { get; set; }
    }
}