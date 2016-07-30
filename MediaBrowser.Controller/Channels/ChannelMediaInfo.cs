using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelMediaInfo
    {
        public string Path { get; set; }

        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public string Container { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }

        public int? AudioBitrate { get; set; }
        public int? VideoBitrate { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? AudioChannels { get; set; }
        public int? AudioSampleRate { get; set; }

        public string VideoProfile { get; set; }
        public float? VideoLevel { get; set; }
        public float? Framerate { get; set; }

        public bool? IsAnamorphic { get; set; }

        public MediaProtocol Protocol { get; set; }

        public long? RunTimeTicks { get; set; }

        public string Id { get; set; }

        public bool ReadAtNativeFramerate { get; set; }
        public bool SupportsDirectPlay { get; set; }

        public ChannelMediaInfo()
        {
            RequiredHttpHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // This is most common
            Protocol = MediaProtocol.Http;
            SupportsDirectPlay = true;
        }

        public MediaSourceInfo ToMediaSource()
        {
            var id = Path.GetMD5().ToString("N");

            var source = new MediaSourceInfo
            {
                MediaStreams = GetMediaStreams(this).ToList(),

                Container = Container,
                Protocol = Protocol,
                Path = Path,
                RequiredHttpHeaders = RequiredHttpHeaders,
                RunTimeTicks = RunTimeTicks,
                Name = id,
                Id = id,
                ReadAtNativeFramerate = ReadAtNativeFramerate,
                SupportsDirectStream = Protocol == MediaProtocol.File,
                SupportsDirectPlay = SupportsDirectPlay
            };

            var bitrate = (AudioBitrate ?? 0) + (VideoBitrate ?? 0);

            if (bitrate > 0)
            {
                source.Bitrate = bitrate;
            }

            return source;
        }

        private IEnumerable<MediaStream> GetMediaStreams(ChannelMediaInfo info)
        {
            var list = new List<MediaStream>();

            if (!string.IsNullOrWhiteSpace(info.VideoCodec))
            {
                list.Add(new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Width = info.Width,
                    RealFrameRate = info.Framerate,
                    Profile = info.VideoProfile,
                    Level = info.VideoLevel,
                    Index = -1,
                    Height = info.Height,
                    Codec = info.VideoCodec,
                    BitRate = info.VideoBitrate,
                    AverageFrameRate = info.Framerate
                });
            }

            if (!string.IsNullOrWhiteSpace(info.AudioCodec))
            {
                list.Add(new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Index = -1,
                    Codec = info.AudioCodec,
                    BitRate = info.AudioBitrate,
                    Channels = info.AudioChannels,
                    SampleRate = info.AudioSampleRate
                });
            }

            return list;
        }
    }
}