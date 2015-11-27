
namespace MediaBrowser.Model.Configuration
{
    public class EncodingOptions
    {
        public int EncodingThreadCount { get; set; }
        public string TranscodingTempPath { get; set; }
        public double DownMixAudioBoost { get; set; }
        public bool EnableDebugLogging { get; set; }
        public bool EnableThrottling { get; set; }
        public int ThrottleThresholdSeconds { get; set; }
        public string HardwareAccelerationType { get; set; }

        public EncodingOptions()
        {
            DownMixAudioBoost = 2;
            EnableThrottling = true;
            ThrottleThresholdSeconds = 110;
            EncodingThreadCount = -1;
        }
    }
}
