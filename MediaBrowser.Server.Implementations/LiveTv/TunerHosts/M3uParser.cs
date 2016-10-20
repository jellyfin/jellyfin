using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
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

                if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                {
                    extInf = line.Substring(8).Trim();
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
            var titleIndex = extInf.LastIndexOf(',');
            var channel = new M3UChannel();
            channel.TunerHostId = tunerHostId;

            channel.Number = extInf.Trim().Split(' ')[0] ?? "0";
            channel.Name = extInf.Substring(titleIndex + 1);

            //Check for channel number with the format from SatIp            
            int number;                   
            var numberIndex = channel.Name.IndexOf('.');
            if (numberIndex > 0)
            {
                if (int.TryParse(channel.Name.Substring(0, numberIndex), out number))
                {
                    channel.Number = number.ToString();
                    channel.Name = channel.Name.Substring(numberIndex + 1);
                }
            }

            if (string.Equals(channel.Number, "-1", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(mediaUrl))
            {
                channel.Number = Path.GetFileNameWithoutExtension(mediaUrl.Split('/').Last());
            }

            if (string.Equals(channel.Number, "-1", StringComparison.OrdinalIgnoreCase))
            {
                channel.Number = "0";
            }

            channel.ImageUrl = FindProperty("tvg-logo", extInf);

            var name = FindProperty("tvg-name", extInf);
            if (string.IsNullOrWhiteSpace(name))
            {
                 name = FindProperty("tvg-id", extInf);
            }

            channel.Name = name;

            var numberString = FindProperty("tvg-id", extInf);
            if (string.IsNullOrWhiteSpace(numberString))
            {
                numberString = FindProperty("channel-id", extInf);
            }

            if (!string.IsNullOrWhiteSpace(numberString))
            {
                channel.Number = numberString;
            }

            return channel;

        }
        private string FindProperty(string property, string properties)
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
            return null;
        }
    }


    public class M3UChannel : ChannelInfo
    {
        public string Path { get; set; }
    }
}