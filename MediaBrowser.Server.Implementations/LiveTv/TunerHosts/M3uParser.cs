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
                return _httpClient.Get(url, cancellationToken);
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
                    var channel = GetChannelnfo(extInf, tunerHostId);
                    channel.Id = channelIdPrefix + urlHash + line.GetMD5().ToString("N");
                    channel.Path = line;
                    channels.Add(channel);
                    extInf = "";
                }
            }
            return channels;
        }
        private M3UChannel GetChannelnfo(string extInf, string tunerHostId)
        {
            var titleIndex = extInf.LastIndexOf(',');
            var channel = new M3UChannel();
            channel.TunerHostId = tunerHostId;

            channel.Number = extInf.Trim().Split(' ')[0] ?? "0";
            channel.Name = extInf.Substring(titleIndex + 1);
            
            if(channel.Number == "-1") { channel.Number = "0"; }         

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
            channel.ImageUrl = FindProperty("tvg-logo", extInf, null);
            channel.Number = FindProperty("tvg-id", extInf, channel.Number);
            channel.Number = FindProperty("channel-id", extInf, channel.Number);
            channel.Name = FindProperty("tvg-name", extInf, channel.Name);
            channel.Name = FindProperty("tvg-id", extInf, channel.Name);
            return channel;

        }
        private string FindProperty(string property, string properties, string defaultResult = "")
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