
namespace MediaBrowser.Model.Configuration
{
    public class EncodingOptions
    {
        public EncodingQuality EncodingQuality { get; set; }
        public string TranscodingTempPath { get; set; }
        public double DownMixAudioBoost { get; set; }
        public string H264Encoder { get; set; }
        public bool EnableDebugLogging { get; set; }
        public bool EnableThrottling { get; set; }
        public int ThrottleThresholdSeconds { get; set; }

        public EncodingOptions()
        {
            H264Encoder = "libx264";
            DownMixAudioBoost = 2;
            EncodingQuality = EncodingQuality.Auto;
            EnableThrottling = true;
            ThrottleThresholdSeconds = 90;
        }
    }
}
