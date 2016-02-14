using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public class M3UTunerHost : BaseTunerHost, ITunerHost
    {
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;

        public M3UTunerHost(IConfigurationManager config, ILogger logger, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IHttpClient httpClient)
            : base(config, logger, jsonSerializer, mediaEncoder)
        {
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public override string Type
        {
            get { return "m3u"; }
        }

        public string Name
        {
            get { return "M3U Tuner"; }
        }

        private const string ChannelIdPrefix = "m3u_";

        protected override async Task<IEnumerable<ChannelInfo>> GetChannelsInternal(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var urlHash = info.Url.GetMD5().ToString("N");

            // Read the file and display it line by line.
            using (var reader = new StreamReader(await GetListingsStream(info, cancellationToken).ConfigureAwait(false)))
            {
                return GetChannels(reader, urlHash);
            }
        }

        private List<M3UChannel> GetChannels(StreamReader reader, string urlHash)
        {
            var channels = new List<M3UChannel>();

            string channnelName = null;
            string channelNumber = null;
            string line;

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
                    Logger.Info("Found m3u channel: {0}", line);
                    var parts = line.Split(new[] { ',' }, 2);
                    channelNumber = parts[0];
                    channnelName = parts[1];
                }
                else if (!string.IsNullOrWhiteSpace(channelNumber))
                {
                    channels.Add(new M3UChannel
                    {
                        Name = channnelName,
                        Number = channelNumber,
                        Id = ChannelIdPrefix + urlHash + line.GetMD5().ToString("N"),
                        Path = line
                    });

                    channelNumber = null;
                    channnelName = null;
                }
            }
            return channels;
        }

        public Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            var list = GetConfiguration().TunerHosts
            .Where(i => i.IsEnabled && string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase))
            .Select(i => new LiveTvTunerInfo()
            {
                Name = Name,
                SourceType = Type,
                Status = LiveTvTunerStatus.Available,
                Id = i.Url.GetMD5().ToString("N"),
                Url = i.Url
            })
            .ToList();

            return Task.FromResult(list);
        }

        protected override async Task<MediaSourceInfo> GetChannelStream(TunerHostInfo info, string channelId, string streamId, CancellationToken cancellationToken)
        {
            var sources = await GetChannelStreamMediaSources(info, channelId, cancellationToken).ConfigureAwait(false);

            return sources.First();
        }

        class M3UChannel : ChannelInfo
        {
            public string Path { get; set; }

            public M3UChannel()
            {
            }
        }

        public async Task Validate(TunerHostInfo info)
        {
            using (var stream = await GetListingsStream(info, CancellationToken.None).ConfigureAwait(false))
            {

            }
        }

        protected override bool IsValidChannelId(string channelId)
        {
            return channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private Task<Stream> GetListingsStream(TunerHostInfo info, CancellationToken cancellationToken)
        {
            if (info.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return _httpClient.Get(info.Url, cancellationToken);
            }
            return Task.FromResult(_fileSystem.OpenRead(info.Url));
        }

        protected override async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, string channelId, CancellationToken cancellationToken)
        {
            var urlHash = info.Url.GetMD5().ToString("N");
            var prefix = ChannelIdPrefix + urlHash;
            if (!channelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var channels = await GetChannels(info, true, cancellationToken).ConfigureAwait(false);
            var m3uchannels = channels.Cast<M3UChannel>();
            var channel = m3uchannels.FirstOrDefault(c => string.Equals(c.Id, channelId, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
            {
                var path = channel.Path;
                MediaProtocol protocol = MediaProtocol.File;
                if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    protocol = MediaProtocol.Http;
                }
                else if (path.StartsWith("rtmp", StringComparison.OrdinalIgnoreCase))
                {
                    protocol = MediaProtocol.Rtmp;
                }
                else if (path.StartsWith("rtsp", StringComparison.OrdinalIgnoreCase))
                {
                    protocol = MediaProtocol.Rtsp;
                }

                var mediaSource = new MediaSourceInfo
                {
                    Path = channel.Path,
                    Protocol = protocol,
                    MediaStreams = new List<MediaStream>
                    {
                        new MediaStream
                        {
                            Type = MediaStreamType.Video,
                            // Set the index to -1 because we don't know the exact index of the video stream within the container
                            Index = -1,
                            IsInterlaced = true
                        },
                        new MediaStream
                        {
                            Type = MediaStreamType.Audio,
                            // Set the index to -1 because we don't know the exact index of the audio stream within the container
                            Index = -1

                        }
                    },
                    RequiresOpening = false,
                    RequiresClosing = false
                };

                return new List<MediaSourceInfo> { mediaSource };
            }
            return new List<MediaSourceInfo> { };
        }

        protected override Task<bool> IsAvailableInternal(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
