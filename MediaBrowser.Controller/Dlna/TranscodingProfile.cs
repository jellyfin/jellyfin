using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Dlna
{
    public class TranscodingProfile
    {
        public string Container { get; set; }

        public DlnaProfileType Type { get; set; }

        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }

        public bool EstimateContentLength { get; set; }
        public bool EnableMpegtsM2TsMode { get; set; }
        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public TranscodingSetting[] Settings { get; set; }

        public TranscodingProfile()
        {
            Settings = new TranscodingSetting[] { };
        }


        public List<string> GetAudioCodecs()
        {
            return (AudioCodec ?? string.Empty).Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }
    }

    public class TranscodingSetting
    {
        public TranscodingSettingType Name { get; set; }
        public string Value { get; set; }
    }

    public enum TranscodingSettingType
    {
        VideoProfile = 0
    }

    public enum TranscodeSeekInfo
    {
        Auto = 0,
        Bytes = 1
    }
}
