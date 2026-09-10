using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Helper for mapping between configuration DTOs and their internal counterparts.
/// </summary>
public static class ConfigurationMapper
{
    /// <summary>
    /// Maps a <see cref="ServerConfiguration"/> to a <see cref="ServerConfigurationDto"/>.
    /// </summary>
    /// <param name="config">The <see cref="ServerConfiguration"/>.</param>
    /// <returns>The <see cref="ServerConfigurationDto"/>.</returns>
    public static ServerConfigurationDto MapToDto(ServerConfiguration config)
    {
        return new ServerConfigurationDto
        {
            LogFileRetentionDays = config.LogFileRetentionDays,
            IsStartupWizardCompleted = config.IsStartupWizardCompleted,
            CachePath = config.CachePath,
            PreviousVersionStr = config.PreviousVersionStr,
            EnableMetrics = config.EnableMetrics,
            EnableNormalizedItemByNameIds = config.EnableNormalizedItemByNameIds,
            IsPortAuthorized = config.IsPortAuthorized,
            QuickConnectAvailable = config.QuickConnectAvailable,
            EnableCaseSensitiveItemIds = config.EnableCaseSensitiveItemIds,
            DisableLiveTvChannelUserDataName = config.DisableLiveTvChannelUserDataName,
            MetadataPath = config.MetadataPath,
            PreferredMetadataLanguage = config.PreferredMetadataLanguage,
            MetadataCountryCode = config.MetadataCountryCode,
            SortReplaceCharacters = config.SortReplaceCharacters,
            SortRemoveCharacters = config.SortRemoveCharacters,
            SortRemoveWords = config.SortRemoveWords,
            MinResumePct = config.MinResumePct,
            MaxResumePct = config.MaxResumePct,
            MinResumeDurationSeconds = config.MinResumeDurationSeconds,
            MinAudiobookResume = config.MinAudiobookResume,
            MaxAudiobookResume = config.MaxAudiobookResume,
            InactiveSessionThreshold = config.InactiveSessionThreshold,
            LibraryMonitorDelay = config.LibraryMonitorDelay,
            LibraryUpdateDuration = config.LibraryUpdateDuration,
            CacheSize = config.CacheSize,
            ImageSavingConvention = config.ImageSavingConvention,
            MetadataOptions = config.MetadataOptions,
            SkipDeserializationForBasicTypes = config.SkipDeserializationForBasicTypes,
            ServerName = config.ServerName,
            UICulture = config.UICulture,
            SaveMetadataHidden = config.SaveMetadataHidden,
            ContentTypes = config.ContentTypes,
            RemoteClientBitrateLimit = config.RemoteClientBitrateLimit,
            EnableFolderView = config.EnableFolderView,
            EnableGroupingMoviesIntoCollections = config.EnableGroupingMoviesIntoCollections,
            EnableGroupingShowsIntoCollections = config.EnableGroupingShowsIntoCollections,
            DisplaySpecialsWithinSeasons = config.DisplaySpecialsWithinSeasons,
            CodecsUsed = config.CodecsUsed,
            PluginRepositories = config.PluginRepositories,
            EnableExternalContentInSuggestions = config.EnableExternalContentInSuggestions,
            ImageExtractionTimeoutMs = config.ImageExtractionTimeoutMs,
            PathSubstitutions = config.PathSubstitutions,
            EnableSlowResponseWarning = config.EnableSlowResponseWarning,
            SlowResponseThresholdMs = config.SlowResponseThresholdMs,
            CorsHosts = config.CorsHosts,
            ActivityLogRetentionDays = config.ActivityLogRetentionDays,
            LibraryScanFanoutConcurrency = config.LibraryScanFanoutConcurrency,
            LibraryMetadataRefreshConcurrency = config.LibraryMetadataRefreshConcurrency,
            AllowClientLogUpload = config.AllowClientLogUpload,
            DummyChapterDuration = config.DummyChapterDuration,
            ChapterImageResolution = config.ChapterImageResolution,
            ParallelImageEncodingLimit = config.ParallelImageEncodingLimit,
            CastReceiverApplications = config.CastReceiverApplications,
            TrickplayOptions = config.TrickplayOptions,
            EnableLegacyAuthorization = config.EnableLegacyAuthorization,
        };
    }

