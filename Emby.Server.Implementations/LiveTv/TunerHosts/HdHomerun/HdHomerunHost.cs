using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Net;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunHost : BaseTunerHost, ITunerHost, IConfigurableTunerHost
    {
        private readonly IHttpClient _httpClient;
        private readonly IFileSystem _fileSystem;
        private readonly IServerApplicationHost _appHost;
        private readonly ISocketFactory _socketFactory;

        public HdHomerunHost(IServerConfigurationManager config, ILogger logger, IJsonSerializer jsonSerializer, IMediaEncoder mediaEncoder, IHttpClient httpClient, IFileSystem fileSystem, IServerApplicationHost appHost, ISocketFactory socketFactory)
            : base(config, logger, jsonSerializer, mediaEncoder)
        {
            _httpClient = httpClient;
            _fileSystem = fileSystem;
            _appHost = appHost;
            _socketFactory = socketFactory;
        }

        public string Name
        {
            get { return "HD Homerun"; }
        }

        public override string Type
        {
            get { return DeviceType; }
        }

        public static string DeviceType
        {
            get { return "hdhomerun"; }
        }

        private const string ChannelIdPrefix = "hdhr_";

        private string GetChannelId(TunerHostInfo info, Channels i)
        {
            var id = ChannelIdPrefix + i.GuideNumber;

            id += '_' + (i.GuideName ?? string.Empty).GetMD5().ToString("N");

            return id;
        }

        private async Task<List<Channels>> GetLineup(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = string.Format("{0}/lineup.json", GetApiUrl(info, false)),
                CancellationToken = cancellationToken,
                BufferContent = false
            };
            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                var lineup = JsonSerializer.DeserializeFromStream<List<Channels>>(stream) ?? new List<Channels>();

                if (info.ImportFavoritesOnly)
                {
                    lineup = lineup.Where(i => i.Favorite).ToList();
                }

                return lineup.Where(i => !i.DRM).ToList();
            }
        }

        protected override async Task<List<ChannelInfo>> GetChannelsInternal(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var lineup = await GetLineup(info, cancellationToken).ConfigureAwait(false);

            return lineup.Select(i => new ChannelInfo
            {
                Name = i.GuideName,
                Number = i.GuideNumber,
                Id = GetChannelId(info, i),
                IsFavorite = i.Favorite,
                TunerHostId = info.Id,
                IsHD = i.HD == 1,
                AudioCodec = i.AudioCodec,
                VideoCodec = i.VideoCodec,
                ChannelType = ChannelType.TV

            }).ToList();
        }

        private readonly Dictionary<string, DiscoverResponse> _modelCache = new Dictionary<string, DiscoverResponse>();
        private async Task<DiscoverResponse> GetModelInfo(TunerHostInfo info, bool throwAllExceptions, CancellationToken cancellationToken)
        {
            lock (_modelCache)
            {
                DiscoverResponse response;
                if (_modelCache.TryGetValue(info.Url, out response))
                {
                    return response;
                }
            }

            try
            {
                using (var stream = await _httpClient.Get(new HttpRequestOptions()
                {
                    Url = string.Format("{0}/discover.json", GetApiUrl(info, false)),
                    CancellationToken = cancellationToken,
                    CacheLength = TimeSpan.FromDays(1),
                    CacheMode = CacheMode.Unconditional,
                    TimeoutMs = Convert.ToInt32(TimeSpan.FromSeconds(5).TotalMilliseconds),
                    BufferContent = false

                }).ConfigureAwait(false))
                {
                    var response = JsonSerializer.DeserializeFromStream<DiscoverResponse>(stream);

                    lock (_modelCache)
                    {
                        _modelCache[info.Id] = response;
                    }

                    return response;
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
                    // HDHR4 doesn't have this api
                    lock (_modelCache)
                    {
                        _modelCache[info.Id] = response;
                    }
                    return response;
                }

                throw;
            }
        }

        private async Task<List<LiveTvTunerInfo>> GetTunerInfos(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var model = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);

            var tuners = new List<LiveTvTunerInfo>();

            using (var manager = new HdHomerunManager(_socketFactory))
            {
                // Legacy HdHomeruns are IPv4 only
                var ipInfo = new IpAddressInfo(info.Url, IpAddressFamily.InterNetwork);

                for (int i = 0; i < model.TunerCount; ++i)
                {
                    var name = String.Format("Tuner {0}", i + 1);
                    var currentChannel = "none"; /// @todo Get current channel and map back to Station Id      
                    var isAvailable = await manager.CheckTunerAvailability(ipInfo, i, cancellationToken).ConfigureAwait(false);
                    LiveTvTunerStatus status = isAvailable ? LiveTvTunerStatus.Available : LiveTvTunerStatus.LiveTv;
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
                .Where(i => i.IsEnabled && string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    list.AddRange(await GetTunerInfos(host, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error getting tuner info", ex);
                }
            }

            return list;
        }

        private string GetApiUrl(TunerHostInfo info, bool isPlayback)
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

            var uri = new Uri(url);

            if (isPlayback)
            {
                var builder = new UriBuilder(uri);
                builder.Port = 5004;
                uri = builder.Uri;
            }

            return uri.AbsoluteUri.TrimEnd('/');
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

        private async Task<MediaSourceInfo> GetMediaSource(TunerHostInfo info, string channelId, string profile)
        {
            int? width = null;
            int? height = null;
            bool isInterlaced = true;
            string videoCodec = null;
            string audioCodec = "ac3";

            int? videoBitrate = null;
            int? audioBitrate = null;

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
            else if (string.Equals(profile, "internet540", StringComparison.OrdinalIgnoreCase))
            {
                width = 960;
                height = 546;
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

            var channels = await GetChannels(info, true, CancellationToken.None).ConfigureAwait(false);
            var channel = channels.FirstOrDefault(i => string.Equals(i.Number, channelId, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
            {
                if (string.IsNullOrWhiteSpace(videoCodec))
                {
                    videoCodec = channel.VideoCodec;
                }
                audioCodec = channel.AudioCodec;

                if (!videoBitrate.HasValue)
                {
                    videoBitrate = (channel.IsHD ?? true) ? 15000000 : 2000000;
                }
                audioBitrate = (channel.IsHD ?? true) ? 448000 : 192000;
            }

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

            var url = GetApiUrl(info, true) + "/auto/v" + channelId;

            // If raw was used, the tuner doesn't support params
            if (!string.IsNullOrWhiteSpace(profile)
                && !string.Equals(profile, "native", StringComparison.OrdinalIgnoreCase))
            {
                url += "?transcode=" + profile;
            }

            var id = profile;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = "native";
            }
            id += "_" + url.GetMD5().ToString("N");

            var mediaSource = new MediaSourceInfo
            {
                Path = url,
                Protocol = MediaProtocol.Http,
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
                RequiresClosing = false,
                BufferMs = 0,
                Container = "ts",
                Id = id,
                SupportsDirectPlay = false,
                SupportsDirectStream = true,
                SupportsTranscoding = true,
                IsInfiniteStream = true
            };

            mediaSource.InferTotalBitrate();

            return mediaSource;
        }

        protected EncodingOptions GetEncodingOptions()
        {
            return Config.GetConfiguration<EncodingOptions>("encoding");
        }

        private string GetHdHrIdFromChannelId(string channelId)
        {
            return channelId.Split('_')[1];
        }

        protected override async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, string channelId, CancellationToken cancellationToken)
        {
            var list = new List<MediaSourceInfo>();

            if (!channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return list;
            }
            var hdhrId = GetHdHrIdFromChannelId(channelId);

            try
            {
                var modelInfo = await GetModelInfo(info, false, cancellationToken).ConfigureAwait(false);
                var model = modelInfo == null ? string.Empty : (modelInfo.ModelNumber ?? string.Empty);

                if ((model.IndexOf("hdtc", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    list.Add(await GetMediaSource(info, hdhrId, "native").ConfigureAwait(false));

                    if (info.AllowHWTranscoding)
                    {
                        list.Add(await GetMediaSource(info, hdhrId, "heavy").ConfigureAwait(false));

                        list.Add(await GetMediaSource(info, hdhrId, "internet540").ConfigureAwait(false));
                        list.Add(await GetMediaSource(info, hdhrId, "internet480").ConfigureAwait(false));
                        list.Add(await GetMediaSource(info, hdhrId, "internet360").ConfigureAwait(false));
                        list.Add(await GetMediaSource(info, hdhrId, "internet240").ConfigureAwait(false));
                        list.Add(await GetMediaSource(info, hdhrId, "mobile").ConfigureAwait(false));
                    }
                }
            }
            catch
            {

            }

            if (list.Count == 0)
            {
                list.Add(await GetMediaSource(info, hdhrId, "native").ConfigureAwait(false));
            }

            return list;
        }

        protected override bool IsValidChannelId(string channelId)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException("channelId");
            }

            return channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase);
        }

        protected override async Task<LiveStream> GetChannelStream(TunerHostInfo info, string channelId, string streamId, CancellationToken cancellationToken)
        {
            var profile = streamId.Split('_')[0];

            Logger.Info("GetChannelStream: channel id: {0}. stream id: {1} profile: {2}", channelId, streamId, profile);

            if (!channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Channel not found");
            }
            var hdhrId = GetHdHrIdFromChannelId(channelId);

            var mediaSource = await GetMediaSource(info, hdhrId, profile).ConfigureAwait(false);

            var liveStream = new HdHomerunLiveStream(mediaSource, streamId, _fileSystem, _httpClient, Logger, Config.ApplicationPaths, _appHost);
            liveStream.EnableStreamSharing = true;
            return liveStream;
        }

        public async Task Validate(TunerHostInfo info)
        {
            if (!info.IsEnabled)
            {
                return;
            }

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

        protected override async Task<bool> IsAvailableInternal(TunerHostInfo tuner, string channelId, CancellationToken cancellationToken)
        {
            var info = await GetTunerInfos(tuner, cancellationToken).ConfigureAwait(false);

            return info.Any(i => i.Status == LiveTvTunerStatus.Available);
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
        }
    }
}
