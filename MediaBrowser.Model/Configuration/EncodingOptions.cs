
namespace MediaBrowser.Model.Configuration
{
    public class EncodingOptions
    {
        public int EncodingThreadCount { get; set; }
        public string TranscodingTempPath { get; set; }
        public double DownMixAudioBoost { get; set; }
        public bool EnableThrottling { get; set; }
        public int ThrottleDelaySeconds { get; set; }
        public string HardwareAccelerationType { get; set; }
        public string EncoderAppPath { get; set; }
        public string VaapiDevice { get; set; }
        public int H264Crf { get; set; }
        public string H264Preset { get; set; }
        public string DeinterlaceMethod { get; set; }
        public bool EnableHardwareEncoding { get; set; }
        public bool EnableSubtitleExtraction { get; set; }

        public string[] HardwareDecodingCodecs { get; set; }

        public EncodingOptions()
        {
            DownMixAudioBoost = 2;
            EnableThrottling = true;
            ThrottleDelaySeconds = 180;
            EncodingThreadCount = -1;
            // This is a DRM device that is almost guaranteed to be there on every intel platform, plus it's the default one in ffmpeg if you don't specify anything
            VaapiDevice = "/dev/dri/renderD128";
            H264Crf = 23;
            EnableHardwareEncoding = true;
            EnableSubtitleExtraction = true;
            HardwareDecodingCodecs = new string[] { "h264", "vc1" };
        }
    }
}
