#pragma warning disable CA1819 // XML serialization handles collections improperly, so we need to use arrays

#nullable disable
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Class EncodingOptions.
/// </summary>
public class EncodingOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncodingOptions" /> class.
    /// </summary>
    public EncodingOptions()
    {
        EnableFallbackFont = false;
        EnableAudioVbr = false;
        DownMixAudioBoost = 2;
        DownMixStereoAlgorithm = DownMixStereoAlgorithms.None;
        MaxMuxingQueueSize = 2048;
        EnableThrottling = false;
        ThrottleDelaySeconds = 180;
        EnableSegmentDeletion = false;
        SegmentKeepSeconds = 720;
        EncodingThreadCount = -1;
        // This is a DRM device that is almost guaranteed to be there on every intel platform,
        // plus it's the default one in ffmpeg if you don't specify anything
        VaapiDevice = "/dev/dri/renderD128";
        QsvDevice = string.Empty;
        EnableTonemapping = false;
        EnableVppTonemapping = false;
        EnableVideoToolboxTonemapping = false;
        TonemappingAlgorithm = TonemappingAlgorithm.bt2390;
        TonemappingMode = TonemappingMode.auto;
        TonemappingRange = TonemappingRange.auto;
        TonemappingDesat = 0;
        TonemappingPeak = 100;
        TonemappingParam = 0;
        VppTonemappingBrightness = 16;
        VppTonemappingContrast = 1;
        H264Crf = 23;
        H265Crf = 28;
        DeinterlaceDoubleRate = false;
        DeinterlaceMethod = DeinterlaceMethod.yadif;
        EnableDecodingColorDepth10Hevc = true;
        EnableDecodingColorDepth10Vp9 = true;
        EnableDecodingColorDepth10HevcRext = false;
        EnableDecodingColorDepth12HevcRext = false;
        // Enhanced Nvdec or system native decoder is required for DoVi to SDR tone-mapping.
        EnableEnhancedNvdecDecoder = true;
        PreferSystemNativeHwDecoder = true;
        EnableIntelLowPowerH264HwEncoder = false;
        EnableIntelLowPowerHevcHwEncoder = false;
        EnableHardwareEncoding = true;
        AllowHevcEncoding = false;
        AllowAv1Encoding = false;
        EnableSubtitleExtraction = true;
        AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = ["mkv"];
        HardwareDecodingCodecs = ["h264", "vc1"];
    }

    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public int EncodingThreadCount { get; set; }

    /// <summary>
    /// Gets or sets the temporary transcoding path.
    /// </summary>
    public string TranscodingTempPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the fallback font.
    /// </summary>
    public string FallbackFontPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the fallback font.
    /// </summary>
    public bool EnableFallbackFont { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether audio VBR is enabled.
    /// </summary>
    public bool EnableAudioVbr { get; set; }

    /// <summary>
    /// Gets or sets the audio boost applied when downmixing audio.
    /// </summary>
    public double DownMixAudioBoost { get; set; }

    /// <summary>
    /// Gets or sets the algorithm used for downmixing audio to stereo.
    /// </summary>
    public DownMixStereoAlgorithms DownMixStereoAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the muxing queue.
    /// </summary>
    public int MaxMuxingQueueSize { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether throttling is enabled.
    /// </summary>
    public bool EnableThrottling { get; set; }

    /// <summary>
    /// Gets or sets the delay after which throttling happens.
    /// </summary>
    public int ThrottleDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether segment deletion is enabled.
    /// </summary>
    public bool EnableSegmentDeletion { get; set; }

    /// <summary>
    /// Gets or sets seconds for which segments should be kept before being deleted.
    /// </summary>
    public int SegmentKeepSeconds { get; set; }

    /// <summary>
    /// Gets or sets the hardware acceleration type.
    /// </summary>
    public HardwareAccelerationType HardwareAccelerationType { get; set; }

    /// <summary>
    /// Gets or sets the FFmpeg path as set by the user via the UI.
    /// </summary>
    public string EncoderAppPath { get; set; }

    /// <summary>
    /// Gets or sets the current FFmpeg path being used by the system and displayed on the transcode page.
    /// </summary>
    public string EncoderAppPathDisplay { get; set; }

    /// <summary>
    /// Gets or sets the VA-API device.
    /// </summary>
    public string VaapiDevice { get; set; }

    /// <summary>
    /// Gets or sets the QSV device.
    /// </summary>
    public string QsvDevice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether tonemapping is enabled.
    /// </summary>
    public bool EnableTonemapping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether VPP tonemapping is enabled.
    /// </summary>
    public bool EnableVppTonemapping { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether videotoolbox tonemapping is enabled.
    /// </summary>
    public bool EnableVideoToolboxTonemapping { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping algorithm.
    /// </summary>
    public TonemappingAlgorithm TonemappingAlgorithm { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping mode.
    /// </summary>
    public TonemappingMode TonemappingMode { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping range.
    /// </summary>
    public TonemappingRange TonemappingRange { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping desaturation.
    /// </summary>
    public double TonemappingDesat { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping peak.
    /// </summary>
    public double TonemappingPeak { get; set; }

    /// <summary>
    /// Gets or sets the tone-mapping parameters.
    /// </summary>
    public double TonemappingParam { get; set; }

    /// <summary>
    /// Gets or sets the VPP tone-mapping brightness.
    /// </summary>
    public double VppTonemappingBrightness { get; set; }

    /// <summary>
    /// Gets or sets the VPP tone-mapping contrast.
    /// </summary>
    public double VppTonemappingContrast { get; set; }

    /// <summary>
    /// Gets or sets the H264 CRF.
    /// </summary>
    public int H264Crf { get; set; }

    /// <summary>
    /// Gets or sets the H265 CRF.
    /// </summary>
    public int H265Crf { get; set; }

    /// <summary>
    /// Gets or sets the encoder preset.
    /// </summary>
    public EncoderPreset? EncoderPreset { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the framerate is doubled when deinterlacing.
    /// </summary>
    public bool DeinterlaceDoubleRate { get; set; }

    /// <summary>
    /// Gets or sets the deinterlace method.
    /// </summary>
    public DeinterlaceMethod DeinterlaceMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 10bit HEVC decoding is enabled.
    /// </summary>
    public bool EnableDecodingColorDepth10Hevc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 10bit VP9 decoding is enabled.
    /// </summary>
    public bool EnableDecodingColorDepth10Vp9 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 8/10bit HEVC RExt decoding is enabled.
    /// </summary>
    public bool EnableDecodingColorDepth10HevcRext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether 12bit HEVC RExt decoding is enabled.
    /// </summary>
    public bool EnableDecodingColorDepth12HevcRext { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the enhanced NVDEC is enabled.
    /// </summary>
    public bool EnableEnhancedNvdecDecoder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the system native hardware decoder should be used.
    /// </summary>
    public bool PreferSystemNativeHwDecoder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Intel H264 low-power hardware encoder should be used.
    /// </summary>
    public bool EnableIntelLowPowerH264HwEncoder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Intel HEVC low-power hardware encoder should be used.
    /// </summary>
    public bool EnableIntelLowPowerHevcHwEncoder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hardware encoding is enabled.
    /// </summary>
    public bool EnableHardwareEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HEVC encoding is enabled.
    /// </summary>
    public bool AllowHevcEncoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether AV1 encoding is enabled.
    /// </summary>
    public bool AllowAv1Encoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether subtitle extraction is enabled.
    /// </summary>
    public bool EnableSubtitleExtraction { get; set; }

    /// <summary>
    /// Gets or sets the codecs hardware encoding is used for.
    /// </summary>
    public string[] HardwareDecodingCodecs { get; set; }

    /// <summary>
    /// Gets or sets the file extensions on-demand metadata based keyframe extraction is enabled for.
    /// </summary>
    public string[] AllowOnDemandMetadataBasedKeyframeExtractionForExtensions { get; set; }
}
