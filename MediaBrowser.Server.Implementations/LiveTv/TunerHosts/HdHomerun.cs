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

namespace MediaBrowser.Server.Implementations.LiveTv.TunerHosts
{
    public class HdHomerun : ITunerHost
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IConfigurationManager _config;

        public HdHomerun(IHttpClient httpClient, ILogger logger, IJsonSerializer jsonSerializer, IConfigurationManager config)
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
            get { return "hdhomerun"; }
        }

        public async Task<IEnumerable<ChannelInfo>> GetChannels(TunerHostInfo info, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = string.Format("{0}/lineup.json", GetApiUrl(info)),
                CancellationToken = cancellationToken
            };
            using (var stream = await _httpClient.Get(options))
            {
                var root = _jsonSerializer.DeserializeFromStream<List<Channels>>(stream);

                if (root != null)
                {
                    return root.Select(i => new ChannelInfo
                    {
                        Name = i.GuideName,
                        Number = i.GuideNumber.ToString(CultureInfo.InvariantCulture),
                        Id = i.GuideNumber.ToString(CultureInfo.InvariantCulture),
                        IsFavorite = i.Favorite

                    });
                }
                return new List<ChannelInfo>();
            }
        }

        public async Task<List<LiveTvTunerInfo>> GetTunerInfos(TunerHostInfo info, CancellationToken cancellationToken)
        {
            string model = null;

            using (var stream = await _httpClient.Get(new HttpRequestOptions()
            {
                Url = string.Format("{0}/", GetApiUrl(info)),
                CancellationToken = cancellationToken
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

            using (var stream = await _httpClient.Get(new HttpRequestOptions()
            {
                Url = string.Format("{0}/tuners.html", GetApiUrl(info)),
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

        public string GetApiUrl(TunerHostInfo info)
        {
            var url = info.Url;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            return url.TrimEnd('/');
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

        public List<TunerHostInfo> GetTunerHosts()
        {
            return GetConfiguration().TunerHosts.Where(i => string.Equals(i.Type, Type, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task<MediaSourceInfo> GetChannelStream(TunerHostInfo info, string channelId, string streamId, CancellationToken cancellationToken)
        {
            var channels = await GetChannels(info, cancellationToken).ConfigureAwait(false);
            var tuners = await GetTunerInfos(info, cancellationToken).ConfigureAwait(false);

            var channel = channels.FirstOrDefault(c => string.Equals(c.Id, channelId, StringComparison.OrdinalIgnoreCase));
            if (channel != null)
            {
                if (tuners.FindIndex(t => t.Status == LiveTvTunerStatus.Available) >= 0)
                {
                    return new MediaSourceInfo
                    {
                        Path = GetApiUrl(info) + "/auto/v" + channelId,
                        Protocol = MediaProtocol.Http,
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
                        }
                    };
                }

                throw new ApplicationException("No tuners avaliable.");
            } 
            throw new ApplicationException("Channel not found.");
        }


        public async Task Validate(TunerHostInfo info)
        {
            await GetChannels(info, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