    /// <summary>
    /// Maps a <see cref="ServerConfigurationDto"/> to a <see cref="ServerConfiguration"/>.
    /// </summary>
    /// <param name="dto">The <see cref="ServerConfigurationDto"/>.</param>
    /// <returns>The <see cref="ServerConfiguration"/>.</returns>
    public static ServerConfiguration MapToInternal(ServerConfigurationDto dto)
    {
        return new ServerConfiguration
        {
            LogFileRetentionDays = dto.LogFileRetentionDays,
            IsStartupWizardCompleted = dto.IsStartupWizardCompleted,
            CachePath = dto.CachePath,
            PreviousVersionStr = dto.PreviousVersionStr,
            EnableMetrics = dto.EnableMetrics,
            EnableNormalizedItemByNameIds = dto.EnableNormalizedItemByNameIds,
            IsPortAuthorized = dto.IsPortAuthorized,
            QuickConnectAvailable = dto.QuickConnectAvailable,
            EnableCaseSensitiveItemIds = dto.EnableCaseSensitiveItemIds,
            DisableLiveTvChannelUserDataName = dto.DisableLiveTvChannelUserDataName,
            MetadataPath = dto.MetadataPath,
            PreferredMetadataLanguage = dto.PreferredMetadataLanguage,
            MetadataCountryCode = dto.MetadataCountryCode,
            SortReplaceCharacters = dto.SortReplaceCharacters,
            SortRemoveCharacters = dto.SortRemoveCharacters,
            SortRemoveWords = dto.SortRemoveWords,
            MinResumePct = dto.MinResumePct,
            MaxResumePct = dto.MaxResumePct,
            MinResumeDurationSeconds = dto.MinResumeDurationSeconds,
            MinAudiobookResume = dto.MinAudiobookResume,
            MaxAudiobookResume = dto.MaxAudiobookResume,
            InactiveSessionThreshold = dto.InactiveSessionThreshold,
            LibraryMonitorDelay = dto.LibraryMonitorDelay,
            LibraryUpdateDuration = dto.LibraryUpdateDuration,
            CacheSize = dto.CacheSize,
            ImageSavingConvention = dto.ImageSavingConvention,
            MetadataOptions = dto.MetadataOptions,
            SkipDeserializationForBasicTypes = dto.SkipDeserializationForBasicTypes,
            ServerName = dto.ServerName,
            UICulture = dto.UICulture,
            SaveMetadataHidden = dto.SaveMetadataHidden,
            ContentTypes = dto.ContentTypes,
            RemoteClientBitrateLimit = dto.RemoteClientBitrateLimit,
            EnableFolderView = dto.EnableFolderView,
            EnableGroupingMoviesIntoCollections = dto.EnableGroupingMoviesIntoCollections,
            EnableGroupingShowsIntoCollections = dto.EnableGroupingShowsIntoCollections,
            DisplaySpecialsWithinSeasons = dto.DisplaySpecialsWithinSeasons,
            CodecsUsed = dto.CodecsUsed,
            PluginRepositories = dto.PluginRepositories,
            EnableExternalContentInSuggestions = dto.EnableExternalContentInSuggestions,
            ImageExtractionTimeoutMs = dto.ImageExtractionTimeoutMs,
            PathSubstitutions = dto.PathSubstitutions,
            EnableSlowResponseWarning = dto.EnableSlowResponseWarning,
            SlowResponseThresholdMs = dto.SlowResponseThresholdMs,
            CorsHosts = dto.CorsHosts,
            ActivityLogRetentionDays = dto.ActivityLogRetentionDays,
            LibraryScanFanoutConcurrency = dto.LibraryScanFanoutConcurrency,
            LibraryMetadataRefreshConcurrency = dto.LibraryMetadataRefreshConcurrency,
            AllowClientLogUpload = dto.AllowClientLogUpload,
            DummyChapterDuration = dto.DummyChapterDuration,
            ChapterImageResolution = dto.ChapterImageResolution,
            ParallelImageEncodingLimit = dto.ParallelImageEncodingLimit,
            CastReceiverApplications = dto.CastReceiverApplications,
            TrickplayOptions = dto.TrickplayOptions,
            EnableLegacyAuthorization = dto.EnableLegacyAuthorization,
        };
    }

