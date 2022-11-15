#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class EncodingOptions
    {
        public EncodingOptions()
        {
            EnableFallbackFont = false;
            DownMixAudioBoost = 2;
            MaxMuxingQueueSize = 2048;
            EnableThrottling = false;
            ThrottleDelaySeconds = 180;
            EncodingThreadCount = -1;
            // This is a DRM device that is almost guaranteed to be there on every intel platform,
            // plus it's the default one in ffmpeg if you don't specify anything
            VaapiDevice = "/dev/dri/renderD128";
            EnableTonemapping = false;
            EnableVppTonemapping = false;
            TonemappingAlgorithm = "bt2390";
            TonemappingRange = "auto";
            TonemappingDesat = 0;
            TonemappingThreshold = 0.8;
            TonemappingPeak = 100;
            TonemappingParam = 0;
            VppTonemappingBrightness = 0;
            VppTonemappingContrast = 1.2;
            H264Crf = 23;
            H265Crf = 28;
            DeinterlaceDoubleRate = false;
            DeinterlaceMethod = "yadif";
            EnableDecodingColorDepth10Hevc = true;
            EnableDecodingColorDepth10Vp9 = true;
            EnableEnhancedNvdecDecoder = false;
            PreferSystemNativeHwDecoder = true;
            EnableIntelLowPowerH264HwEncoder = false;
            EnableIntelLowPowerHevcHwEncoder = false;
            EnableHardwareEncoding = true;
            AllowHevcEncoding = false;
            EnableSubtitleExtraction = true;
            AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = new[] { "mkv" };
            HardwareDecodingCodecs = new string[] { "h264", "vc1" };
        }

        public int EncodingThreadCount { get; set; }

        public string TranscodingTempPath { get; set; }

        public string FallbackFontPath { get; set; }

        public bool EnableFallbackFont { get; set; }

        public double DownMixAudioBoost { get; set; }

        public int MaxMuxingQueueSize { get; set; }

        public bool EnableThrottling { get; set; }

        public int ThrottleDelaySeconds { get; set; }

        public string HardwareAccelerationType { get; set; }

        /// <summary>
        /// Gets or sets the FFmpeg path as set by the user via the UI.
        /// </summary>
        public string EncoderAppPath { get; set; }

        /// <summary>
        /// Gets or sets the current FFmpeg path being used by the system and displayed on the transcode page.
        /// </summary>
        public string EncoderAppPathDisplay { get; set; }

        public string VaapiDevice { get; set; }

        public bool EnableTonemapping { get; set; }

        public bool EnableVppTonemapping { get; set; }

        public string TonemappingAlgorithm { get; set; }

        public string TonemappingRange { get; set; }

        public double TonemappingDesat { get; set; }

        public double TonemappingThreshold { get; set; }

        public double TonemappingPeak { get; set; }

        public double TonemappingParam { get; set; }

        public double VppTonemappingBrightness { get; set; }

        public double VppTonemappingContrast { get; set; }

        public int H264Crf { get; set; }

        public int H265Crf { get; set; }

        public string EncoderPreset { get; set; }

        public bool DeinterlaceDoubleRate { get; set; }

        public string DeinterlaceMethod { get; set; }

        public bool EnableDecodingColorDepth10Hevc { get; set; }

        public bool EnableDecodingColorDepth10Vp9 { get; set; }

        public bool EnableEnhancedNvdecDecoder { get; set; }

        public bool PreferSystemNativeHwDecoder { get; set; }

        public bool EnableIntelLowPowerH264HwEncoder { get; set; }

        public bool EnableIntelLowPowerHevcHwEncoder { get; set; }

        public bool EnableHardwareEncoding { get; set; }

        public bool AllowHevcEncoding { get; set; }

        public bool EnableSubtitleExtraction { get; set; }

        public string[] HardwareDecodingCodecs { get; set; }

        public string[] AllowOnDemandMetadataBasedKeyframeExtractionForExtensions { get; set; }
    }
}
