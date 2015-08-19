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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public class HdHomerunHost : ITunerHost
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IConfigurationManager _config;

        public HdHomerunHost(IHttpClient httpClient, ILogger logger, IJsonSerializer jsonSerializer, IConfigurationManager config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonSerializer = jsonSerializer;
            _config = config;
        }

        public string Name
        {
            get { return "HD Homerun"; }
        }

        public string Type
        {
            get { return DeviceType; }
        }

        public static string DeviceType
        {
            get { return "hdhomerun"; }
        }

        private const string ChannelIdPrefix = "hdhr_";

        private List<TunerHostInfo> GetTunerHosts()
        {
            return GetConfiguration().TunerHosts
                .Where(i => i.IsEnabled && string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannels(CancellationToken cancellationToken)
        {
            var list = new List<ChannelInfo>();

            var hosts = GetTunerHosts();

            var ipAddresses = new List<string>();

            foreach (var host in hosts)
            {
                var ip = GetApiUrl(host, false);

                if (ipAddresses.Contains(ip, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    list.AddRange(await GetChannels(host, cancellationToken).ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error getting channel list", ex);
                }

                ipAddresses.Add(ip);
            }

            return list;
        }

        private async Task<IEnumerable<ChannelInfo>> GetChannels(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = string.Format("{0}/lineup.json", GetApiUrl(info, false)),
                CancellationToken = cancellationToken
            };
            using (var stream = await _httpClient.Get(options))
            {
                var root = _jsonSerializer.DeserializeFromStream<List<Channels>>(stream);

                if (root != null)
                {
                    var result = root.Select(i => new ChannelInfo
                    {
                        Name = i.GuideName,
                        Number = i.GuideNumber.ToString(CultureInfo.InvariantCulture),
                        Id = ChannelIdPrefix + i.GuideNumber.ToString(CultureInfo.InvariantCulture),
                        IsFavorite = i.Favorite

                    });

                    if (info.ImportFavoritesOnly)
                    {
                        result = result.Where(i => (i.IsFavorite ?? true)).ToList();
                    }

                    return result;
                }
                return new List<ChannelInfo>();
            }
        }

        private async Task<string> GetModelInfo(TunerHostInfo info, CancellationToken cancellationToken)
        {
            string model = null;

            using (var stream = await _httpClient.Get(new HttpRequestOptions()
            {
                Url = string.Format("{0}/", GetApiUrl(info, false)),
                CancellationToken = cancellationToken,
                CacheLength = TimeSpan.FromDays(1),
                CacheMode = CacheMode.Unconditional
            }))
            {
                using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = StripXML(sr.ReadLine());
                        if (line.StartsWith("Model:")) { model = line.Replace("Model: ", ""); }
                        //if (line.StartsWith("Device ID:")) { deviceID = line.Replace("Device ID: ", ""); }
                        //if (line.StartsWith("Firmware:")) { firmware = line.Replace("Firmware: ", ""); }
                    }
                }
            }

            return null;
        }

        public async Task<List<LiveTvTunerInfo>> GetTunerInfos(TunerHostInfo info, CancellationToken cancellationToken)
        {
            string model = await GetModelInfo(info, cancellationToken).ConfigureAwait(false);

            using (var stream = await _httpClient.Get(new HttpRequestOptions()
            {
                Url = string.Format("{0}/tuners.html", GetApiUrl(info, false)),
                CancellationToken = cancellationToken
            }))
            {
                var tuners = new List<LiveTvTunerInfo>();
                using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                {
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
                            tuners.Add(new LiveTvTunerInfo()
                            {
                                Name = name,
                                SourceType = string.IsNullOrWhiteSpace(model) ? Name : model,
                                ProgramName = currentChannel,
                                Status = status
                            });
                        }
                    }
                }
                return tuners;
            }
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
                    _logger.ErrorException("Error getting tuner info", ex);
                }
            }

            return list;
        }

        private string GetApiUrl(TunerHostInfo info, bool isPlayback)
        {
            var url = info.Url;

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

        private class Channels
        {
            public string GuideNumber { get; set; }
            public string GuideName { get; set; }
            public string URL { get; set; }
            public bool Favorite { get; set; }
            public bool DRM { get; set; }
        }

        private LiveTvOptions GetConfiguration()
        {
            return _config.GetConfiguration<LiveTvOptions>("livetv");
        }

        private MediaSourceInfo GetMediaSource(TunerHostInfo info, string channelId, string profile)
        {
            int? width = null;
            int? height = null;
            bool isInterlaced = true;
            var videoCodec = "mpeg2video";
            int? videoBitrate = null;

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
                videoBitrate = 8000000;
            }
            else if (string.Equals(profile, "internet720", StringComparison.OrdinalIgnoreCase))
            {
                width = 1280;
                height = 720;
                isInterlaced = false;
                videoCodec = "h264";
                videoBitrate = 5000000;
            }
            else if (string.Equals(profile, "internet540", StringComparison.OrdinalIgnoreCase))
            {
                width = 1280;
                height = 720;
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

            var url = GetApiUrl(info, true) + "/auto/v" + channelId;

            if (!string.IsNullOrWhiteSpace(profile) && !string.Equals(profile, "native", StringComparison.OrdinalIgnoreCase))
            {
                url += "?transcode=" + profile;
            }

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
                                BitRate = videoBitrate
                                
                            },
                            new MediaStream
                            {
                                Type = MediaStreamType.Audio,
                                // Set the index to -1 because we don't know the exact index of the audio stream within the container
                                Index = -1,
                                Codec = "ac3",
                                BitRate = 128000
                            }
                        },
                RequiresOpening = false,
                RequiresClosing = false,
                BufferMs = 1000,
                Container = "ts",
                Id = profile
            };

            return mediaSource;
        }

        public async Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(TunerHostInfo info, string channelId, CancellationToken cancellationToken)
        {
            var list = new List<MediaSourceInfo>();

            if (!channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return list;
            }
            channelId = channelId.Substring(ChannelIdPrefix.Length);

            list.Add(GetMediaSource(info, channelId, "native"));

            try
            {
                string model = await GetModelInfo(info, cancellationToken).ConfigureAwait(false);
                model = model ?? string.Empty;

                if (model.IndexOf("hdtc", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    list.Insert(0, GetMediaSource(info, channelId, "heavy"));

                    list.Add(GetMediaSource(info, channelId, "internet480"));
                    list.Add(GetMediaSource(info, channelId, "internet360"));
                    list.Add(GetMediaSource(info, channelId, "internet240"));
                    list.Add(GetMediaSource(info, channelId, "mobile"));
                }
            }
            catch (Exception ex)
            {

            }

            return list;
        }

        public async Task<MediaSourceInfo> GetChannelStream(TunerHostInfo info, string channelId, string streamId, CancellationToken cancellationToken)
        {
            if (!channelId.StartsWith(ChannelIdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            channelId = channelId.Substring(ChannelIdPrefix.Length);

            return GetMediaSource(info, channelId, streamId);
        }

        public async Task Validate(TunerHostInfo info)
        {
            await GetChannels(info, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
