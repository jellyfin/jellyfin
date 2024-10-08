using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Emby.Server.Implementations;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.PreStartupRoutines;

/// <inheritdoc />
public class MigrateEncodingOptions : IMigrationRoutine
{
    private readonly ServerApplicationPaths _applicationPaths;
    private readonly ILogger<MigrateEncodingOptions> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateEncodingOptions"/> class.
    /// </summary>
    /// <param name="applicationPaths">An instance of <see cref="ServerApplicationPaths"/>.</param>
    /// <param name="loggerFactory">An instance of the <see cref="ILoggerFactory"/> interface.</param>
    public MigrateEncodingOptions(ServerApplicationPaths applicationPaths, ILoggerFactory loggerFactory)
    {
        _applicationPaths = applicationPaths;
        _logger = loggerFactory.CreateLogger<MigrateEncodingOptions>();
    }

    /// <inheritdoc />
    public Guid Id => Guid.Parse("A8E61960-7726-4450-8F3D-82C12DAABBCB");

    /// <inheritdoc />
    public string Name => nameof(MigrateEncodingOptions);

    /// <inheritdoc />
    public bool PerformOnNewInstall => false;

    /// <inheritdoc />
    public void Perform()
    {
        string path = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, "encoding.xml");
        var oldSerializer = new XmlSerializer(typeof(OldEncodingOptions), new XmlRootAttribute("EncodingOptions"));
        OldEncodingOptions? oldConfig = null;

        try
        {
            using var xmlReader = XmlReader.Create(path);
            oldConfig = (OldEncodingOptions?)oldSerializer.Deserialize(xmlReader);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Migrate EncodingOptions deserialize Invalid Operation error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migrate EncodingOptions deserialize error");
        }

        if (oldConfig is null)
        {
            return;
        }

        var hardwareAccelerationType = HardwareAccelerationType.none;
        if (Enum.TryParse<HardwareAccelerationType>(oldConfig.HardwareAccelerationType, true, out var parsedHardwareAccelerationType))
        {
            hardwareAccelerationType = parsedHardwareAccelerationType;
        }

        var tonemappingAlgorithm = TonemappingAlgorithm.none;
        if (Enum.TryParse<TonemappingAlgorithm>(oldConfig.TonemappingAlgorithm, true, out var parsedTonemappingAlgorithm))
        {
            tonemappingAlgorithm = parsedTonemappingAlgorithm;
        }

        var tonemappingMode = TonemappingMode.auto;
        if (Enum.TryParse<TonemappingMode>(oldConfig.TonemappingMode, true, out var parsedTonemappingMode))
        {
            tonemappingMode = parsedTonemappingMode;
        }

        var tonemappingRange = TonemappingRange.auto;
        if (Enum.TryParse<TonemappingRange>(oldConfig.TonemappingRange, true, out var parsedTonemappingRange))
        {
            tonemappingRange = parsedTonemappingRange;
        }

        var encoderPreset = EncoderPreset.superfast;
        if (Enum.TryParse<EncoderPreset>(oldConfig.TonemappingRange, true, out var parsedEncoderPreset))
        {
            encoderPreset = parsedEncoderPreset;
        }

        var deinterlaceMethod = DeinterlaceMethod.yadif;
        if (Enum.TryParse<DeinterlaceMethod>(oldConfig.TonemappingRange, true, out var parsedDeinterlaceMethod))
        {
            deinterlaceMethod = parsedDeinterlaceMethod;
        }