    /// <summary>
    /// Maps an <see cref="EncodingOptions"/> to an <see cref="EncodingOptionsDto"/>.
    /// </summary>
    /// <param name="options">The <see cref="EncodingOptions"/>.</param>
    /// <returns>The <see cref="EncodingOptionsDto"/>.</returns>
    public static EncodingOptionsDto MapToDto(EncodingOptions options)
    {
        return new EncodingOptionsDto
        {
            EncodingThreadCount = options.EncodingThreadCount,
            TranscodingTempPath = options.TranscodingTempPath,
            FallbackFontPath = options.FallbackFontPath,
            EnableFallbackFont = options.EnableFallbackFont,
            EnableAudioVbr = options.EnableAudioVbr,
            DownMixAudioBoost = options.DownMixAudioBoost,
            DownMixStereoAlgorithm = options.DownMixStereoAlgorithm,
            MaxMuxingQueueSize = options.MaxMuxingQueueSize,
            EnableThrottling = options.EnableThrottling,
            ThrottleDelaySeconds = options.ThrottleDelaySeconds,
            EnableSegmentDeletion = options.EnableSegmentDeletion,
            SegmentKeepSeconds = options.SegmentKeepSeconds,
            HardwareAccelerationType = options.HardwareAccelerationType,
            EncoderAppPath = options.EncoderAppPath,
            EncoderAppPathDisplay = options.EncoderAppPathDisplay,
            VaapiDevice = options.VaapiDevice,
            QsvDevice = options.QsvDevice,
            EnableTonemapping = options.EnableTonemapping,
            EnableVppTonemapping = options.EnableVppTonemapping,
            EnableVideoToolboxTonemapping = options.EnableVideoToolboxTonemapping,
            TonemappingAlgorithm = options.TonemappingAlgorithm,
            TonemappingMode = options.TonemappingMode,
            TonemappingRange = options.TonemappingRange,
            TonemappingDesat = options.TonemappingDesat,
            TonemappingPeak = options.TonemappingPeak,
            TonemappingParam = options.TonemappingParam,
            VppTonemappingBrightness = options.VppTonemappingBrightness,
            VppTonemappingContrast = options.VppTonemappingContrast,
            H264Crf = options.H264Crf,
            H265Crf = options.H265Crf,
            EncoderPreset = options.EncoderPreset,
            DeinterlaceDoubleRate = options.DeinterlaceDoubleRate,
            DeinterlaceMethod = options.DeinterlaceMethod,
            EnableDecodingColorDepth10Hevc = options.EnableDecodingColorDepth10Hevc,
            EnableDecodingColorDepth10Vp9 = options.EnableDecodingColorDepth10Vp9,
            EnableDecodingColorDepth10HevcRext = options.EnableDecodingColorDepth10HevcRext,
            EnableDecodingColorDepth12HevcRext = options.EnableDecodingColorDepth12HevcRext,
            EnableEnhancedNvdecDecoder = options.EnableEnhancedNvdecDecoder,
            PreferSystemNativeHwDecoder = options.PreferSystemNativeHwDecoder,
            EnableIntelLowPowerH264HwEncoder = options.EnableIntelLowPowerH264HwEncoder,
            EnableIntelLowPowerHevcHwEncoder = options.EnableIntelLowPowerHevcHwEncoder,
            EnableHardwareEncoding = options.EnableHardwareEncoding,
            AllowHevcEncoding = options.AllowHevcEncoding,
            AllowAv1Encoding = options.AllowAv1Encoding,
            EnableSubtitleExtraction = options.EnableSubtitleExtraction,
            SubtitleExtractionTimeoutMinutes = options.SubtitleExtractionTimeoutMinutes,
            HardwareDecodingCodecs = options.HardwareDecodingCodecs,
            AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = options.AllowOnDemandMetadataBasedKeyframeExtractionForExtensions,
            HlsAudioSeekStrategy = options.HlsAudioSeekStrategy,
        };
    }

