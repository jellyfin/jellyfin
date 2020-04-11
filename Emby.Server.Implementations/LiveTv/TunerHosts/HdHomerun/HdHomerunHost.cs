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
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private readonly IHttpClient _httpClient;
        private readonly IServerApplicationHost _appHost;
        private readonly ISocketFactory _socketFactory;
        private readonly INetworkManager _networkManager;
        private readonly IStreamHelper _streamHelper;

        public HdHomerunHost(
            IServerConfigurationManager config,
            ILogger<HdHomerunHost> logger,
            IJsonSerializer jsonSerializer,
            IFileSystem fileSystem,
            IHttpClient httpClient,
            IServerApplicationHost appHost,
            ISocketFactory socketFactory,
            INetworkManager networkManager,
            IStreamHelper streamHelper)
            : base(config, logger, jsonSerializer, fileSystem)
        {
            _httpClient = httpClient;
            _appHost = appHost;
            _socketFactory = socketFactory;
            _networkManager = networkManager;
            _streamHelper = streamHelper;
        }

        public string Name => "HD Homerun";

        public override string Type => "hdhomerun";

        protected override string ChannelIdPrefix => "hdhr_";

        private string GetChannelId(TunerHostInfo info, Channels i)
            => ChannelIdPrefix + i.GuideNumber;

        private async Task<List<Channels>> GetLineup(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            var options = new HttpRequestOptions
            {
                Url = model.LineupURL,
                CancellationToken = cancellationToken,
                BufferContent = false
            };

            using (var response = await _httpClient.SendAsync(options, HttpMethod.Get).ConfigureAwait(false))
            using (var stream = response.Content)
            {
                var lineup = await JsonSerializer.DeserializeFromStreamAsync<List<Channels>>(stream).ConfigureAwait(false) ?? new List<Channels>();

                if (info.ImportFavoritesOnly)
                {
                    lineup = lineup.Where(i => i.Favorite).ToList();
                }

                return lineup.Where(i => !i.DRM).ToList();
            }
        }

        private class HdHomerunChannelInfo : ChannelInfo
        {
            public bool IsLegacyTuner { get; set; }
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var lineup = await GetLineup(info, cancellationToken).ConfigureAwait(false);

            return lineup.Select(i => new HdHomerunChannelInfo
            {
                Name = i.GuideName,
                Number = i.GuideNumber,
                Id = GetChannelId(info, i),
                IsFavorite = i.Favorite,
                TunerHostId = info.Id,
                IsHD = i.HD == 1,
                AudioCodec = i.AudioCodec,
                VideoCodec = i.VideoCodec,
                ChannelType = ChannelType.TV,
                IsLegacyTuner = (i.URL ?? string.Empty).StartsWith("hdhomerun", StringComparison.OrdinalIgnoreCase),
                Path = i.URL

            }).Cast<ChannelInfo>().ToList();
        }

        private readonly Dictionary<string, DiscoverResponse> _modelCache = new Dictionary<string, DiscoverResponse>();
        private async Task<DiscoverResponse> GetModelInfo(TunerHostInfo info, bool throwAllExceptions, CancellationToken cancellationToken)
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
                using (var response = await _httpClient.SendAsync(new HttpRequestOptions()
                {
                    Url = string.Format("{0}/discover.json", GetApiUrl(info)),
                    CancellationToken = cancellationToken,
                    BufferContent = false
                }, HttpMethod.Get).ConfigureAwait(false))
                using (var stream = response.Content)
                {
                    var discoverResponse = await JsonSerializer.DeserializeFromStreamAsync<DiscoverResponse>(stream).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(cacheKey))
                    {
                        lock (_modelCache)
                        {
                            _modelCache[cacheKey] = discoverResponse;
                        }
                    }

                    return discoverResponse;
                }
            }
            catch (HttpException ex)
            {
                if (!throwAllExceptions && ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.NotFound)
                {
                    var defaultValue = "HDHR";
                    var response = new DiscoverResponse
                    {
                        ModelNumber = defaultValue
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

            using (var response = await _httpClient.SendAsync(new HttpRequestOptions()
            {
                Url = string.Format("{0}/tuners.html", GetApiUrl(info)),
                CancellationToken = cancellationToken,
                BufferContent = false
            }, HttpMethod.Get).ConfigureAwait(false))
            using (var stream = response.Content)
            using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                var tuners = new List<LiveTvTunerInfo>();
                while (!sr.EndOfStream)
                {
                    string line = StripXML(sr.ReadLine());
                    if (line.Contains("Channel"))
                    {
                        LiveTvTunerStatus status;
                        var index = line.IndexOf("Channel", StringComparison.OrdinalIgnoreCase);
                        var name = line.Substring(0, index - 1);
                        var currentChannel = line.Substring(index + 7);
                        if (currentChannel != "none") { status = LiveTvTunerStatus.LiveTv; } else { status = LiveTvTunerStatus.Available; }
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
        }

        private static string StripXML(string source)
        {
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
                    buffer[bufferIndex] = let;
                    bufferIndex++;
                }
            }

            return new string(buffer, 0, bufferIndex);
        }

        private async Task<List<LiveTvTunerInfo>> GetTunerInfosUdp(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            var tuners = new List<LiveTvTunerInfo>();

            var uri = new Uri(GetApiUrl(info));

            using (var manager = new HdHomerunManager())
            {
                // Legacy HdHomeruns are IPv4 only
                var ipInfo = IPAddress.Parse(uri.Host);

                for (int i = 0; i < model.TunerCount; ++i)
                {
                    var name = string.Format("Tuner {0}", i + 1);
                    var currentChannel = "none"; // @todo Get current channel and map back to Station Id
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

        private class Channels
        {
            public string GuideNumber { get; set; }
            public string GuideName { get; set; }
            public string VideoCodec { get; set; }
            public string AudioCodec { get; set; }
            public string URL { get; set; }
            public bool Favorite { get; set; }
            public bool DRM { get; set; }
            public int HD { get; set; }
        }

        protected EncodingOptions GetEncodingOptions()
        {
            return Config.GetConfiguration<EncodingOptions>("encoding");
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

            if (!videoBitrate.HasValue)
            {
                videoBitrate = isHd ? 15000000 : 2000000;
            }

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
                MediaStreams = new List<MediaStream>
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
                //IgnoreIndex = true,
                //ReadAtNativeFramerate = true
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
        }

        protected override async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, ChannelInfo channelInfo, CancellationToken cancellationToken)
        {
            var list = new List<MediaSourceInfo>();

            var channelId = channelInfo.Id;
            var hdhrId = GetHdHrIdFromChannelId(channelId);

            var hdHomerunChannelInfo = channelInfo as HdHomerunChannelInfo;

            var isLegacyTuner = hdHomerunChannelInfo != null && hdHomerunChannelInfo.IsLegacyTuner;

            if (isLegacyTuner)
            {
                list.Add(GetMediaSource(info, hdhrId, channelInfo, "native"));
            }
            else
            {
                var modelInfo = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

                if (modelInfo != null && modelInfo.SupportsTranscoding)
                {
                    if (info.AllowHWTranscoding)
                    {
                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "heavy"));

                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "internet540"));
                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "internet480"));
                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "internet360"));
                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "internet240"));
                        list.Add(GetMediaSource(info, hdhrId, channelInfo, "mobile"));
                    }

                    list.Add(GetMediaSource(info, hdhrId, channelInfo, "native"));
                }

                if (list.Count == 0)
                {
                    list.Add(GetMediaSource(info, hdhrId, channelInfo, "native"));
                }
            }

            return list;
        }

        protected override async Task<ILiveStream> GetChannelStream(TunerHostInfo info, ChannelInfo channelInfo, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken)
        {
            var profile = streamId.Split('_')[0];

            Logger.LogInformation("GetChannelStream: channel id: {0}. stream id: {1} profile: {2}", channelInfo.Id, streamId, profile);

            var hdhrId = GetHdHrIdFromChannelId(channelInfo.Id);

            var hdhomerunChannel = channelInfo as HdHomerunChannelInfo;

            var modelInfo = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            if (!modelInfo.SupportsTranscoding)
            {
                profile = "native";
            }

            var mediaSource = GetMediaSource(info, hdhrId, channelInfo, profile);

            if (hdhomerunChannel != null && hdhomerunChannel.IsLegacyTuner)
            {
                return new HdHomerunUdpStream(
                    mediaSource,
                    info,
                    streamId,
                    new LegacyHdHomerunChannelCommands(hdhomerunChannel.Path),
                    modelInfo.TunerCount,
                    FileSystem,
                    Logger,
                    Config,
                    _appHost,
                    _networkManager,
                    _streamHelper);
            }

            var enableHttpStream = true;
            if (enableHttpStream)
            {
                mediaSource.Protocol = MediaProtocol.Http;

                var httpUrl = channelInfo.Path;

                // If raw was used, the tuner doesn't support params
                if (!string.IsNullOrWhiteSpace(profile) && !string.Equals(profile, "native", StringComparison.OrdinalIgnoreCase))
                {
                    httpUrl += "?transcode=" + profile;
                }

                mediaSource.Path = httpUrl;

                return new SharedHttpStream(
                    mediaSource,
                    info,
                    streamId,
                    FileSystem,
                    _httpClient,
                    Logger,
                    Config,
                    _appHost,
                    _streamHelper);
            }

            return new HdHomerunUdpStream(
                mediaSource,
                info,
                streamId,
                new HdHomerunChannelCommands(hdhomerunChannel.Number, profile),
                modelInfo.TunerCount,
                FileSystem,
                Logger,
                Config,
                _appHost,
                _networkManager,
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
            catch (HttpException ex)
            {
                if (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.NotFound)
                {
                    // HDHR4 doesn't have this api
                    return;
                }

                throw;
            }
        }

        public class DiscoverResponse
        {
            public string FriendlyName { get; set; }
            public string ModelNumber { get; set; }
            public string FirmwareName { get; set; }
            public string FirmwareVersion { get; set; }
            public string DeviceID { get; set; }
            public string DeviceAuth { get; set; }
            public string BaseURL { get; set; }
            public string LineupURL { get; set; }
            public int TunerCount { get; set; }

            public bool SupportsTranscoding
            {
                get
                {
                    var model = ModelNumber ?? string.Empty;

                    if ((model.IndexOf("hdtc", StringComparison.OrdinalIgnoreCase) != -1))
                    {
                        return true;
                    }

                    return false;
                }
            }
        }

        public async Task<List<TunerHostInfo>> DiscoverDevices(int discoveryDurationMs, CancellationToken cancellationToken)
        {
            lock (_modelCache)
            {
                _modelCache.Clear();
            }

            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(discoveryDurationMs).Token, cancellationToken).Token;
            var list = new List<TunerHostInfo>();

            // Create udp broadcast discovery message
            byte[] discBytes = { 0, 2, 0, 12, 1, 4, 255, 255, 255, 255, 2, 4, 255, 255, 255, 255, 115, 204, 125, 143 };
            using (var udpClient = _socketFactory.CreateUdpBroadcastSocket(0))
            {
                // Need a way to set the Receive timeout on the socket otherwise this might never timeout?
                try
                {
                    await udpClient.SendToAsync(discBytes, 0, discBytes.Length, new IPEndPoint(IPAddress.Parse("255.255.255.255"), 65001), cancellationToken);
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

        private async Task<TunerHostInfo> TryGetTunerHostInfo(string url, CancellationToken cancellationToken)
        {
            var hostInfo = new TunerHostInfo
            {
                Type = Type,
                Url = url
            };

            var modelInfo = await GetModelInfo(hostInfo, false, cancellationToken).ConfigureAwait(false);

            hostInfo.DeviceId = modelInfo.DeviceID;
            hostInfo.FriendlyName = modelInfo.FriendlyName;

            return hostInfo;
        }
    }
}
