#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.LiveTv.TunerHosts
{
    public class M3UTunerHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private static readonly string[] _mimeTypesCanShareHttpStream = ["video/MP2T"];
        private static readonly string[] _extensionsCanShareHttpStream = [".ts", ".tsv", ".m2t"];

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly INetworkManager _networkManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IStreamHelper _streamHelper;

        public M3UTunerHost(
            IServerConfigurationManager config,
            IMediaSourceManager mediaSourceManager,
            ILogger<M3UTunerHost> logger,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IServerApplicationHost appHost,
            INetworkManager networkManager,
            IStreamHelper streamHelper)
            : base(config, logger, fileSystem)
        {
            _httpClientFactory = httpClientFactory;
            _appHost = appHost;
            _networkManager = networkManager;
            _mediaSourceManager = mediaSourceManager;
            _streamHelper = streamHelper;
        }

        public override string Type => "m3u";

        public virtual string Name => "M3U Tuner";

        private string GetFullChannelIdPrefix(TunerHostInfo info)
        {
            return ChannelIdPrefix + info.Url.GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
        {
            var channelIdPrefix = GetFullChannelIdPrefix(tuner);

            return await new M3uParser(Logger, _httpClientFactory)
                .Parse(tuner, channelIdPrefix, cancellationToken)
                .ConfigureAwait(false);
        }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo tunerHost, ChannelInfo channel, string streamId, IList<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            var tunerCount = tunerHost.TunerCount;

            if (tunerCount > 0)
            {
                var tunerHostId = tunerHost.Id;
                var liveStreams = currentLiveStreams.Where(i => string.Equals(i.TunerHostId, tunerHostId, StringComparison.OrdinalIgnoreCase));

                if (liveStreams.Count() >= tunerCount)
                {
                    throw new LiveTvConflictException("M3U simultaneous stream limit has been reached.");
                }
            }

            var sources = await GetChannelStreamMediaSources(tunerHost, channel, cancellationToken).ConfigureAwait(false);

            var mediaSource = sources[0];

            if (tunerHost.AllowStreamSharing && mediaSource.Protocol == MediaProtocol.Http && !mediaSource.RequiresLooping)
            {
                var extension = Path.GetExtension(new UriBuilder(mediaSource.Path).Path);

                if (string.IsNullOrEmpty(extension))
                {
                    try
                    {
                        using var message = new HttpRequestMessage(HttpMethod.Head, mediaSource.Path);
                        using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                            .SendAsync(message, cancellationToken)
                            .ConfigureAwait(false);

                        if (response.IsSuccessStatusCode)
                        {
                            if (_mimeTypesCanShareHttpStream.Contains(response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase))
                            {
                                return new SharedHttpStream(mediaSource, tunerHost, streamId, FileSystem, _httpClientFactory, Logger, Config, _appHost, _streamHelper);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        Logger.LogWarning("HEAD request to check MIME type failed, shared stream disabled");
                    }
                }
                else if (_extensionsCanShareHttpStream.Contains(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return new SharedHttpStream(mediaSource, tunerHost, streamId, FileSystem, _httpClientFactory, Logger, Config, _appHost, _streamHelper);
                }
            }

            return new LiveStream(mediaSource, tunerHost, FileSystem, Logger, Config, _streamHelper);
        }

        public async Task Validate(TunerHostInfo info)
        {
            using (await new M3uParser(Logger, _httpClientFactory).GetListingsStream(info, CancellationToken.None).ConfigureAwait(false))
            {
            }
        }

        protected override Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, ChannelInfo channel, CancellationToken cancellationToken)
        {
            return Task.FromResult(new List<MediaSourceInfo> { CreateMediaSourceInfo(tuner, channel) });
        }

        protected virtual MediaSourceInfo CreateMediaSourceInfo(TunerHostInfo info, ChannelInfo channel)
        {
            var path = channel.Path;

            var supportsDirectPlay = !info.EnableStreamLooping && info.TunerCount == 0;
            var supportsDirectStream = !info.EnableStreamLooping;

            var protocol = _mediaSourceManager.GetPathProtocol(path);

            var isRemote = true;
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri))
            {
                isRemote = !_networkManager.IsInLocalNetwork(uri.Host);
            }

            var httpHeaders = new Dictionary<string, string>();

            if (protocol == MediaProtocol.Http)
            {
                // Use user-defined user-agent. If there isn't one, make it look like a browser.
                httpHeaders[HeaderNames.UserAgent] = string.IsNullOrWhiteSpace(info.UserAgent) ?
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36" :
                    info.UserAgent;
            }

            var mediaSource = new MediaSourceInfo
            {
                Path = path,
                Protocol = protocol,
                MediaStreams = new MediaStream[]
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

                Id = channel.Path.GetMD5().ToString("N", CultureInfo.InvariantCulture),
                IsInfiniteStream = true,
                IsRemote = isRemote,

                IgnoreDts = info.IgnoreDts,
                SupportsDirectPlay = supportsDirectPlay,
                SupportsDirectStream = supportsDirectStream,

                RequiredHttpHeaders = httpHeaders,
                UseMostCompatibleTranscodingProfile = !info.AllowFmp4TranscodingContainer,
                FallbackMaxStreamingBitrate = info.FallbackMaxStreamingBitrate
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