    /// <summary>
    /// Maps an <see cref="EncodingOptionsDto"/> to an <see cref="EncodingOptions"/>.
    /// </summary>
    /// <param name="dto">The <see cref="EncodingOptionsDto"/>.</param>
    /// <returns>The <see cref="EncodingOptions"/>.</returns>
    public static EncodingOptions MapToInternal(EncodingOptionsDto dto)
    {
        return new EncodingOptions
        {
            EncodingThreadCount = dto.EncodingThreadCount,
            TranscodingTempPath = dto.TranscodingTempPath,
            FallbackFontPath = dto.FallbackFontPath,
            EnableFallbackFont = dto.EnableFallbackFont,
            EnableAudioVbr = dto.EnableAudioVbr,
            DownMixAudioBoost = dto.DownMixAudioBoost,
            DownMixStereoAlgorithm = dto.DownMixStereoAlgorithm,
            MaxMuxingQueueSize = dto.MaxMuxingQueueSize,
            EnableThrottling = dto.EnableThrottling,
            ThrottleDelaySeconds = dto.ThrottleDelaySeconds,
            EnableSegmentDeletion = dto.EnableSegmentDeletion,
            SegmentKeepSeconds = dto.SegmentKeepSeconds,
            HardwareAccelerationType = dto.HardwareAccelerationType,
            EncoderAppPath = dto.EncoderAppPath,
            EncoderAppPathDisplay = dto.EncoderAppPathDisplay,
            VaapiDevice = dto.VaapiDevice,
            QsvDevice = dto.QsvDevice,
            EnableTonemapping = dto.EnableTonemapping,
            EnableVppTonemapping = dto.EnableVppTonemapping,
            EnableVideoToolboxTonemapping = dto.EnableVideoToolboxTonemapping,
            TonemappingAlgorithm = dto.TonemappingAlgorithm,
            TonemappingMode = dto.TonemappingMode,
            TonemappingRange = dto.TonemappingRange,
            TonemappingDesat = dto.TonemappingDesat,
            TonemappingPeak = dto.TonemappingPeak,
            TonemappingParam = dto.TonemappingParam,
            VppTonemappingBrightness = dto.VppTonemappingBrightness,
            VppTonemappingContrast = dto.VppTonemappingContrast,
            H264Crf = dto.H264Crf,
            H265Crf = dto.H265Crf,
            EncoderPreset = dto.EncoderPreset,
            DeinterlaceDoubleRate = dto.DeinterlaceDoubleRate,
            DeinterlaceMethod = dto.DeinterlaceMethod,
            EnableDecodingColorDepth10Hevc = dto.EnableDecodingColorDepth10Hevc,
            EnableDecodingColorDepth10Vp9 = dto.EnableDecodingColorDepth10Vp9,
            EnableDecodingColorDepth10HevcRext = dto.EnableDecodingColorDepth10HevcRext,
            EnableDecodingColorDepth12HevcRext = dto.EnableDecodingColorDepth12HevcRext,
            EnableEnhancedNvdecDecoder = dto.EnableEnhancedNvdecDecoder,
            PreferSystemNativeHwDecoder = dto.PreferSystemNativeHwDecoder,
            EnableIntelLowPowerH264HwEncoder = dto.EnableIntelLowPowerH264HwEncoder,
            EnableIntelLowPowerHevcHwEncoder = dto.EnableIntelLowPowerHevcHwEncoder,
            EnableHardwareEncoding = dto.EnableHardwareEncoding,
            AllowHevcEncoding = dto.AllowHevcEncoding,
            AllowAv1Encoding = dto.AllowAv1Encoding,
            EnableSubtitleExtraction = dto.EnableSubtitleExtraction,
            SubtitleExtractionTimeoutMinutes = dto.SubtitleExtractionTimeoutMinutes,
            HardwareDecodingCodecs = dto.HardwareDecodingCodecs,
            AllowOnDemandMetadataBasedKeyframeExtractionForExtensions = dto.AllowOnDemandMetadataBasedKeyframeExtractionForExtensions,
            HlsAudioSeekStrategy = dto.HlsAudioSeekStrategy,
        };
    }

#pragma warning disable CS0618 // Type or member is obsolete
    /// <summary>
    /// Maps a <see cref="NetworkConfiguration"/> to a <see cref="NetworkConfigurationDto"/>.
    /// </summary>
    /// <param name="config">The <see cref="NetworkConfiguration"/>.</param>
    /// <returns>The <see cref="NetworkConfigurationDto"/>.</returns>
    public static NetworkConfigurationDto MapToDto(NetworkConfiguration config)
    {
        return new NetworkConfigurationDto
        {
            BaseUrl = config.BaseUrl,
            EnableHttps = config.EnableHttps,
            RequireHttps = config.RequireHttps,
            CertificatePath = config.CertificatePath,
            CertificatePassword = config.CertificatePassword,
            InternalHttpPort = config.InternalHttpPort,
            InternalHttpsPort = config.InternalHttpsPort,
            PublicHttpPort = config.PublicHttpPort,
            PublicHttpsPort = config.PublicHttpsPort,
            AutoDiscovery = config.AutoDiscovery,
            EnableUPnP = config.EnableUPnP,
            EnableIPv4 = config.EnableIPv4,
            EnableIPv6 = config.EnableIPv6,
            EnableRemoteAccess = config.EnableRemoteAccess,
            LocalNetworkSubnets = config.LocalNetworkSubnets,
            LocalNetworkAddresses = config.LocalNetworkAddresses,
            KnownProxies = config.KnownProxies,
            IgnoreVirtualInterfaces = config.IgnoreVirtualInterfaces,
            VirtualInterfaceNames = config.VirtualInterfaceNames,
            EnablePublishedServerUriByRequest = config.EnablePublishedServerUriByRequest,
            PublishedServerUriBySubnet = config.PublishedServerUriBySubnet,
            RemoteIPFilter = config.RemoteIPFilter,
            IsRemoteIPFilterBlacklist = config.IsRemoteIPFilterBlacklist,
        };
    }

