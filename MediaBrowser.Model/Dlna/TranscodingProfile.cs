using System.Collections.Generic;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class TranscodingProfile
    {
        public string Container { get; set; }

        public DlnaProfileType Type { get; set; }

        public string VideoCodec { get; set; }

        public string AudioCodec { get; set; }

        public string Protocol { get; set; }

        public bool EstimateContentLength { get; set; }

        public bool EnableMpegtsM2TsMode { get; set; }

        public TranscodeSeekInfo TranscodeSeekInfo { get; set; }

        public bool CopyTimestamps { get; set; }

        public EncodingContext Context { get; set; }

        public bool EnableSubtitlesInManifest { get; set; }

        public bool EnableSplittingOnNonKeyFrames { get; set; }

        public string MaxAudioChannels { get; set; }

        public List<string> GetAudioCodecs()
        {
            List<string> list = new List<string>();
            foreach (string i in (AudioCodec ?? string.Empty).Split(','))
            {
                if (!string.IsNullOrEmpty(i)) list.Add(i);
            }
            return list;
        }
    }
}
