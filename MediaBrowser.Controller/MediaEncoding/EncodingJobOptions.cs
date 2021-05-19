#nullable disable

#pragma warning disable CS1591

using System.Linq;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingJobOptions : BaseEncodingJobOptions
    {
        public string OutputDirectory { get; set; }

        public string ItemId { get; set; }

        public string TempDirectory { get; set; }

        public bool ReadInputAtNativeFramerate { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has fixed resolution.
        /// </summary>
        /// <value><c>true</c> if this instance has fixed resolution; otherwise, <c>false</c>.</value>
        public bool HasFixedResolution => Width.HasValue || Height.HasValue;

        public DeviceProfile DeviceProfile { get; set; }

        public EncodingJobOptions(StreamInfo info, DeviceProfile deviceProfile)
        {
            Container = info.Container;
            StartTimeTicks = info.StartPositionTicks;
            MaxWidth = info.MaxWidth;
            MaxHeight = info.MaxHeight;
            MaxFramerate = info.MaxFramerate;
            Id = info.ItemId;
            MediaSourceId = info.MediaSourceId;
            AudioCodec = info.TargetAudioCodec.FirstOrDefault();
            MaxAudioChannels = info.GlobalMaxAudioChannels;
            AudioBitRate = info.AudioBitrate;
            AudioSampleRate = info.TargetAudioSampleRate;
            DeviceProfile = deviceProfile;
            VideoCodec = info.TargetVideoCodec.FirstOrDefault();
            VideoBitRate = info.VideoBitrate;
            AudioStreamIndex = info.AudioStreamIndex;
            SubtitleMethod = info.SubtitleDeliveryMethod;
            Context = info.Context;
            TranscodingMaxAudioChannels = info.TranscodingMaxAudioChannels;

            if (info.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External)
            {
                SubtitleStreamIndex = info.SubtitleStreamIndex;
            }

            StreamOptions = info.StreamOptions;
        }
    }

    // For now until api and media encoding layers are unified
}
