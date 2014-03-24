using System.Collections.Generic;

namespace MediaBrowser.Controller.Dlna
{
    public class TranscodingProfile
    {
        public string Container { get; set; }

        public DlnaProfileType Type { get; set; }

        public string VideoCodec { get; set; }
        public string AudioCodec { get; set; }

        public List<TranscodingSetting> Settings { get; set; }

        public TranscodingProfile()
        {
            Settings = new List<TranscodingSetting>();
        }
    }

    public class TranscodingSetting
    {
        public TranscodingSettingType Name { get; set; }
        public string Value { get; set; }
    }

    public enum TranscodingSettingType
    {
        Profile
    }
}
