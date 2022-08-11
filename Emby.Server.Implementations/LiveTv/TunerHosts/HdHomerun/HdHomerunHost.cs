#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
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
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly ISocketFactory _socketFactory;
        private readonly IStreamHelper _streamHelper;

        private readonly JsonSerializerOptions _jsonOptions;

        private readonly Dictionary<string, DiscoverResponse> _modelCache = new Dictionary<string, DiscoverResponse>();

        public HdHomerunHost(
            IServerConfigurationManager config,
            ILogger<HdHomerunHost> logger,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory,
            IServerApplicationHost appHost,
            ISocketFactory socketFactory,
            IStreamHelper streamHelper,
            IMemoryCache memoryCache)
            : base(config, logger, fileSystem, memoryCache)
        {
            _httpClientFactory = httpClientFactory;
            _appHost = appHost;
            _socketFactory = socketFactory;
            _streamHelper = streamHelper;

            _jsonOptions = JsonDefaults.Options;
        }

        public string Name => "HD Homerun";

        public override string Type => "hdhomerun";

        protected override string ChannelIdPrefix => "hdhr_";

        private string GetChannelId(Channels i)
            => ChannelIdPrefix + i.GuideNumber;

        internal async Task<List<Channels>> GetLineup(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(model.LineupURL ?? model.BaseURL + "/lineup.json", HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var lineup = await JsonSerializer.DeserializeAsync<List<Channels>>(stream, _jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? new List<Channels>();

            if (info.ImportFavoritesOnly)
            {
                lineup = lineup.Where(i => i.Favorite).ToList();
            }

            return lineup.Where(i => !i.DRM).ToList();
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo tuner, CancellationToken cancellationToken)
        {
            var lineup = await GetLineup(tuner, cancellationToken).ConfigureAwait(false);

            return lineup.Select(i => new HdHomerunChannelInfo
            {
                Name = i.GuideName,
                Number = i.GuideNumber,
                Id = GetChannelId(i),
                IsFavorite = i.Favorite,
                TunerHostId = tuner.Id,
                IsHD = i.HD,
                AudioCodec = i.AudioCodec,
                VideoCodec = i.VideoCodec,
                ChannelType = ChannelType.TV,
                IsLegacyTuner = (i.URL ?? string.Empty).StartsWith("hdhomerun", StringComparison.OrdinalIgnoreCase),
                Path = i.URL
            }).Cast<ChannelInfo>().ToList();
        }

        internal async Task<DiscoverResponse> GetModelInfo(TunerHostInfo info, bool throwAllExceptions, CancellationToken cancellationToken)
        {
            var cacheKey = info.Id;

            lock (_modelCache)
            {
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    if (_modelCache.TryGetValue(cacheKey, out DiscoverResponse response))
                    {
                        return response;
                    }
                }
            }

            try
            {
                using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .GetAsync(GetApiUrl(info) + "/discover.json", HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var discoverResponse = await JsonSerializer.DeserializeAsync<DiscoverResponse>(stream, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    lock (_modelCache)
                    {
                        _modelCache[cacheKey] = discoverResponse;
                    }
                }

                return discoverResponse;
            }
            catch (HttpRequestException ex)
            {
                if (!throwAllExceptions && ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    const string DefaultValue = "HDHR";
                    var response = new DiscoverResponse
                    {
                        ModelNumber = DefaultValue
                    };
                    if (!string.IsNullOrEmpty(cacheKey))
                    {
                        // HDHR4 doesn't have this api
                        lock (_modelCache)
                        {
                            _modelCache[cacheKey] = response;
                        }
                    }

                    return response;
                }

                throw;
            }
        }

        private async Task<List<LiveTvTunerInfo>> GetTunerInfosHttp(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(string.Format(CultureInfo.InvariantCulture, "{0}/tuners.html", GetApiUrl(info)), HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            var tuners = new List<LiveTvTunerInfo>();
            await foreach (var line in sr.ReadAllLinesAsync().ConfigureAwait(false))
            {
                string stripedLine = StripXML(line);
                if (stripedLine.Contains("Channel", StringComparison.Ordinal))
                {
                    LiveTvTunerStatus status;
                    var index = stripedLine.IndexOf("Channel", StringComparison.OrdinalIgnoreCase);
                    var name = stripedLine.Substring(0, index - 1);
                    var currentChannel = stripedLine.Substring(index + 7);
                    if (string.Equals(currentChannel, "none", StringComparison.Ordinal))
                    {
                        status = LiveTvTunerStatus.LiveTv;
                    }
                    else
                    {
                        status = LiveTvTunerStatus.Available;
                    }

                    tuners.Add(new LiveTvTunerInfo
                    {
                        Name = name,
                        SourceType = string.IsNullOrWhiteSpace(model.ModelNumber) ? Name : model.ModelNumber,
                        ProgramName = currentChannel,
                        Status = status
                    });
                }
            }

            return tuners;
        }

        private static string StripXML(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            char[] buffer = new char[source.Length];
            int bufferIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }

                if (let == '>')
                {
                    inside = false;
                    continue;
                }

                if (!inside)
                {
                    buffer[bufferIndex++] = let;
                }
            }

            return new string(buffer, 0, bufferIndex);
        }

        private async Task<List<LiveTvTunerInfo>> GetTunerInfosUdp(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            var tuners = new List<LiveTvTunerInfo>(model.TunerCount);

            var uri = new Uri(GetApiUrl(info));

            using (var manager = new HdHomerunManager())
            {
                // Legacy HdHomeruns are IPv4 only
                var ipInfo = IPAddress.Parse(uri.Host);

                for (int i = 0; i < model.TunerCount; i++)
                {
                    var name = string.Format(CultureInfo.InvariantCulture, "Tuner {0}", i + 1);
                    var currentChannel = "none"; // TODO: Get current channel and map back to Station Id
                    var isAvailable = await manager.CheckTunerAvailability(ipInfo, i, cancellationToken).ConfigureAwait(false);
                    var status = isAvailable ? LiveTvTunerStatus.Available : LiveTvTunerStatus.LiveTv;
                    tuners.Add(new LiveTvTunerInfo
                    {
                        Name = name,
                        SourceType = string.IsNullOrWhiteSpace(model.ModelNumber) ? Name : model.ModelNumber,
                        ProgramName = currentChannel,
                        Status = status
                    });
                }
            }

            return tuners;
        }

        public async Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken)
        {
            var list = new List<LiveTvTunerInfo>();

            foreach (var host in GetConfiguration().TunerHosts
                .Where(i => string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    list.AddRange(await GetTunerInfos(host, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error getting tuner info");
                }
            }

            return list;
        }

        public async Task<List<LiveTvTunerInfo>> GetTunerInfos(TunerHostInfo info, CancellationToken cancellationToken)
        {
            // TODO Need faster way to determine UDP vs HTTP
            var channels = await GetChannels(info, true, cancellationToken).ConfigureAwait(false);

            var hdHomerunChannelInfo = channels.FirstOrDefault() as HdHomerunChannelInfo;

            if (hdHomerunChannelInfo == null || hdHomerunChannelInfo.IsLegacyTuner)
            {
                return await GetTunerInfosUdp(info, cancellationToken).ConfigureAwait(false);
            }

            return await GetTunerInfosHttp(info, cancellationToken).ConfigureAwait(false);
        }

        private static string GetApiUrl(TunerHostInfo info)
        {
            var url = info.Url;

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("Invalid tuner info");
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            return new Uri(url).AbsoluteUri.TrimEnd('/');
        }

        private static string GetHdHrIdFromChannelId(string channelId)
        {
            return channelId.Split('_')[1];
        }

        private MediaSourceInfo GetMediaSource(TunerHostInfo info, string channelId, ChannelInfo channelInfo, string profile)
        {
            int? width = null;
            int? height = null;
            bool isInterlaced = true;
            string videoCodec = null;

            int? videoBitrate = null;

            var isHd = channelInfo.IsHD ?? true;

            if (string.Equals(profile, "mobile", StringComparison.OrdinalIgnoreCase))
            {
                width = 1280;
                height = 720;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 2000000;
            }
            else if (string.Equals(profile, "heavy", StringComparison.OrdinalIgnoreCase))
            {
                width = 1920;
                height = 1080;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 15000000;
            }
            else if (string.Equals(profile, "internet720", StringComparison.OrdinalIgnoreCase))
            {
                width = 1280;
                height = 720;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 8000000;
            }
            else if (string.Equals(profile, "internet540", StringComparison.OrdinalIgnoreCase))
            {
                width = 960;
                height = 540;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 2500000;
            }
            else if (string.Equals(profile, "internet480", StringComparison.OrdinalIgnoreCase))
            {
                width = 848;
                height = 480;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 2000000;
            }
            else if (string.Equals(profile, "internet360", StringComparison.OrdinalIgnoreCase))
            {
                width = 640;
                height = 360;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 1500000;
            }
            else if (string.Equals(profile, "internet240", StringComparison.OrdinalIgnoreCase))
            {
                width = 432;
                height = 240;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 1000000;
            }
            else
            {
                // This is for android tv's 1200 condition. Remove once not needed anymore so that we can avoid possible side effects of dummying up this data
                if (isHd)
                {
                    width = 1920;
                    height = 1080;
                }
            }

            if (string.IsNullOrWhiteSpace(videoCodec))
            {
                videoCodec = channelInfo.VideoCodec;
            }

            string audioCodec = channelInfo.AudioCodec;

            videoBitrate ??= isHd ? 15000000 : 2000000;

            int? audioBitrate = isHd ? 448000 : 192000;

            // normalize
            if (string.Equals(videoCodec, "mpeg2", StringComparison.OrdinalIgnoreCase))
            {
                videoCodec = "mpeg2video";
            }

            string nal = null;
            if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
            {
                nal = "0";
            }

            var url = GetApiUrl(info);

            var id = profile;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = "native";
            }

            id += "_" + channelId.GetMD5().ToString("N", CultureInfo.InvariantCulture) + "_" + url.GetMD5().ToString("N", CultureInfo.InvariantCulture);

            var mediaSource = new MediaSourceInfo
            {
                Path = url,
                Protocol = MediaProtocol.Udp,
                MediaStreams = new MediaStream[]
                {
                    new MediaStream
                    {
                        Type = MediaStreamType.Video,
                        // Set the index to -1 because we don't know the exact index of the video stream within the container
                        Index = -1,
                        IsInterlaced = isInterlaced,
                        Codec = videoCodec,
                        Width = width,
                        Height = height,
                        BitRate = videoBitrate,
                        NalLengthSize = nal
                    },
                    new MediaStream
                    {
                        Type = MediaStreamType.Audio,
                        // Set the index to -1 because we don't know the exact index of the audio stream within the container
                        Index = -1,
                        Codec = audioCodec,
                        BitRate = audioBitrate
                    }
                },
                RequiresOpening = true,
                RequiresClosing = true,
                BufferMs = 0,
                Container = "ts",
                Id = id,
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                IsInfiniteStream = true,
                IgnoreDts = true,
                // IgnoreIndex = true,
                // ReadAtNativeFramerate = true
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
        }

        protected override async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo tuner, ChannelInfo channel, CancellationToken cancellationToken)
        {
            var list = new List<MediaSourceInfo>();

            var channelId = channel.Id;
            var hdhrId = GetHdHrIdFromChannelId(channelId);

            if (channel is HdHomerunChannelInfo hdHomerunChannelInfo && hdHomerunChannelInfo.IsLegacyTuner)
            {
                list.Add(GetMediaSource(tuner, hdhrId, channel, "native"));
            }
            else
            {
                var modelInfo = await GetModelInfo(tuner, false, cancellationToken).ConfigureAwait(false);

                if (modelInfo != null && modelInfo.SupportsTranscoding)
                {
                    if (tuner.AllowHWTranscoding)
                    {
                        list.Add(GetMediaSource(tuner, hdhrId, channel, "heavy"));

                        list.Add(GetMediaSource(tuner, hdhrId, channel, "internet540"));
                        list.Add(GetMediaSource(tuner, hdhrId, channel, "internet480"));
                        list.Add(GetMediaSource(tuner, hdhrId, channel, "internet360"));
                        list.Add(GetMediaSource(tuner, hdhrId, channel, "internet240"));
                        list.Add(GetMediaSource(tuner, hdhrId, channel, "mobile"));
                    }

                    list.Add(GetMediaSource(tuner, hdhrId, channel, "native"));
                }

                if (list.Count == 0)
                {
                    list.Add(GetMediaSource(tuner, hdhrId, channel, "native"));
                }
            }

            return list;
        }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo tunerHost, ChannelInfo channel, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            var tunerCount = tunerHost.TunerCount;

            if (tunerCount > 0)
            {
                var tunerHostId = tunerHost.Id;
                var liveStreams = currentLiveStreams.Where(i => string.Equals(i.TunerHostId, tunerHostId, StringComparison.OrdinalIgnoreCase));

                if (liveStreams.Count() >= tunerCount)
                {
                    throw new LiveTvConflictException("HDHomeRun simultaneous stream limit has been reached.");
                }
            }

            var profile = streamId.AsSpan().LeftPart('_').ToString();

            Logger.LogInformation("GetChannelStream: channel id: {0}. stream id: {1} profile: {2}", channel.Id, streamId, profile);

            var hdhrId = GetHdHrIdFromChannelId(channel.Id);

            var hdhomerunChannel = channel as HdHomerunChannelInfo;

            var modelInfo = await GetModelInfo(tunerHost, false, cancellationToken).ConfigureAwait(false);

            if (!modelInfo.SupportsTranscoding)
            {
                profile = "native";
            }

            var mediaSource = GetMediaSource(tunerHost, hdhrId, channel, profile);

            if (hdhomerunChannel != null && hdhomerunChannel.IsLegacyTuner)
            {
                return new HdHomerunUdpStream(
                    mediaSource,
                    tunerHost,
                    streamId,
                    new LegacyHdHomerunChannelCommands(hdhomerunChannel.Path),
                    modelInfo.TunerCount,
                    FileSystem,
                    Logger,
                    Config,
                    _appHost,
                    _streamHelper);
            }

            var enableHttpStream = true;
            if (enableHttpStream)
            {
                mediaSource.Protocol = MediaProtocol.Http;

                var httpUrl = channel.Path;

                // If raw was used, the tuner doesn't support params
                if (!string.IsNullOrWhiteSpace(profile) && !string.Equals(profile, "native", StringComparison.OrdinalIgnoreCase))
                {
                    httpUrl += "?transcode=" + profile;
                }

                mediaSource.Path = httpUrl;

                return new SharedHttpStream(
                    mediaSource,
                    tunerHost,
                    streamId,
                    FileSystem,
                    _httpClientFactory,
                    Logger,
                    Config,
                    _appHost,
                    _streamHelper);
            }

            return new HdHomerunUdpStream(
                mediaSource,
                tunerHost,
                streamId,
                new HdHomerunChannelCommands(hdhomerunChannel.Number, profile),
                modelInfo.TunerCount,
                FileSystem,
                Logger,
                Config,
                _appHost,
                _streamHelper);
        }

        public async Task Validate(TunerHostInfo info)
        {
            lock (_modelCache)
            {
                _modelCache.Clear();
            }

            try
            {
                // Test it by pulling down the lineup
                var modelInfo = await GetModelInfo(info, true, CancellationToken.None).ConfigureAwait(false);
                info.DeviceId = modelInfo.DeviceID;
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                {
                    // HDHR4 doesn't have this api
                    return;
                }

                throw;
            }
        }

        public async Task<List<TunerHostInfo>> DiscoverDevices(int discoveryDurationMs, CancellationToken cancellationToken)
        {
            lock (_modelCache)
            {
                _modelCache.Clear();
            }

            using var timedCancellationToken = new CancellationTokenSource(discoveryDurationMs);
            using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timedCancellationToken.Token, cancellationToken);
            cancellationToken = linkedCancellationTokenSource.Token;
            var list = new List<TunerHostInfo>();

            // Create udp broadcast discovery message
            byte[] discBytes = { 0, 2, 0, 12, 1, 4, 255, 255, 255, 255, 2, 4, 255, 255, 255, 255, 115, 204, 125, 143 };
            using (var udpClient = _socketFactory.CreateUdpBroadcastSocket(0))
            {
                // Need a way to set the Receive timeout on the socket otherwise this might never timeout?
                try
                {
                    await udpClient.SendToAsync(discBytes, 0, discBytes.Length, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65001), cancellationToken).ConfigureAwait(false);
                    var receiveBuffer = new byte[8192];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var response = await udpClient.ReceiveAsync(receiveBuffer, 0, receiveBuffer.Length, cancellationToken).ConfigureAwait(false);
                        var deviceIp = response.RemoteEndPoint.Address.ToString();

                        // check to make sure we have enough bytes received to be a valid message and make sure the 2nd byte is the discover reply byte
                        if (response.ReceivedBytes > 13 && response.Buffer[1] == 3)
                        {
                            var deviceAddress = "http://" + deviceIp;

                            var info = await TryGetTunerHostInfo(deviceAddress, cancellationToken).ConfigureAwait(false);

                            if (info != null)
                            {
                                list.Add(info);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    // Socket timeout indicates all messages have been received.
                    Logger.LogError(ex, "Error while sending discovery message");
                }
            }

            return list;
        }

        internal async Task<TunerHostInfo> TryGetTunerHostInfo(string url, CancellationToken cancellationToken)
        {
            var hostInfo = new TunerHostInfo
            {
                Type = Type,
                Url = url
            };

            var modelInfo = await GetModelInfo(hostInfo, false, cancellationToken).ConfigureAwait(false);

            hostInfo.DeviceId = modelInfo.DeviceID;
            hostInfo.FriendlyName = modelInfo.FriendlyName;
            hostInfo.TunerCount = modelInfo.TunerCount;

            return hostInfo;
        }

        private class HdHomerunChannelInfo : ChannelInfo
        {
            public bool IsLegacyTuner { get; set; }
        }
    }
}
