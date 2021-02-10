#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Configuration
{
    public class EncodingOptions
    {
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
        /// FFmpeg path as set by the user via the UI.
        /// </summary>
        public string EncoderAppPath { get; set; }

        /// <summary>
        /// The current FFmpeg path being used by the system and displayed on the transcode page.
        /// </summary>
        public string EncoderAppPathDisplay { get; set; }

        public string VaapiDevice { get; set; }

        public string OpenclDevice { get; set; }

        public bool EnableTonemapping { get; set; }

        public bool EnableVppTonemapping { get; set; }

        public string TonemappingAlgorithm { get; set; }

        public string TonemappingRange { get; set; }

        public double TonemappingDesat { get; set; }

        public double TonemappingThreshold { get; set; }

        public double TonemappingPeak { get; set; }

        public double TonemappingParam { get; set; }

        public int H264Crf { get; set; }

        public int H265Crf { get; set; }

        public string EncoderPreset { get; set; }

        public bool DeinterlaceDoubleRate { get; set; }

        public string DeinterlaceMethod { get; set; }

        public bool EnableDecodingColorDepth10Hevc { get; set; }

        public bool EnableDecodingColorDepth10Vp9 { get; set; }

        public bool EnableEnhancedNvdecDecoder { get; set; }

        public bool EnableHardwareEncoding { get; set; }

        public bool AllowHevcEncoding { get; set; }

        public bool EnableSubtitleExtraction { get; set; }

        public string[] HardwareDecodingCodecs { get; set; }

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
            // This is the OpenCL device that is used for tonemapping.
            // The left side of the dot is the platform number, and the right side is the device number on the platform.
            OpenclDevice = "0.0";
            EnableTonemapping = false;
            EnableVppTonemapping = false;
            TonemappingAlgorithm = "hable";
            TonemappingRange = "auto";
            TonemappingDesat = 0;
            TonemappingThreshold = 0.8;
            TonemappingPeak = 100;
            TonemappingParam = 0;
            H264Crf = 23;
            H265Crf = 28;
            DeinterlaceDoubleRate = false;
            DeinterlaceMethod = "yadif";
            EnableDecodingColorDepth10Hevc = true;
            EnableDecodingColorDepth10Vp9 = true;
            EnableEnhancedNvdecDecoder = true;
            EnableHardwareEncoding = true;
            AllowHevcEncoding = true;
            EnableSubtitleExtraction = true;
            HardwareDecodingCodecs = new string[] { "h264", "vc1" };
        }
    }
}