    /// <summary>
    /// Maps a <see cref="NetworkConfigurationDto"/> to a <see cref="NetworkConfiguration"/>.
    /// </summary>
    /// <param name="dto">The <see cref="NetworkConfigurationDto"/>.</param>
    /// <returns>The <see cref="NetworkConfiguration"/>.</returns>
    public static NetworkConfiguration MapToInternal(NetworkConfigurationDto dto)
    {
        return new NetworkConfiguration
        {
            BaseUrl = dto.BaseUrl,
            EnableHttps = dto.EnableHttps,
            RequireHttps = dto.RequireHttps,
            CertificatePath = dto.CertificatePath,
            CertificatePassword = dto.CertificatePassword,
            InternalHttpPort = dto.InternalHttpPort,
            InternalHttpsPort = dto.InternalHttpsPort,
            PublicHttpPort = dto.PublicHttpPort,
            PublicHttpsPort = dto.PublicHttpsPort,
            AutoDiscovery = dto.AutoDiscovery,
            EnableUPnP = dto.EnableUPnP,
            EnableIPv4 = dto.EnableIPv4,
            EnableIPv6 = dto.EnableIPv6,
            EnableRemoteAccess = dto.EnableRemoteAccess,
            LocalNetworkSubnets = dto.LocalNetworkSubnets,
            LocalNetworkAddresses = dto.LocalNetworkAddresses,
            KnownProxies = dto.KnownProxies,
            IgnoreVirtualInterfaces = dto.IgnoreVirtualInterfaces,
            VirtualInterfaceNames = dto.VirtualInterfaceNames,
            EnablePublishedServerUriByRequest = dto.EnablePublishedServerUriByRequest,
            PublishedServerUriBySubnet = dto.PublishedServerUriBySubnet,
            RemoteIPFilter = dto.RemoteIPFilter,
            IsRemoteIPFilterBlacklist = dto.IsRemoteIPFilterBlacklist,
        };
    }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// Maps a <see cref="MetadataConfiguration"/> to a <see cref="MetadataConfigurationDto"/>.
    /// </summary>
    /// <param name="config">The <see cref="MetadataConfiguration"/>.</param>
    /// <returns>The <see cref="MetadataConfigurationDto"/>.</returns>
    public static MetadataConfigurationDto MapToDto(MetadataConfiguration config)
    {
        return new MetadataConfigurationDto
        {
            UseFileCreationTimeForDateAdded = config.UseFileCreationTimeForDateAdded,
        };
    }

    /// <summary>
    /// Maps a <see cref="MetadataConfigurationDto"/> to a <see cref="MetadataConfiguration"/>.
    /// </summary>
    /// <param name="dto">The <see cref="MetadataConfigurationDto"/>.</param>
    /// <returns>The <see cref="MetadataConfiguration"/>.</returns>
    public static MetadataConfiguration MapToInternal(MetadataConfigurationDto dto)
    {
        return new MetadataConfiguration
        {
            UseFileCreationTimeForDateAdded = dto.UseFileCreationTimeForDateAdded,
        };
    }

