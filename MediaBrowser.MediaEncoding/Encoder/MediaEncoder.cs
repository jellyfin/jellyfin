#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AsyncKeyedLock;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    /// <summary>
    /// Class MediaEncoder.
    /// </summary>
    public partial class MediaEncoder : IMediaEncoder, IDisposable
    {
        /// <summary>
        /// The default SDR image extraction timeout in milliseconds.
        /// </summary>
        internal const int DefaultSdrImageExtractionTimeout = 10000;

        /// <summary>
        /// The default HDR image extraction timeout in milliseconds.
        /// </summary>
        internal const int DefaultHdrImageExtractionTimeout = 20000;

        /// <summary>
        /// Frame rate assumed when the source does not report one, used to rebuild trickplay timestamps.
        /// </summary>
        private const float FallbackFrameRate = 30;

        private readonly ILogger<MediaEncoder> _logger;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly IConfiguration _config;
        private readonly IServerConfigurationManager _serverConfig;
        private readonly string _startupOptionFFmpegPath;

        private readonly AsyncNonKeyedLocker _thumbnailResourcePool;

        // MediaEncoder is registered as a Singleton
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private List<string> _encoders = new List<string>();
        private List<string> _decoders = new List<string>();
        private List<string> _hwaccels = new List<string>();
        private List<string> _filters = new List<string>();
        private IDictionary<FilterOptionType, bool> _filtersWithOption = new Dictionary<FilterOptionType, bool>();
        private IDictionary<BitStreamFilterOptionType, bool> _bitStreamFiltersWithOption = new Dictionary<BitStreamFilterOptionType, bool>();

        private bool _isPkeyPauseSupported = false;
        private bool _isLowPriorityHwDecodeSupported = false;
        private bool _proberSupportsFirstVideoFrame = false;

        private bool _isVaapiDeviceAmd = false;
        private bool _isVaapiDeviceInteliHD = false;
        private bool _isVaapiDeviceInteli965 = false;
        private bool _isVaapiDeviceSupportVulkanDrmModifier = false;
        private bool _isVaapiDeviceSupportVulkanDrmInterop = false;

        private bool _isVideoToolboxAv1DecodeAvailable = false;

        private static string[] _vulkanImageDrmFmtModifierExts =
        {
            "VK_EXT_image_drm_format_modifier",
        };

        private static string[] _vulkanExternalMemoryDmaBufExts =
        {
            "VK_KHR_external_memory_fd",
            "VK_EXT_external_memory_dma_buf",
            "VK_KHR_external_semaphore_fd",
            "VK_EXT_external_memory_host"
        };

        private readonly IFFPaths _ffPaths;
        private readonly IFFRunner _ffRunner;

        private Version _ffmpegVersion = null;
        private int _threads;

        public MediaEncoder(
            ILogger<MediaEncoder> logger,
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IConfiguration config,
            IServerConfigurationManager serverConfig,
            IFFPaths ffPaths,
            IFFRunner ffRunner)
        {
            _ffPaths = ffPaths;
            _ffRunner = ffRunner;
            _logger = logger;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _blurayExaminer = blurayExaminer;
            _localization = localization;
            _config = config;
            _serverConfig = serverConfig;
            _startupOptionFFmpegPath = config.GetValue<string>(Controller.Extensions.ConfigurationExtensions.FfmpegPathKey) ?? string.Empty;

            _jsonSerializerOptions = new JsonSerializerOptions(JsonDefaults.Options);
            _jsonSerializerOptions.Converters.Add(new JsonBoolStringConverter());

            // Although the type is not nullable, this might still be null during unit tests
            var semaphoreCount = serverConfig.Configuration?.ParallelImageEncodingLimit ?? 0;
            if (semaphoreCount < 1)
            {
                semaphoreCount = Environment.ProcessorCount;
            }

            _thumbnailResourcePool = new(semaphoreCount);
        }

        /// <inheritdoc />
        public string EncoderPath => _ffPaths.EncoderPath;

        /// <inheritdoc />
        public string ProbePath => _ffPaths.ProbePath;

        /// <inheritdoc />
        public Version EncoderVersion => _ffmpegVersion;

        /// <inheritdoc />
        public bool IsPkeyPauseSupported => _isPkeyPauseSupported;

        /// <inheritdoc />
        public bool IsVaapiDeviceAmd => _isVaapiDeviceAmd;

        /// <inheritdoc />
        public bool IsVaapiDeviceInteliHD => _isVaapiDeviceInteliHD;

        /// <inheritdoc />
        public bool IsVaapiDeviceInteli965 => _isVaapiDeviceInteli965;

        /// <inheritdoc />
        public bool IsVaapiDeviceSupportVulkanDrmModifier => _isVaapiDeviceSupportVulkanDrmModifier;

        /// <inheritdoc />
        public bool IsVaapiDeviceSupportVulkanDrmInterop => _isVaapiDeviceSupportVulkanDrmInterop;

        public bool IsVideoToolboxAv1DecodeAvailable => _isVideoToolboxAv1DecodeAvailable;

        /// <summary>
        /// Run at startup to validate ffmpeg.
        /// Sets global variables FFmpegPath.
        /// Precedence is: CLI/Env var > Config > $PATH.
        /// </summary>
        /// <returns>bool indicates whether a valid ffmpeg is found.</returns>
        public async Task<bool> SetFFmpegPathAsync()
        {
            var skipValidation = _config.GetFFmpegSkipValidation();
            if (skipValidation)
            {
                _logger.LogWarning("FFmpeg: Skipping FFmpeg Validation due to FFmpeg:novalidation set to true");
                return true;
            }

            // 1) Check if the --ffmpeg CLI switch has been given
            var ffmpegPath = _startupOptionFFmpegPath;
            string ffmpegPathSetMethodText = "command line or environment variable";
            if (string.IsNullOrEmpty(ffmpegPath))
            {
                // 2) Custom path stored in config/encoding xml file under tag <EncoderAppPath> should be used as a fallback
                ffmpegPath = _configurationManager.GetEncodingOptions().EncoderAppPath;
                ffmpegPathSetMethodText = "encoding.xml config file";
                if (string.IsNullOrEmpty(ffmpegPath))
                {
                    // 3) Check "ffmpeg"
                    ffmpegPath = "ffmpeg";
                    ffmpegPathSetMethodText = "system $PATH";
                }
            }

            if (!await ValidatePathAsync(ffmpegPath).ConfigureAwait(false))
            {
                _ffPaths.SetEncoderPath(string.Empty);
                _logger.LogError("FFmpeg: Path set by {FfmpegPathSetMethodText} is invalid", ffmpegPathSetMethodText);
                return false;
            }

            // Write the FFmpeg path to the config/encoding.xml file as <EncoderAppPathDisplay> so it appears in UI
            var options = _configurationManager.GetEncodingOptions();
            options.EncoderAppPathDisplay = _ffPaths.EncoderPath;
            _configurationManager.SaveConfiguration("encoding", options);

            // Only if mpeg path is set, try and set path to probe
            if (!string.IsNullOrEmpty(_ffPaths.EncoderPath))
            {
                // Interrogate to understand what coders are supported
                var validator = new EncoderValidator(_logger, _ffPaths.EncoderPath, _ffRunner);

                SetAvailableDecoders(await validator.GetDecodersAsync().ConfigureAwait(false));
                SetAvailableEncoders(await validator.GetEncodersAsync().ConfigureAwait(false));
                SetAvailableFilters(await validator.GetFiltersAsync().ConfigureAwait(false));
                SetAvailableFiltersWithOption(await validator.GetFiltersWithOptionAsync().ConfigureAwait(false));
                SetAvailableBitStreamFiltersWithOption(await validator.GetBitStreamFiltersWithOptionAsync().ConfigureAwait(false));
                SetAvailableHwaccels(await validator.GetHwaccelsAsync().ConfigureAwait(false));
                await SetMediaEncoderVersionAsync(validator).ConfigureAwait(false);

                _threads = EncodingHelper.GetNumberOfThreads(null, options, null);

                _isPkeyPauseSupported = await validator.CheckSupportedRuntimeKeyAsync("p      pause transcoding", _ffmpegVersion).ConfigureAwait(false);
                _isLowPriorityHwDecodeSupported = await validator.CheckSupportedHwaccelFlagAsync("low_priority").ConfigureAwait(false);
                _proberSupportsFirstVideoFrame = await validator.CheckSupportedProberOptionAsync("only_first_vframe").ConfigureAwait(false);

                // Check the Vaapi device vendor
                if (OperatingSystem.IsLinux()
                    && SupportsHwaccel("vaapi")
                    && !string.IsNullOrEmpty(options.VaapiDevice)
                    && options.HardwareAccelerationType == HardwareAccelerationType.vaapi)
                {
                    _isVaapiDeviceAmd = await validator.CheckVaapiDeviceByDriverNameAsync("Mesa Gallium driver", options.VaapiDevice).ConfigureAwait(false);
                    _isVaapiDeviceInteliHD = await validator.CheckVaapiDeviceByDriverNameAsync("Intel iHD driver", options.VaapiDevice).ConfigureAwait(false);
                    _isVaapiDeviceInteli965 = await validator.CheckVaapiDeviceByDriverNameAsync("Intel i965 driver", options.VaapiDevice).ConfigureAwait(false);
                    _isVaapiDeviceSupportVulkanDrmModifier = await validator.CheckVulkanDrmDeviceByExtensionNameAsync(options.VaapiDevice, _vulkanImageDrmFmtModifierExts).ConfigureAwait(false);
                    _isVaapiDeviceSupportVulkanDrmInterop = await validator.CheckVulkanDrmDeviceByExtensionNameAsync(options.VaapiDevice, _vulkanExternalMemoryDmaBufExts).ConfigureAwait(false);

                    if (_isVaapiDeviceAmd)
                    {
                        _logger.LogInformation("VAAPI device {RenderNodePath} is AMD GPU", options.VaapiDevice);
                    }
                    else if (_isVaapiDeviceInteliHD)
                    {
                        _logger.LogInformation("VAAPI device {RenderNodePath} is Intel GPU (iHD)", options.VaapiDevice);
                    }
                    else if (_isVaapiDeviceInteli965)
                    {
                        _logger.LogInformation("VAAPI device {RenderNodePath} is Intel GPU (i965)", options.VaapiDevice);
                    }

                    if (_isVaapiDeviceSupportVulkanDrmModifier)
                    {
                        _logger.LogInformation("VAAPI device {RenderNodePath} supports Vulkan DRM modifier", options.VaapiDevice);
                    }

                    if (_isVaapiDeviceSupportVulkanDrmInterop)
                    {
                        _logger.LogInformation("VAAPI device {RenderNodePath} supports Vulkan DRM interop", options.VaapiDevice);
                    }
                }

                // Check if VideoToolbox supports AV1 decode
                if (OperatingSystem.IsMacOS() && SupportsHwaccel("videotoolbox"))
                {
                    _isVideoToolboxAv1DecodeAvailable = validator.CheckIsVideoToolboxAv1DecodeAvailable();
                }
            }

            _logger.LogInformation("FFmpeg: {FfmpegPath}", _ffPaths.EncoderPath);
            return !string.IsNullOrWhiteSpace(ffmpegPath);
        }

        /// <summary>
        /// Validates the supplied FQPN to ensure it is a ffmpeg utility.
        /// If checks pass, global variable FFmpegPath is updated.
        /// </summary>
        /// <param name="path">FQPN to test.</param>
        /// <returns><c>true</c> if the version validation succeeded; otherwise, <c>false</c>.</returns>
        private async Task<bool> ValidatePathAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            bool rc = await new EncoderValidator(_logger, path, _ffRunner).ValidateVersionAsync().ConfigureAwait(false);
            if (!rc)
            {
                _logger.LogError("FFmpeg: Failed version check: {Path}", path);
                return false;
            }

            _ffPaths.SetEncoderPath(path);
            return true;
        }

        private string GetEncoderPathFromDirectory(string path, string filename, bool recursive = false)
        {
            try
            {
                var files = _fileSystem.GetFilePaths(path, recursive);

                return files.FirstOrDefault(i => Path.GetFileNameWithoutExtension(i.AsSpan()).Equals(filename, StringComparison.OrdinalIgnoreCase)
                                                    && !Path.GetExtension(i.AsSpan()).Equals(".c", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                // Trap all exceptions, like DirNotExists, and return null
                return null;
            }
        }

        public void SetAvailableEncoders(IEnumerable<string> list)
        {
            _encoders = list.ToList();
        }

        public void SetAvailableDecoders(IEnumerable<string> list)
        {
            _decoders = list.ToList();
        }

        public void SetAvailableHwaccels(IEnumerable<string> list)
        {
            _hwaccels = list.ToList();
        }

        public void SetAvailableFilters(IEnumerable<string> list)
        {
            _filters = list.ToList();
        }

        public void SetAvailableFiltersWithOption(IDictionary<FilterOptionType, bool> dict)
        {
            _filtersWithOption = dict;
        }

        public void SetAvailableBitStreamFiltersWithOption(IDictionary<BitStreamFilterOptionType, bool> dict)
        {
            _bitStreamFiltersWithOption = dict;
        }

        public async Task SetMediaEncoderVersionAsync(EncoderValidator validator)
        {
            _ffmpegVersion = await validator.GetFFmpegVersionAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public bool SupportsEncoder(string encoder)
        {
            return _encoders.Contains(encoder, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool SupportsDecoder(string decoder)
        {
            return _decoders.Contains(decoder, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool SupportsHwaccel(string hwaccel)
        {
            return _hwaccels.Contains(hwaccel, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool SupportsFilter(string filter)
        {
            return _filters.Contains(filter, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool SupportsFilterWithOption(FilterOptionType option)
        {
            return _filtersWithOption.TryGetValue(option, out var val) && val;
        }

        public bool SupportsBitStreamFilterWithOption(BitStreamFilterOptionType option)
        {
            return _bitStreamFiltersWithOption.TryGetValue(option, out var val) && val;
        }

        public bool CanEncodeToAudioCodec(string codec)
        {
            if (string.Equals(codec, "opus", StringComparison.OrdinalIgnoreCase))
            {
                codec = "libopus";
            }
            else if (string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                codec = "libmp3lame";
            }

            return SupportsEncoder(codec);
        }

        public bool CanEncodeToSubtitleCodec(string codec)
        {
            // TODO
            return true;
        }

        /// <inheritdoc />
        public Task<MediaInfo> GetMediaInfo(MediaInfoRequest request, CancellationToken cancellationToken)
        {
            var extractChapters = request.ExtractChapters;
            var extraArgs = GetExtraArguments(request);

            return GetMediaInfoInternal(
                GetInputArgument(request.MediaSource.Path, request.MediaSource),
                request.MediaSource.Path,
                request.MediaSource.Protocol,
                extractChapters,
                extraArgs,
                request.MediaType == DlnaProfileType.Audio,
                request.MediaSource.VideoType,
                cancellationToken);
        }

        internal string GetExtraArguments(MediaInfoRequest request)
        {
            var ffmpegAnalyzeDuration = _config.GetFFmpegAnalyzeDuration() ?? string.Empty;
            var ffmpegProbeSize = _config.GetFFmpegProbeSize() ?? string.Empty;
            var analyzeDuration = string.Empty;
            var extraArgs = string.Empty;

            if (request.MediaSource.AnalyzeDurationMs > 0)
            {
                analyzeDuration = "-analyzeduration " + (request.MediaSource.AnalyzeDurationMs * 1000);
            }
            else if (!string.IsNullOrEmpty(ffmpegAnalyzeDuration))
            {
                analyzeDuration = "-analyzeduration " + ffmpegAnalyzeDuration;
            }

            if (!string.IsNullOrEmpty(analyzeDuration))
            {
                extraArgs = analyzeDuration;
            }

            if (!string.IsNullOrEmpty(ffmpegProbeSize))
            {
                extraArgs += " -probesize " + ffmpegProbeSize;
            }

            if (request.MediaSource.RequiredHttpHeaders.TryGetValue("User-Agent", out var userAgent))
            {
                extraArgs += $" -user_agent \"{userAgent}\"";
            }

            if (request.MediaSource.Protocol == MediaProtocol.Rtsp)
            {
                extraArgs += " -rtsp_transport tcp+udp -rtsp_flags prefer_tcp";
            }

            return extraArgs;
        }

        /// <inheritdoc />
        public string GetInputArgument(IReadOnlyList<string> inputFiles, MediaSourceInfo mediaSource)
        {
            return EncodingUtils.GetInputArgument("file", inputFiles, mediaSource.Protocol);
        }

        /// <inheritdoc />
        public string GetInputArgument(string inputFile, MediaSourceInfo mediaSource)
        {
            var prefix = "file";
            if (mediaSource.IsoType == IsoType.BluRay)
            {
                prefix = "bluray";
            }

            return EncodingUtils.GetInputArgument(prefix, new[] { inputFile }, mediaSource.Protocol);
        }

        /// <inheritdoc />
        public string GetExternalSubtitleInputArgument(string inputFile)
        {
            const string Prefix = "file";

            return EncodingUtils.GetInputArgument(Prefix, new[] { inputFile }, MediaProtocol.File);
        }

        /// <summary>
        /// Gets the media info internal.
        /// </summary>
        /// <returns>Task{MediaInfoResult}.</returns>
        private async Task<MediaInfo> GetMediaInfoInternal(
            string inputPath,
            string primaryPath,
            MediaProtocol protocol,
            bool extractChapters,
            string probeSizeArgument,
            bool isAudio,
            VideoType? videoType,
            CancellationToken cancellationToken)
        {
            InternalMediaInfoResult result = null;

            var request = new ProbeRequest
            {
                Input = inputPath,
                SourceTuning = probeSizeArgument,
                IncludeChapters = extractChapters,
                FirstVideoFrameOnly = protocol == MediaProtocol.File && !isAudio && _proberSupportsFirstVideoFrame,
                Threads = _threads,
                Stdout = async (stdout, ct) =>
                {
                    result = await JsonSerializer.DeserializeAsync<InternalMediaInfoResult>(
                        stdout,
                        _jsonSerializerOptions,
                        ct).ConfigureAwait(false);
                }
            };

            var runResult = await _ffRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);

            if (!runResult.Succeeded)
            {
                throw new FfmpegException($"ffprobe failed for {primaryPath}: {runResult.Stderr}");
            }

            if (result is null || (result.Streams is null && result.Format is null))
            {
                throw new FfmpegException("ffprobe failed - streams and format are both null.");
            }

            if (result.Streams is not null)
            {
                // Normalize aspect ratio if invalid
                foreach (var stream in result.Streams)
                {
                    if (string.Equals(stream.DisplayAspectRatio, "0:1", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.DisplayAspectRatio = string.Empty;
                    }

                    if (string.Equals(stream.SampleAspectRatio, "0:1", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.SampleAspectRatio = string.Empty;
                    }
                }
            }

            return new ProbeResultNormalizer(_logger, _localization).GetMediaInfo(result, videoType, isAudio, primaryPath, protocol);
        }

        /// <inheritdoc />
        public Task<string> ExtractAudioImage(string path, int? imageStreamIndex, CancellationToken cancellationToken)
        {
            var mediaSource = new MediaSourceInfo
            {
                Protocol = MediaProtocol.File
            };

            return ExtractImage(path, null, null, imageStreamIndex, mediaSource, true, null, null, ImageFormat.Jpg, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ExtractVideoImage(string inputFile, string container, MediaSourceInfo mediaSource, MediaStream videoStream, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFile, container, videoStream, null, mediaSource, false, threedFormat, offset, ImageFormat.Jpg, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ExtractVideoImage(string inputFile, string container, MediaSourceInfo mediaSource, MediaStream imageStream, int? imageStreamIndex, ImageFormat? targetFormat, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFile, container, imageStream, imageStreamIndex, mediaSource, false, null, null, targetFormat, cancellationToken);
        }

        private async Task<string> ExtractImage(
            string inputFile,
            string container,
            MediaStream videoStream,
            int? imageStreamIndex,
            MediaSourceInfo mediaSource,
            bool isAudio,
            Video3DFormat? threedFormat,
            TimeSpan? offset,
            ImageFormat? targetFormat,
            CancellationToken cancellationToken)
        {
            var inputArgument = GetInputPathArgument(inputFile, mediaSource);

            if (!isAudio)
            {
                try
                {
                    return await ExtractImageInternal(inputArgument, container, videoStream, imageStreamIndex, threedFormat, offset, true, targetFormat, false, cancellationToken).ConfigureAwait(false);
                }
                catch (ArgumentException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "I-frame image extraction failed, will attempt standard way. Input: {Arguments}", inputArgument);
                }
            }

            return await ExtractImageInternal(inputArgument, container, videoStream, imageStreamIndex, threedFormat, offset, false, targetFormat, isAudio, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> ExtractImageInternal(
            string inputPath,
            string container,
            MediaStream videoStream,
            int? imageStreamIndex,
            Video3DFormat? threedFormat,
            TimeSpan? offset,
            bool useIFrame,
            ImageFormat? targetFormat,
            bool isAudio,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(inputPath);

            var useTradeoff = _config.GetFFmpegImgExtractPerfTradeoff();

            var outputExtension = targetFormat?.GetExtension() ?? ".jpg";

            var tempExtractPath = Path.Combine(_configurationManager.ApplicationPaths.TempDirectory, Guid.NewGuid() + outputExtension);
            Directory.CreateDirectory(Path.GetDirectoryName(tempExtractPath));

            // deint -> scale -> thumbnail -> tonemap.
            // put the SW tonemap right after the thumbnail to do it only once to reduce cpu usage.
            var filters = new List<string>();

            // deinterlace using bwdif algorithm for video stream.
            if (videoStream is not null && videoStream.IsInterlaced)
            {
                filters.Add("bwdif=0:-1:0");
            }

            // apply some filters to thumbnail extracted below (below) crop any black lines that we made and get the correct ar.
            // This filter chain may have adverse effects on recorded tv thumbnails if ar changes during presentation ex. commercials @ diff ar
            var scaler = threedFormat switch
            {
                // hsbs crop width in half,scale to correct size, set the display aspect,crop out any black bars we may have made. Work out the correct height based on the display aspect it will maintain the aspect where -1 in this case (3d) may not.
                Video3DFormat.HalfSideBySide => @"crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1",
                // fsbs crop width in half,set the display aspect,crop out any black bars we may have made
                Video3DFormat.FullSideBySide => @"crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1",
                // htab crop height in half,scale to correct size, set the display aspect,crop out any black bars we may have made
                Video3DFormat.HalfTopAndBottom => @"crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1",
                // ftab crop height in half, set the display aspect,crop out any black bars we may have made
                Video3DFormat.FullTopAndBottom => @"crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\,ih*dar):min(ih\,iw/dar):(iw-min(iw\,iw*sar))/2:(ih - min (ih\,ih/sar))/2,setsar=sar=1",
                _ => "scale=round(iw*sar/2)*2:round(ih/2)*2"
            };

            filters.Add(scaler);

            // Use ffmpeg to sample N frames and pick the best thumbnail. Have a fall back just in case.
            var enableThumbnail = !useTradeoff && useIFrame && !string.Equals("wtv", container, StringComparison.OrdinalIgnoreCase);
            if (enableThumbnail)
            {
                filters.Add("thumbnail=n=24");
            }

            // Use SW tonemap on HDR video stream only when the zscale or tonemapx filter is available.
            // Only enable Dolby Vision tonemap when tonemapx is available
            var enableHdrExtraction = false;

            if (videoStream?.VideoRange == VideoRange.HDR)
            {
                if (SupportsFilter("tonemapx"))
                {
                    var peak = videoStream.VideoRangeType == VideoRangeType.DOVI ? "400" : "100";
                    enableHdrExtraction = true;
                    filters.Add($"tonemapx=tonemap=bt2390:desat=0:peak={peak}:t=bt709:m=bt709:p=bt709:format=yuv420p:range=full");
                }
                else if (SupportsFilter("zscale") && videoStream.VideoRangeType != VideoRangeType.DOVI)
                {
                    enableHdrExtraction = true;
                    filters.Add("zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0:peak=100,zscale=t=bt709:m=bt709:out_range=full,format=yuv420p");
                }
            }

            var timeoutMs = _configurationManager.Configuration.ImageExtractionTimeoutMs;
            if (timeoutMs <= 0)
            {
                timeoutMs = enableHdrExtraction ? DefaultHdrImageExtractionTimeout : DefaultSdrImageExtractionTimeout;
            }

            var inputFormat = string.IsNullOrWhiteSpace(container)
                ? string.Empty
                : EncodingHelper.GetInputFormat(container) ?? string.Empty;

            // The mpegts demuxer cannot seek to keyframes, so we have to let the decoder discard
            // non-keyframes, which may contain corrupted images.
            var seekMpegTs = offset.HasValue && string.Equals("mpegts", container, StringComparison.OrdinalIgnoreCase);

            var request = new ImageRequest
            {
                Input = inputPath,
                OutputPath = tempExtractPath,
                Filters = string.Join(',', filters),
                EncoderVersion = EncoderVersion,
                InputFormat = inputFormat,
                SeekTo = offset ?? TimeSpan.Zero,
                StreamIndex = imageStreamIndex ?? ImageRequest.AutoStreamIndex,

                // Cover art is already at its native size; only chapter stills get rescaled.
                Resolution = isAudio ? ImageResolution.MatchSource : _serverConfig.Configuration.ChapterImageResolution,
                KeyFrameOnly = useIFrame && (useTradeoff || seekMpegTs),
                Threads = _threads,
                Timeout = TimeSpan.FromMilliseconds(timeoutMs)
            };

            FFResult result;
            using (await _thumbnailResourcePool.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                result = await _ffRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
            }

            if (result.StopReason == FFStopReason.TimedOut)
            {
                throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg image extraction timed out for {0} after {1}ms", inputPath, timeoutMs));
            }

            var file = _fileSystem.GetFileInfo(tempExtractPath);
            if (!result.Succeeded || !file.Exists || file.Length == 0)
            {
                throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg image extraction failed for {0}", inputPath));
            }

            return tempExtractPath;
        }

        /// <inheritdoc />
        public async Task<string> ExtractVideoImagesOnIntervalAccelerated(
            string inputFile,
            string container,
            MediaSourceInfo mediaSource,
            MediaStream imageStream,
            int maxWidth,
            TimeSpan interval,
            bool allowHwAccel,
            bool enableHwEncoding,
            int? threads,
            int? qualityScale,
            ProcessPriorityClass? priority,
            bool enableKeyFrameOnlyExtraction,
            EncodingHelper encodingHelper,
            CancellationToken cancellationToken)
        {
            var options = allowHwAccel ? _configurationManager.GetEncodingOptions() : new EncodingOptions();
            threads ??= _threads;

            if (allowHwAccel && enableKeyFrameOnlyExtraction)
            {
                var hardwareAccelerationType = options.HardwareAccelerationType;
                var supportsKeyFrameOnly = (hardwareAccelerationType == HardwareAccelerationType.nvenc && options.EnableEnhancedNvdecDecoder)
                                           || (hardwareAccelerationType == HardwareAccelerationType.amf && OperatingSystem.IsWindows())
                                           || (hardwareAccelerationType == HardwareAccelerationType.qsv && options.PreferSystemNativeHwDecoder)
                                           || hardwareAccelerationType == HardwareAccelerationType.vaapi
                                           || hardwareAccelerationType == HardwareAccelerationType.videotoolbox
                                           || hardwareAccelerationType == HardwareAccelerationType.rkmpp;
                if (!supportsKeyFrameOnly)
                {
                    // Disable hardware acceleration when the hardware decoder does not support keyframe only mode.
                    allowHwAccel = false;
                    options = new EncodingOptions();
                }
            }

            // A new EncodingOptions instance must be used as to not disable HW acceleration for all of Jellyfin.
            // Additionally, we must set a few fields without defaults to prevent null pointer exceptions.
            if (!allowHwAccel)
            {
                options.EnableHardwareEncoding = false;
                options.HardwareAccelerationType = HardwareAccelerationType.none;
                options.EnableTonemapping = false;
            }

            if (imageStream.Width is not null && imageStream.Height is not null && !string.IsNullOrEmpty(imageStream.AspectRatio))
            {
                // For hardware trickplay encoders, we need to re-calculate the size because they used fixed scale dimensions
                var darParts = imageStream.AspectRatio.Split(':');
                var (wa, ha) = (double.Parse(darParts[0], CultureInfo.InvariantCulture), double.Parse(darParts[1], CultureInfo.InvariantCulture));
                // When dimension / DAR does not equal to 1:1, then the frames are most likely stored stretched.
                // Note: this might be incorrect for 3D videos as the SAR stored might be per eye instead of per video, but we really can do little about it.
                var shouldResetHeight = Math.Abs((imageStream.Width.Value * ha) - (imageStream.Height.Value * wa)) > .05;
                if (shouldResetHeight)
                {
                    // SAR = DAR * Height / Width
                    // RealHeight = Height / SAR = Height / (DAR * Height / Width) = Width / DAR
                    imageStream.Height = Convert.ToInt32(imageStream.Width.Value * ha / wa);
                }
            }

            var baseRequest = new BaseEncodingJobOptions { MaxWidth = maxWidth, MaxFramerate = (float)(1.0 / interval.TotalSeconds) };
            var jobState = new EncodingJobInfo(TranscodingJobType.Progressive)
            {
                IsVideoRequest = true,  // must be true for InputVideoHwaccelArgs to return non-empty value
                MediaSource = mediaSource,
                VideoStream = imageStream,
                BaseRequest = baseRequest,  // GetVideoProcessingFilterParam errors if null
                MediaPath = inputFile,
                OutputVideoCodec = "mjpeg"
            };
            var vidEncoder = enableHwEncoding ? encodingHelper.GetVideoEncoder(jobState, options) : jobState.OutputVideoCodec;

            // Get input and filter arguments
            var inputArg = encodingHelper.GetInputArgument(jobState, options, container).Trim();
            if (string.IsNullOrWhiteSpace(inputArg))
            {
                throw new InvalidOperationException("EncodingHelper returned empty input arguments.");
            }

            if (!allowHwAccel)
            {
                inputArg = "-threads " + threads + " " + inputArg; // HW accel args set a different input thread count, only set if disabled
            }

            if (options.HardwareAccelerationType == HardwareAccelerationType.videotoolbox && _isLowPriorityHwDecodeSupported)
            {
                // VideoToolbox supports low priority decoding, which is useful for trickplay
                inputArg = "-hwaccel_flags +low_priority " + inputArg;
            }

            var filterParam = encodingHelper.GetVideoProcessingFilterParam(jobState, options, vidEncoder).Trim();
            if (string.IsNullOrWhiteSpace(filterParam))
            {
                throw new InvalidOperationException("EncodingHelper returned empty or invalid filter parameters.");
            }

            // Keyframe-only extraction takes whatever keyframes exist, so it never samples a timeline.
            var normalizeFrameRate = enableKeyFrameOnlyExtraction
                ? 0
                : (imageStream.ReferenceFrameRate is > 0 ? imageStream.ReferenceFrameRate.Value : FallbackFrameRate);

            try
            {
                return await ExtractVideoImagesOnIntervalInternal(
                    (enableKeyFrameOnlyExtraction ? "-skip_frame nokey " : string.Empty) + inputArg,
                    filterParam,
                    normalizeFrameRate,
                    vidEncoder,
                    qualityScale,
                    priority,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (FfmpegException ex)
            {
                if (!enableKeyFrameOnlyExtraction)
                {
                    throw;
                }

                _logger.LogWarning(ex, "I-frame trickplay extraction failed, will attempt standard way. Input: {InputFile}", inputFile);
            }

            return await ExtractVideoImagesOnIntervalInternal(inputArg, filterParam, normalizeFrameRate, vidEncoder, qualityScale, priority, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> ExtractVideoImagesOnIntervalInternal(
            string inputArg,
            string filterParam,
            double normalizeFrameRate,
            string vidEncoder,
            int? qualityScale,
            ProcessPriorityClass? priority,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputArg))
            {
                throw new InvalidOperationException("Empty or invalid input argument.");
            }

            // Output arguments
            var targetDirectory = Path.Combine(_configurationManager.ApplicationPaths.TempDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(targetDirectory);
            var outputPath = Path.Combine(targetDirectory, "%08d.jpg");

            var idleTimeoutMs = _configurationManager.Configuration.ImageExtractionTimeoutMs;
            idleTimeoutMs = idleTimeoutMs <= 0 ? DefaultHdrImageExtractionTimeout : idleTimeoutMs;

            var request = new TrickplayRequest
            {
                Input = inputArg,
                FilterChain = filterParam,
                NormalizeTimestampsAtFrameRate = normalizeFrameRate,
                OutputPath = outputPath,
                VideoEncoder = vidEncoder,
                EncoderVersion = EncoderVersion,
                QualityScale = qualityScale ?? TrickplayRequest.DefaultQualityScale,
                Priority = priority ?? FFDefaults.InheritPriority,

                // ffmpeg runs for as long as the media is long, so the wall clock cannot bound this.
                // New tiles appearing is the liveness signal instead.
                IdleTimeout = TimeSpan.FromMilliseconds(idleTimeoutMs),
                ProgressProbe = () => _fileSystem.GetFilePaths(targetDirectory).Count()
            };

            FFResult result;
            using (await _thumbnailResourcePool.LockAsync(cancellationToken).ConfigureAwait(false))
            {
                result = await _ffRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);
            }

            if (!result.Succeeded)
            {
                if (result.StopReason == FFStopReason.Stalled)
                {
                    _logger.LogInformation("Trickplay process stopped producing images; giving up.");
                }

                // Clean up here: targetDirectory is not returned on failure, so the caller cannot.
                // ffmpeg ideally would not write anything when it fails, but that is not guaranteed.
                try
                {
                    Directory.Delete(targetDirectory, true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to delete ffmpeg temp directory {TargetDirectory}", targetDirectory);
                }

                throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg trickplay extraction failed for {0}", outputPath));
            }

            return targetDirectory;
        }

        public string GetTimeParameter(long ticks)
        {
            var time = TimeSpan.FromTicks(ticks);

            return GetTimeParameter(time);
        }

        public string GetTimeParameter(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
        }

        public string EscapeSubtitleFilterPath(string path)
        {
            // https://ffmpeg.org/ffmpeg-filters.html#Notes-on-filtergraph-escaping
            // We need to double escape

            return path
                .Replace('\\', '/')
                .Replace(":", "\\:", StringComparison.Ordinal)
                .Replace("'", @"'\\\''", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _thumbnailResourcePool.Dispose();
            }
        }

        /// <inheritdoc />
        public Task ConvertImage(string inputPath, string outputPath)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetPrimaryPlaylistVobFiles(string path, uint? titleNumber)
        {
            // Eliminate menus and intros by omitting VIDEO_TS.VOB and all subsequent title .vob files ending with _0.VOB
            var allVobs = _fileSystem.GetFiles(path, true)
                .Where(file => string.Equals(file.Extension, ".VOB", StringComparison.OrdinalIgnoreCase))
                .Where(file => !string.Equals(file.Name, "VIDEO_TS.VOB", StringComparison.OrdinalIgnoreCase))
                .Where(file => !file.Name.EndsWith("_0.VOB", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.FullName)
                .ToList();

            if (titleNumber.HasValue)
            {
                var prefix = string.Format(CultureInfo.InvariantCulture, "VTS_{0:D2}_", titleNumber.Value);
                var vobs = allVobs.Where(i => i.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

                if (vobs.Count > 0)
                {
                    return vobs.Select(i => i.FullName).ToList();
                }

                _logger.LogWarning("Could not determine .vob files for title {Title} of {Path}.", titleNumber, path);
            }

            // Check for multiple big titles (> 900 MB)
            var titles = allVobs
                .Where(vob => vob.Length >= 900 * 1024 * 1024)
                .Select(vob => _fileSystem.GetFileNameWithoutExtension(vob).AsSpan().RightPart('_').ToString())
                .Distinct()
                .ToList();

            // Fall back to first title if no big title is found
            if (titles.Count == 0)
            {
                titles.Add(_fileSystem.GetFileNameWithoutExtension(allVobs[0]).AsSpan().RightPart('_').ToString());
            }

            // Aggregate all .vob files of the titles
            return allVobs
                .Where(vob => titles.Contains(_fileSystem.GetFileNameWithoutExtension(vob).AsSpan().RightPart('_').ToString()))
                .Select(i => i.FullName)
                .Order()
                .ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetPrimaryPlaylistM2tsFiles(string path)
            => _blurayExaminer.GetDiscInfo(path).Files;

        /// <inheritdoc />
        public string GetInputPathArgument(EncodingJobInfo state)
            => GetInputPathArgument(state.MediaPath, state.MediaSource);

        /// <inheritdoc />
        public string GetInputPathArgument(string path, MediaSourceInfo mediaSource)
        {
            return mediaSource.VideoType switch
            {
                VideoType.Dvd => GetInputArgument(GetPrimaryPlaylistVobFiles(path, null), mediaSource),
                VideoType.BluRay => GetInputArgument(GetPrimaryPlaylistM2tsFiles(path), mediaSource),
                _ => GetInputArgument(path, mediaSource)
            };
        }

        /// <inheritdoc />
        public void GenerateConcatConfig(MediaSourceInfo source, string concatFilePath)
        {
            // Get all playable files
            IReadOnlyList<string> files;
            var videoType = source.VideoType;
            if (videoType == VideoType.Dvd)
            {
                files = GetPrimaryPlaylistVobFiles(source.Path, null);
            }
            else if (videoType == VideoType.BluRay)
            {
                files = GetPrimaryPlaylistM2tsFiles(source.Path);
            }
            else
            {
                return;
            }

            // Generate concat configuration entries for each file and write to file
            Directory.CreateDirectory(Path.GetDirectoryName(concatFilePath));
            using var sw = new FormattingStreamWriter(concatFilePath, CultureInfo.InvariantCulture);
            foreach (var path in files)
            {
                var mediaInfoResult = GetMediaInfo(
                    new MediaInfoRequest
                    {
                        MediaType = DlnaProfileType.Video,
                        MediaSource = new MediaSourceInfo
                        {
                            Path = path,
                            Protocol = MediaProtocol.File,
                            VideoType = videoType
                        }
                    },
                    CancellationToken.None).GetAwaiter().GetResult();

                var duration = TimeSpan.FromTicks(mediaInfoResult.RunTimeTicks.Value).TotalSeconds;

                // Add file path stanza to concat configuration
                sw.WriteLine("file '{0}'", path.Replace("'", @"'\''", StringComparison.Ordinal));

                // Add duration stanza to concat configuration
                sw.WriteLine("duration {0}", duration);
            }
        }

        public bool CanExtractSubtitles(string codec)
        {
            return _configurationManager.GetEncodingOptions().EnableSubtitleExtraction;
        }
    }
}
