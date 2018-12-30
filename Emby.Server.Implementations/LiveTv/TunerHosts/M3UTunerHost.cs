using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using System.IO;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class M3UTunerHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;
        private readonly IEnvironmentInfo _environment;
        private readonly INetworkManager _networkManager;
        private readonly IMediaSourceManager _mediaSourceManager;

        public M3UTunerHost(IServerConfigurationManager config, IMediaSourceManager mediaSourceManager, ILogger logger, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IHttpClient httpClient, IServerApplicationHost appHost, IEnvironmentInfo environment, INetworkManager networkManager) : base(config, logger, jsonSerializer, mediaEncoder, fileSystem)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _environment = environment;
            _networkManager = networkManager;
            _mediaSourceManager = mediaSourceManager;
        }

        public override string Type
        {
            get { return "m3u"; }
        }

        public virtual string Name
        {
            get { return "M3U Tuner"; }
        }

        private string GetFullChannelIdPrefix(TunerHostInfo info)
        {
            return ChannelIdPrefix + info.Url.GetMD5().ToString("N");
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var channelIdPrefix = GetFullChannelIdPrefix(info);

            var result = await new M3uParser(Logger, FileSystem, _httpClient, _appHost).Parse(info.Url, channelIdPrefix, info.Id, cancellationToken).ConfigureAwait(false);

            return result.Cast<ChannelInfo>().ToList();
        }

        public Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            var list = GetTunerHosts()
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

        private string[] _disallowedSharedStreamExtensions = new string[]
        {
            ".mkv",
            ".mp4",
            ".m3u8",
            ".mpd"
        };

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo info, ChannelInfo channelInfo, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            var tunerCount = info.TunerCount;

            if (tunerCount > 0)
            {
                var tunerHostId = info.Id;
                var liveStreams = currentLiveStreams.Where(i => string.Equals(i.TunerHostId, tunerHostId, StringComparison.OrdinalIgnoreCase)).ToList();

                if (liveStreams.Count >= tunerCount)
                {
                    throw new LiveTvConflictException("M3U simultaneous stream limit has been reached.");
                }
            }

            var sources = await GetChannelStreamMediaSources(info, channelInfo, cancellationToken).ConfigureAwait(false);

            var mediaSource = sources.First();

            if (mediaSource.Protocol == MediaProtocol.Http && !mediaSource.RequiresLooping)
            {
                var extension = Path.GetExtension(mediaSource.Path) ?? string.Empty;

                if (!_disallowedSharedStreamExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return new SharedHttpStream(mediaSource, info, streamId, FileSystem, _httpClient, Logger, Config.ApplicationPaths, _appHost);
                }
            }

            return new LiveStream(mediaSource, info, FileSystem, Logger, Config.ApplicationPaths);
        }

        public async Task Validate(TunerHostInfo info)
        {
            using (var stream = await new M3uParser(Logger, FileSystem, _httpClient, _appHost).GetListingsStream(info.Url, CancellationToken.None).ConfigureAwait(false))
            {

            }
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, ChannelInfo channelInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<MediaSourceInfo> { CreateMediaSourceInfo(info, channelInfo) });
        }

        protected virtual MediaSourceInfo CreateMediaSourceInfo(TunerHostInfo info, ChannelInfo channel)
        {
            var path = channel.Path;

            var supportsDirectPlay = !info.EnableStreamLooping && info.TunerCount == 0;
            var supportsDirectStream = !info.EnableStreamLooping;

            var protocol = _mediaSourceManager.GetPathProtocol(path);

            Uri uri;
            var isRemote = true;
            if (Uri.TryCreate(path, UriKind.Absolute, out uri))
            {
                isRemote = !_networkManager.IsInLocalNetwork(uri.Host);
            }

            var httpHeaders = new Dictionary<string, string>();

            if (protocol == MediaProtocol.Http)
            {
                // Use user-defined user-agent. If there isn't one, make it look like a browser.
                httpHeaders["User-Agent"] = string.IsNullOrWhiteSpace(info.UserAgent) ?
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.85 Safari/537.36" :
                    info.UserAgent;
            }

            var mediaSource = new MediaSourceInfo
            {
                Path = path,
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
                RequiresOpening = true,
                RequiresClosing = true,
                RequiresLooping = info.EnableStreamLooping,

                ReadAtNativeFramerate = false,

                Id = channel.Path.GetMD5().ToString("N"),
                IsInfiniteStream = true,
                IsRemote = isRemote,

                IgnoreDts = true,
                SupportsDirectPlay = supportsDirectPlay,
                SupportsDirectStream = supportsDirectStream,

                RequiredHttpHeaders = httpHeaders
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
        }

        public Task<List<TunerHostInfo>> DiscoverDevices(int discoveryDurationMs, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<TunerHostInfo>());
        }
    }
}