    /// <summary>
    /// Maps an <see cref="XbmcMetadataOptions"/> to an <see cref="XbmcMetadataOptionsDto"/>.
    /// </summary>
    /// <param name="config">The <see cref="XbmcMetadataOptions"/>.</param>
    /// <returns>The <see cref="XbmcMetadataOptionsDto"/>.</returns>
    public static XbmcMetadataOptionsDto MapToDto(XbmcMetadataOptions config)
    {
        return new XbmcMetadataOptionsDto
        {
            UserId = config.UserId,
            ReleaseDateFormat = config.ReleaseDateFormat,
            SaveImagePathsInNfo = config.SaveImagePathsInNfo,
            EnablePathSubstitution = config.EnablePathSubstitution,
            EnableExtraThumbsDuplication = config.EnableExtraThumbsDuplication,
        };
    }

    /// <summary>
    /// Maps an <see cref="XbmcMetadataOptionsDto"/> to an <see cref="XbmcMetadataOptions"/>.
    /// </summary>
    /// <param name="dto">The <see cref="XbmcMetadataOptionsDto"/>.</param>
    /// <returns>The <see cref="XbmcMetadataOptions"/>.</returns>
    public static XbmcMetadataOptions MapToInternal(XbmcMetadataOptionsDto dto)
    {
        return new XbmcMetadataOptions
        {
            UserId = dto.UserId,
            ReleaseDateFormat = dto.ReleaseDateFormat,
            SaveImagePathsInNfo = dto.SaveImagePathsInNfo,
            EnablePathSubstitution = dto.EnablePathSubstitution,
            EnableExtraThumbsDuplication = dto.EnableExtraThumbsDuplication,
        };
    }

    /// <summary>
    /// Maps a <see cref="LiveTvOptions"/> to a <see cref="LiveTvOptionsDto"/>.
    /// </summary>
    /// <param name="config">The <see cref="LiveTvOptions"/>.</param>
    /// <returns>The <see cref="LiveTvOptionsDto"/>.</returns>
    public static LiveTvOptionsDto MapToDto(LiveTvOptions config)
    {
        return new LiveTvOptionsDto
        {
            GuideDays = config.GuideDays,
            RecordingPath = config.RecordingPath,
            MovieRecordingPath = config.MovieRecordingPath,
            SeriesRecordingPath = config.SeriesRecordingPath,
            EnableRecordingSubfolders = config.EnableRecordingSubfolders,
            EnableOriginalAudioWithEncodedRecordings = config.EnableOriginalAudioWithEncodedRecordings,
            TunerHosts = config.TunerHosts,
            ListingProviders = config.ListingProviders,
            PrePaddingSeconds = config.PrePaddingSeconds,
            PostPaddingSeconds = config.PostPaddingSeconds,
            MediaLocationsCreated = config.MediaLocationsCreated,
            RecordingPostProcessor = config.RecordingPostProcessor,
            RecordingPostProcessorArguments = config.RecordingPostProcessorArguments,
            SaveRecordingNFO = config.SaveRecordingNFO,
            SaveRecordingImages = config.SaveRecordingImages,
        };
    }

    /// <summary>
    /// Maps a <see cref="LiveTvOptionsDto"/> to a <see cref="LiveTvOptions"/>.
    /// </summary>
    /// <param name="dto">The <see cref="LiveTvOptionsDto"/>.</param>
    /// <returns>The <see cref="LiveTvOptions"/>.</returns>
    public static LiveTvOptions MapToInternal(LiveTvOptionsDto dto)
    {
        return new LiveTvOptions
        {
            GuideDays = dto.GuideDays,
            RecordingPath = dto.RecordingPath,
            MovieRecordingPath = dto.MovieRecordingPath,
            SeriesRecordingPath = dto.SeriesRecordingPath,
            EnableRecordingSubfolders = dto.EnableRecordingSubfolders,
            EnableOriginalAudioWithEncodedRecordings = dto.EnableOriginalAudioWithEncodedRecordings,
            TunerHosts = dto.TunerHosts,
            ListingProviders = dto.ListingProviders,
            PrePaddingSeconds = dto.PrePaddingSeconds,
            PostPaddingSeconds = dto.PostPaddingSeconds,
            MediaLocationsCreated = dto.MediaLocationsCreated,
            RecordingPostProcessor = dto.RecordingPostProcessor,
            RecordingPostProcessorArguments = dto.RecordingPostProcessorArguments,
            SaveRecordingNFO = dto.SaveRecordingNFO,
            SaveRecordingImages = dto.SaveRecordingImages,
        };
    }
}
