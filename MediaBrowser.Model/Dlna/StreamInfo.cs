using System;
using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Class StreamInfo.
    /// </summary>
    public class StreamInfo
    {
        public string ItemId { get; set; }

        public string MediaSourceId { get; set; }

        public bool IsDirectStream { get; set; }

        public DlnaProfileType MediaType { get; set; }

        public string Container { get; set; }

        public long StartPositionTicks { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }

        public int? AudioStreamIndex { get; set; }

        public int? SubtitleStreamIndex { get; set; }

        public int? MaxAudioChannels { get; set; }

        public int? AudioBitrate { get; set; }

        public int? VideoBitrate { get; set; }

        public int? VideoLevel { get; set; }

        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }

        public int? MaxFramerate { get; set; }

        public string DeviceProfileId { get; set; }
        public string DeviceId { get; set; }

        public string ToUrl(string baseUrl)
        {
            return ToDlnaUrl(baseUrl);
        }

        public string ToDlnaUrl(string baseUrl)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                throw new ArgumentNullException(baseUrl);
            }

            var dlnaCommand = BuildDlnaParam(this);

            var extension = string.IsNullOrEmpty(Container) ? string.Empty : "." + Container;

            baseUrl = baseUrl.TrimEnd('/');

            if (MediaType == DlnaProfileType.Audio)
            {
                return string.Format("{0}/audio/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
            }
            return string.Format("{0}/videos/{1}/stream{2}?{3}", baseUrl, ItemId, extension, dlnaCommand);
        }

        private static string BuildDlnaParam(StreamInfo item)
        {
            var usCulture = new CultureInfo("en-US");

            var list = new List<string>
            {
                item.DeviceProfileId ?? string.Empty,
                item.DeviceId ?? string.Empty,
                item.MediaSourceId ?? string.Empty,
                (item.IsDirectStream).ToString().ToLower(),
                item.VideoCodec ?? string.Empty,
                item.AudioCodec ?? string.Empty,
                item.AudioStreamIndex.HasValue ? item.AudioStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.SubtitleStreamIndex.HasValue ? item.SubtitleStreamIndex.Value.ToString(usCulture) : string.Empty,
                item.VideoBitrate.HasValue ? item.VideoBitrate.Value.ToString(usCulture) : string.Empty,
                item.AudioBitrate.HasValue ? item.AudioBitrate.Value.ToString(usCulture) : string.Empty,
                item.MaxAudioChannels.HasValue ? item.MaxAudioChannels.Value.ToString(usCulture) : string.Empty,
                item.MaxFramerate.HasValue ? item.MaxFramerate.Value.ToString(usCulture) : string.Empty,
                item.MaxWidth.HasValue ? item.MaxWidth.Value.ToString(usCulture) : string.Empty,
                item.MaxHeight.HasValue ? item.MaxHeight.Value.ToString(usCulture) : string.Empty,
                item.StartPositionTicks.ToString(usCulture),
                item.VideoLevel.HasValue ? item.VideoLevel.Value.ToString(usCulture) : string.Empty
            };

            return string.Format("Params={0}", string.Join(";", list.ToArray()));
        }

    }

    /// <summary>
    /// Class AudioOptions.
    /// </summary>
    public class AudioOptions
    {
        public string ItemId { get; set; }
        public List<MediaSourceInfo> MediaSources { get; set; }
        public int? MaxBitrateSetting { get; set; }
        public DeviceProfile Profile { get; set; }
        public string MediaSourceId { get; set; }
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// Class VideoOptions.
    /// </summary>
    public class VideoOptions : AudioOptions
    {
        public int? AudioStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
    }
}