        var encodingOptions = new EncodingOptions()
        {
            EncodingThreadCount = oldConfig.EncodingThreadCount,
            TranscodingTempPath = oldConfig.TranscodingTempPath,
            FallbackFontPath = oldConfig.FallbackFontPath,
            EnableFallbackFont = oldConfig.EnableFallbackFont,
            EnableAudioVbr = oldConfig.EnableAudioVbr,
            DownMixAudioBoost = oldConfig.DownMixAudioBoost,
            DownMixStereoAlgorithm = oldConfig.DownMixStereoAlgorithm,
            MaxMuxingQueueSize = oldConfig.MaxMuxingQueueSize,
            EnableThrottling = oldConfig.EnableThrottling,
            ThrottleDelaySeconds = oldConfig.ThrottleDelaySeconds,
            EnableSegmentDeletion = oldConfig.EnableSegmentDeletion,
            SegmentKeepSeconds = oldConfig.SegmentKeepSeconds,
            HardwareAccelerationType = hardwareAccelerationType,
            EncoderAppPath = oldConfig.EncoderAppPath,
            EncoderAppPathDisplay = oldConfig.EncoderAppPathDisplay,
            VaapiDevice = oldConfig.VaapiDevice,
            EnableTonemapping = oldConfig.EnableTonemapping,
            EnableVppTonemapping = oldConfig.EnableVppTonemapping,
            EnableVideoToolboxTonemapping = oldConfig.EnableVideoToolboxTonemapping,
            TonemappingAlgorithm = tonemappingAlgorithm,
            TonemappingMode = tonemappingMode,
            TonemappingRange = tonemappingRange,
            TonemappingDesat = oldConfig.TonemappingDesat,
            TonemappingPeak = oldConfig.TonemappingPeak,
            TonemappingParam = oldConfig.TonemappingParam,
            VppTonemappingBrightness = oldConfig.VppTonemappingBrightness,
            VppTonemappingContrast = oldConfig.VppTonemappingContrast,
            H264Crf = oldConfig.H264Crf,
            H265Crf = oldConfig.H265Crf,
            EncoderPreset = encoderPreset,
            DeinterlaceDoubleRate = oldConfig.DeinterlaceDoubleRate,
            DeinterlaceMethod = deinterlaceMethod,
            EnableDecodingColorDepth10Hevc = oldConfig.EnableDecodingColorDepth10Hevc,
            EnableDecodingColorDepth10Vp9 = oldConfig.EnableDecodingColorDepth10Vp9,
            EnableEnhancedNvdecDecoder = oldConfig.EnableEnhancedNvdecDecoder,
            PreferSystemNativeHwDecoder = oldConfig.PreferSystemNativeHwDecoder,
            EnableIntelLowPowerH264HwEncoder = oldConfig.EnableIntelLowPowerH264HwEncoder,
            EnableIntelLowPowerHevcHwEncoder = oldConfig.EnableIntelLowPowerHevcHwEncoder,
            EnableHardwareEncoding = oldConfig.EnableHardwareEncoding,
            AllowHevcEncoding = oldConfig.AllowHevcEncoding,
            AllowAv1Encoding = oldConfig.AllowAv1Encoding,
            EnableSubtitleExtraction = oldConfig.EnableSubtitleExtraction,
            HardwareDecodingCodecs = oldConfig.HardwareDecodingCodecs,
            AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = oldConfig.AllowOnDemandMetadataBasedKeyframeExtractionForExtensions
        };

        var newSerializer = new XmlSerializer(typeof(EncodingOptions));
        var xmlWriterSettings = new XmlWriterSettings { Indent = true };
        using var xmlWriter = XmlWriter.Create(path, xmlWriterSettings);
        newSerializer.Serialize(xmlWriter, encodingOptions);
    }

#pragma warning disable
    public sealed class OldEncodingOptions
    {
        public int EncodingThreadCount { get; set; }

        public string TranscodingTempPath { get; set; }

        public string FallbackFontPath { get; set; }

        public bool EnableFallbackFont { get; set; }

        public bool EnableAudioVbr { get; set; }

        public double DownMixAudioBoost { get; set; }

        public DownMixStereoAlgorithms DownMixStereoAlgorithm { get; set; }

        public int MaxMuxingQueueSize { get; set; }

        public bool EnableThrottling { get; set; }

        public int ThrottleDelaySeconds { get; set; }

        public bool EnableSegmentDeletion { get; set; }

        public int SegmentKeepSeconds { get; set; }

        public string HardwareAccelerationType { get; set; }

        public string EncoderAppPath { get; set; }

        public string EncoderAppPathDisplay { get; set; }

        public string VaapiDevice { get; set; }

        public bool EnableTonemapping { get; set; }

        public bool EnableVppTonemapping { get; set; }

        public bool EnableVideoToolboxTonemapping { get; set; }

        public string TonemappingAlgorithm { get; set; }

        public string TonemappingMode { get; set; }

        public string TonemappingRange { get; set; }

        public double TonemappingDesat { get; set; }

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

        public bool AllowAv1Encoding { get; set; }

        public bool EnableSubtitleExtraction { get; set; }

        public string[] HardwareDecodingCodecs { get; set; }

        public string[] AllowOnDemandMetadataBasedKeyframeExtractionForExtensions { get; set; }
    }
}
