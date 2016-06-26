using System.Linq;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class EncodingJobOptions
    {
        public string OutputContainer { get; set; }
        public string OutputDirectory { get; set; }

        public long? StartTimeTicks { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? MaxWidth { get; set; }
        public int? MaxHeight { get; set; }
        public bool Static = false;
        public float? Framerate { get; set; }
        public float? MaxFramerate { get; set; }
        public string Profile { get; set; }
        public int? Level { get; set; }

        public string DeviceId { get; set; }
        public string ItemId { get; set; }
        public string MediaSourceId { get; set; }
        public string AudioCodec { get; set; }

        public bool EnableAutoStreamCopy { get; set; }

        public int? MaxAudioChannels { get; set; }
        public int? AudioChannels { get; set; }
        public int? AudioBitRate { get; set; }
        public int? AudioSampleRate { get; set; }
   
        public DeviceProfile DeviceProfile { get; set; }
        public EncodingContext Context { get; set; }

        public string VideoCodec { get; set; }

        public int? VideoBitRate { get; set; }
        public int? AudioStreamIndex { get; set; }
        public int? VideoStreamIndex { get; set; }
        public int? SubtitleStreamIndex { get; set; }
        public int? MaxRefFrames { get; set; }
        public int? MaxVideoBitDepth { get; set; }
        public int? CpuCoreLimit { get; set; }
        public bool ReadInputAtNativeFramerate { get; set; }
        public SubtitleDeliveryMethod SubtitleMethod { get; set; }
        public bool CopyTimestamps { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has fixed resolution.
        /// </summary>
        /// <value><c>true</c> if this instance has fixed resolution; otherwise, <c>false</c>.</value>
        public bool HasFixedResolution
        {
            get
            {
                return Width.HasValue || Height.HasValue;
            }
        }

        public EncodingJobOptions()
        {
            
        }

        public EncodingJobOptions(StreamInfo info, DeviceProfile deviceProfile)
        {
            OutputContainer = info.Container;
            StartTimeTicks = info.StartPositionTicks;
            MaxWidth = info.MaxWidth;
            MaxHeight = info.MaxHeight;
            MaxFramerate = info.MaxFramerate;
            Profile = info.VideoProfile;
            Level = info.VideoLevel;
            ItemId = info.ItemId;
            MediaSourceId = info.MediaSourceId;
            AudioCodec = info.TargetAudioCodec;
            MaxAudioChannels = info.MaxAudioChannels;
            AudioBitRate = info.AudioBitrate;
            AudioSampleRate = info.TargetAudioSampleRate;
            DeviceProfile = deviceProfile;
            VideoCodec = info.VideoCodec;
            VideoBitRate = info.VideoBitrate;
            AudioStreamIndex = info.AudioStreamIndex;
            MaxRefFrames = info.MaxRefFrames;
            MaxVideoBitDepth = info.MaxVideoBitDepth;
            SubtitleMethod = info.SubtitleDeliveryMethod;
            Context = info.Context;

            if (info.SubtitleDeliveryMethod != SubtitleDeliveryMethod.External)
            {
                SubtitleStreamIndex = info.SubtitleStreamIndex;
            }
        }
    }
}
