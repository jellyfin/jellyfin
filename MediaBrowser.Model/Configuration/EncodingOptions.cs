
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

        public EncodingOptions()
        {
            DownMixAudioBoost = 2;
            EnableThrottling = true;
            ThrottleDelaySeconds = 180;
            EncodingThreadCount = -1;
        }
    }
}
