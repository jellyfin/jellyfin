using System.ComponentModel.DataAnnotations;
using System.Linq;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Configuration Controller.
/// </summary>
[Route("System")]
[Authorize]
[Tags("System")]
public class ConfigurationController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public ConfigurationController(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets application configuration.
    /// </summary>
    /// <response code="200">Application configuration returned.</response>
    /// <returns>Application configuration.</returns>
    [HttpGet("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ServerConfigurationDto> GetConfiguration()
    {
        return MapToDto(_configurationManager.Configuration);
    }

    /// <summary>
    /// Updates application configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <response code="204">Configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateConfiguration([FromBody, Required] ServerConfigurationDto configuration)
    {
        _configurationManager.ReplaceConfiguration(MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Gets a default MetadataOptions object.
    /// </summary>
    /// <response code="200">Metadata options returned.</response>
    /// <returns>Default MetadataOptions.</returns>
    [HttpGet("Configuration/MetadataOptions/Default")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<MetadataOptions> GetDefaultMetadataOptions()
    {
        return new MetadataOptions();
    }

    /// <summary>
    /// Gets encoding configuration.
    /// </summary>
    /// <response code="200">Encoding configuration returned.</response>
    /// <returns>Encoding configuration.</returns>
    [HttpGet("Configuration/Encoding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<EncodingOptionsDto> GetEncodingConfiguration()
    {
        return MapToDto(_configurationManager.GetConfiguration<EncodingOptions>("encoding"));
    }

    /// <summary>
    /// Updates encoding configuration.
    /// </summary>
    /// <param name="configuration">Encoding configuration.</param>
    /// <response code="204">Encoding configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Encoding")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateEncodingConfiguration([FromBody, Required] EncodingOptionsDto configuration)
    {
        _configurationManager.SaveConfiguration("encoding", MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Gets network configuration.
    /// </summary>
    /// <response code="200">Network configuration returned.</response>
    /// <returns>Network configuration.</returns>
    [HttpGet("Configuration/Network")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<NetworkConfigurationDto> GetNetworkConfiguration()
    {
        return MapToDto(_configurationManager.GetConfiguration<NetworkConfiguration>("network"));
    }

    /// <summary>
    /// Updates network configuration.
    /// </summary>
    /// <param name="configuration">Network configuration.</param>
    /// <response code="204">Network configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Network")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateNetworkConfiguration([FromBody, Required] NetworkConfigurationDto configuration)
    {
        _configurationManager.SaveConfiguration("network", MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Gets metadata configuration.
    /// </summary>
    /// <response code="200">Metadata configuration returned.</response>
    /// <returns>Metadata configuration.</returns>
    [HttpGet("Configuration/Metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<MetadataConfigurationDto> GetMetadataConfiguration()
    {
        return MapToDto(_configurationManager.GetConfiguration<MetadataConfiguration>("metadata"));
    }

    /// <summary>
    /// Updates metadata configuration.
    /// </summary>
    /// <param name="configuration">Metadata configuration.</param>
    /// <response code="204">Metadata configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Metadata")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateMetadataConfiguration([FromBody, Required] MetadataConfigurationDto configuration)
    {
        _configurationManager.SaveConfiguration("metadata", MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Gets XbmcMetadata configuration.
    /// </summary>
    /// <response code="200">XbmcMetadata configuration returned.</response>
    /// <returns>XbmcMetadata configuration.</returns>
    [HttpGet("Configuration/XbmcMetadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<XbmcMetadataOptionsDto> GetXbmcMetadataConfiguration()
    {
        return MapToDto(_configurationManager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata"));
    }

    /// <summary>
    /// Updates XbmcMetadata configuration.
    /// </summary>
    /// <param name="configuration">XbmcMetadata configuration.</param>
    /// <response code="204">XbmcMetadata configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/XbmcMetadata")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateXbmcMetadataConfiguration([FromBody, Required] XbmcMetadataOptionsDto configuration)
    {
        _configurationManager.SaveConfiguration("xbmcmetadata", MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Gets LiveTv configuration.
    /// </summary>
    /// <response code="200">LiveTv configuration returned.</response>
    /// <returns>LiveTv configuration.</returns>
    [HttpGet("Configuration/LiveTv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<LiveTvOptionsDto> GetLiveTvConfiguration()
    {
        return MapToDto(_configurationManager.GetConfiguration<LiveTvOptions>("livetv"));
    }

    /// <summary>
    /// Updates LiveTv configuration.
    /// </summary>
    /// <param name="configuration">LiveTv configuration.</param>
    /// <response code="204">LiveTv configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/LiveTv")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateLiveTvConfiguration([FromBody, Required] LiveTvOptionsDto configuration)
    {
        _configurationManager.SaveConfiguration("livetv", MapToInternal(configuration));
        return NoContent();
    }

    /// <summary>
    /// Updates branding configuration.
    /// </summary>
    /// <param name="configuration">Branding configuration.</param>
    /// <response code="204">Branding configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Branding")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateBrandingConfiguration([FromBody, Required] BrandingOptionsDto configuration)
    {
        // Get the current branding configuration to preserve SplashscreenLocation
        var currentBranding = _configurationManager.GetConfiguration<BrandingOptions>("branding");

        // Update only the properties from BrandingOptionsDto
        currentBranding.LoginDisclaimer = configuration.LoginDisclaimer;
        currentBranding.CustomCss = configuration.CustomCss;
        currentBranding.SplashscreenEnabled = configuration.SplashscreenEnabled;

        _configurationManager.SaveConfiguration("branding", currentBranding);

        return NoContent();
    }

    // Mapping Helpers

    private static ServerConfigurationDto MapToDto(ServerConfiguration config)
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

    private static ServerConfiguration MapToInternal(ServerConfigurationDto dto)
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

    private static EncodingOptionsDto MapToDto(EncodingOptions options)
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

    private static EncodingOptions MapToInternal(EncodingOptionsDto dto)
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
    private static NetworkConfigurationDto MapToDto(NetworkConfiguration config)
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

    private static NetworkConfiguration MapToInternal(NetworkConfigurationDto dto)
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

    private static MetadataConfigurationDto MapToDto(MetadataConfiguration config)
    {
        return new MetadataConfigurationDto
        {
            UseFileCreationTimeForDateAdded = config.UseFileCreationTimeForDateAdded,
        };
    }

    private static MetadataConfiguration MapToInternal(MetadataConfigurationDto dto)
    {
        return new MetadataConfiguration
        {
            UseFileCreationTimeForDateAdded = dto.UseFileCreationTimeForDateAdded,
        };
    }

    private static XbmcMetadataOptionsDto MapToDto(XbmcMetadataOptions config)
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

    private static XbmcMetadataOptions MapToInternal(XbmcMetadataOptionsDto dto)
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

    private static LiveTvOptionsDto MapToDto(LiveTvOptions config)
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

    private static LiveTvOptions MapToInternal(LiveTvOptionsDto dto)
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
