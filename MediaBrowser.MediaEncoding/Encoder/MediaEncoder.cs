#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

        private readonly ILogger<MediaEncoder> _logger;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly IBlurayExaminer _blurayExaminer;
        private readonly IConfiguration _config;
        private readonly IServerConfigurationManager _serverConfig;
        private readonly string _startupOptionFFmpegPath;

        private readonly AsyncNonKeyedLocker _thumbnailResourcePool;

        private readonly object _runningProcessesLock = new object();
        private readonly List<ProcessWrapper> _runningProcesses = new List<ProcessWrapper>();

        // MediaEncoder is registered as a Singleton
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        private List<string> _encoders = new List<string>();
        private List<string> _decoders = new List<string>();
        private List<string> _hwaccels = new List<string>();
        private List<string> _filters = new List<string>();
        private IDictionary<int, bool> _filtersWithOption = new Dictionary<int, bool>();

        private bool _isPkeyPauseSupported = false;
        private bool _isLowPriorityHwDecodeSupported = false;

        private bool _isVaapiDeviceAmd = false;
        private bool _isVaapiDeviceInteliHD = false;
        private bool _isVaapiDeviceInteli965 = false;
        private bool _isVaapiDeviceSupportVulkanDrmModifier = false;
        private bool _isVaapiDeviceSupportVulkanDrmInterop = false;

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

        private Version _ffmpegVersion = null;
        private string _ffmpegPath = string.Empty;
        private string _ffprobePath;
        private int _threads;

        public MediaEncoder(
            ILogger<MediaEncoder> logger,
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            IBlurayExaminer blurayExaminer,
            ILocalizationManager localization,
            IConfiguration config,
            IServerConfigurationManager serverConfig)
        {
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
        public string EncoderPath => _ffmpegPath;

        /// <inheritdoc />
        public string ProbePath => _ffprobePath;

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

        [GeneratedRegex(@"[^\/\\]+?(\.[^\/\\\n.]+)?$")]
        private static partial Regex FfprobePathRegex();

        /// <summary>
        /// Run at startup to validate ffmpeg.
        /// Sets global variables FFmpegPath.
        /// Precedence is: CLI/Env var > Config > $PATH.
        /// </summary>
        /// <returns>bool indicates whether a valid ffmpeg is found.</returns>
        public bool SetFFmpegPath()
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

            if (!ValidatePath(ffmpegPath))
            {
                _ffmpegPath = null;
                _logger.LogError("FFmpeg: Path set by {FfmpegPathSetMethodText} is invalid", ffmpegPathSetMethodText);
                return false;
            }

            // Write the FFmpeg path to the config/encoding.xml file as <EncoderAppPathDisplay> so it appears in UI
            var options = _configurationManager.GetEncodingOptions();
            options.EncoderAppPathDisplay = _ffmpegPath ?? string.Empty;
            _configurationManager.SaveConfiguration("encoding", options);

            // Only if mpeg path is set, try and set path to probe
            if (_ffmpegPath is not null)
            {
                // Determine a probe path from the mpeg path
                _ffprobePath = FfprobePathRegex().Replace(_ffmpegPath, "ffprobe$1");

                // Interrogate to understand what coders are supported
                var validator = new EncoderValidator(_logger, _ffmpegPath);

                SetAvailableDecoders(validator.GetDecoders());
                SetAvailableEncoders(validator.GetEncoders());
                SetAvailableFilters(validator.GetFilters());
                SetAvailableFiltersWithOption(validator.GetFiltersWithOption());
                SetAvailableHwaccels(validator.GetHwaccels());
                SetMediaEncoderVersion(validator);

                _threads = EncodingHelper.GetNumberOfThreads(null, options, null);

                _isPkeyPauseSupported = validator.CheckSupportedRuntimeKey("p      pause transcoding", _ffmpegVersion);
                _isLowPriorityHwDecodeSupported = validator.CheckSupportedHwaccelFlag("low_priority");

                // Check the Vaapi device vendor
                if (OperatingSystem.IsLinux()
                    && SupportsHwaccel("vaapi")
                    && !string.IsNullOrEmpty(options.VaapiDevice)
                    && options.HardwareAccelerationType == HardwareAccelerationType.vaapi)
                {
                    _isVaapiDeviceAmd = validator.CheckVaapiDeviceByDriverName("Mesa Gallium driver", options.VaapiDevice);
                    _isVaapiDeviceInteliHD = validator.CheckVaapiDeviceByDriverName("Intel iHD driver", options.VaapiDevice);
                    _isVaapiDeviceInteli965 = validator.CheckVaapiDeviceByDriverName("Intel i965 driver", options.VaapiDevice);
                    _isVaapiDeviceSupportVulkanDrmModifier = validator.CheckVulkanDrmDeviceByExtensionName(options.VaapiDevice, _vulkanImageDrmFmtModifierExts);
                    _isVaapiDeviceSupportVulkanDrmInterop = validator.CheckVulkanDrmDeviceByExtensionName(options.VaapiDevice, _vulkanExternalMemoryDmaBufExts);

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
            }

            _logger.LogInformation("FFmpeg: {FfmpegPath}", _ffmpegPath ?? string.Empty);
            return !string.IsNullOrWhiteSpace(ffmpegPath);
        }

        /// <summary>
        /// Validates the supplied FQPN to ensure it is a ffmpeg utility.
        /// If checks pass, global variable FFmpegPath is updated.
        /// </summary>
        /// <param name="path">FQPN to test.</param>
        /// <returns><c>true</c> if the version validation succeeded; otherwise, <c>false</c>.</returns>
        private bool ValidatePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            bool rc = new EncoderValidator(_logger, path).ValidateVersion();
            if (!rc)
            {
                _logger.LogError("FFmpeg: Failed version check: {Path}", path);
                return false;
            }

            _ffmpegPath = path;
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

        public void SetAvailableFiltersWithOption(IDictionary<int, bool> dict)
        {
            _filtersWithOption = dict;
        }

        public void SetMediaEncoderVersion(EncoderValidator validator)
        {
            _ffmpegVersion = validator.GetFFmpegVersion();
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
            if (_filtersWithOption.TryGetValue((int)option, out var val))
            {
                return val;
            }

            return false;
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
            var extractChapters = request.MediaType == DlnaProfileType.Video && request.ExtractChapters;
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
            var args = extractChapters
                ? "{0} -i {1} -threads {2} -v warning -print_format json -show_streams -show_chapters -show_format"
                : "{0} -i {1} -threads {2} -v warning -print_format json -show_streams -show_format";
            args = string.Format(CultureInfo.InvariantCulture, args, probeSizeArgument, inputPath, _threads).Trim();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both or ffmpeg may hang due to deadlocks.
                    RedirectStandardOutput = true,

                    FileName = _ffprobePath,
                    Arguments = args,

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                },
                EnableRaisingEvents = true
            };

            _logger.LogInformation("Starting {ProcessFileName} with args {ProcessArgs}", _ffprobePath, args);

            var memoryStream = new MemoryStream();
            await using (memoryStream.ConfigureAwait(false))
            using (var processWrapper = new ProcessWrapper(process, this))
            {
                StartProcess(processWrapper);
                using var reader = process.StandardOutput;
                await reader.BaseStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                memoryStream.Seek(0, SeekOrigin.Begin);
                InternalMediaInfoResult result;
                try
                {
                    result = await JsonSerializer.DeserializeAsync<InternalMediaInfoResult>(
                                        memoryStream,
                                        _jsonSerializerOptions,
                                        cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    StopProcess(processWrapper, 100);

                    throw;
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
                    _logger.LogError(ex, "I-frame image extraction failed, will attempt standard way. Input: {Arguments}", inputArgument);
                }
            }

            return await ExtractImageInternal(inputArgument, container, videoStream, imageStreamIndex, threedFormat, offset, false, targetFormat, isAudio, cancellationToken).ConfigureAwait(false);
        }

        private string GetImageResolutionParameter()
        {
            var imageResolutionParameter = _serverConfig.Configuration.ChapterImageResolution switch
            {
                ImageResolution.P144 => "256x144",
                ImageResolution.P240 => "426x240",
                ImageResolution.P360 => "640x360",
                ImageResolution.P480 => "854x480",
                ImageResolution.P720 => "1280x720",
                ImageResolution.P1080 => "1920x1080",
                ImageResolution.P1440 => "2560x1440",
                ImageResolution.P2160 => "3840x2160",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(imageResolutionParameter))
            {
                imageResolutionParameter = " -s " + imageResolutionParameter;
            }

            return imageResolutionParameter;
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

            // Use ffmpeg to sample 100 (we can drop this if required using thumbnail=50 for 50 frames) frames and pick the best thumbnail. Have a fall back just in case.
            // mpegts need larger batch size otherwise the corrupted thumbnail will be created. Larger batch size will lower the processing speed.
            var enableThumbnail = !useTradeoff && useIFrame && !string.Equals("wtv", container, StringComparison.OrdinalIgnoreCase);
            if (enableThumbnail)
            {
                var useLargerBatchSize = string.Equals("mpegts", container, StringComparison.OrdinalIgnoreCase);
                filters.Add("thumbnail=n=" + (useLargerBatchSize ? "50" : "24"));
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
                    filters.Add($"tonemapx=tonemap=bt2390:desat=0:peak={peak}:t=bt709:m=bt709:p=bt709:format=yuv420p");
                }
                else if (SupportsFilter("zscale") && videoStream.VideoRangeType != VideoRangeType.DOVI)
                {
                    enableHdrExtraction = true;
                    filters.Add("zscale=t=linear:npl=100,format=gbrpf32le,zscale=p=bt709,tonemap=tonemap=hable:desat=0:peak=100,zscale=t=bt709:m=bt709,format=yuv420p");
                }
            }

            var vf = string.Join(',', filters);
            var mapArg = imageStreamIndex.HasValue ? (" -map 0:" + imageStreamIndex.Value.ToString(CultureInfo.InvariantCulture)) : string.Empty;
            var args = string.Format(CultureInfo.InvariantCulture, "-i {0}{3} -threads {4} -v quiet -vframes 1 -vf {2}{5} -f image2 \"{1}\"", inputPath, tempExtractPath, vf, mapArg, _threads, isAudio ? string.Empty : GetImageResolutionParameter());

            if (offset.HasValue)
            {
                args = string.Format(CultureInfo.InvariantCulture, "-ss {0} ", GetTimeParameter(offset.Value)) + args;
            }

            if (useIFrame && useTradeoff)
            {
                args = "-skip_frame nokey " + args;
            }

            if (!string.IsNullOrWhiteSpace(container))
            {
                var inputFormat = EncodingHelper.GetInputFormat(container);
                if (!string.IsNullOrWhiteSpace(inputFormat))
                {
                    args = "-f " + inputFormat + " " + args;
                }
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                },
                EnableRaisingEvents = true
            };

            _logger.LogDebug("{ProcessFileName} {ProcessArguments}", process.StartInfo.FileName, process.StartInfo.Arguments);

            using (var processWrapper = new ProcessWrapper(process, this))
            {
                using (await _thumbnailResourcePool.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    StartProcess(processWrapper);

                    var timeoutMs = _configurationManager.Configuration.ImageExtractionTimeoutMs;
                    if (timeoutMs <= 0)
                    {
                        timeoutMs = enableHdrExtraction ? DefaultHdrImageExtractionTimeout : DefaultSdrImageExtractionTimeout;
                    }

                    try
                    {
                        await process.WaitForExitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException ex)
                    {
                        process.Kill(true);
                        throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg image extraction timed out for {0} after {1}ms", inputPath, timeoutMs), ex);
                    }
                }

                var file = _fileSystem.GetFileInfo(tempExtractPath);

                if (processWrapper.ExitCode > 0 || !file.Exists || file.Length == 0)
                {
                    throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg image extraction failed for {0}", inputPath));
                }

                return tempExtractPath;
            }
        }

        /// <inheritdoc />
        public Task<string> ExtractVideoImagesOnIntervalAccelerated(
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

            if (enableKeyFrameOnlyExtraction)
            {
                inputArg = "-skip_frame nokey " + inputArg;
            }

            var filterParam = encodingHelper.GetVideoProcessingFilterParam(jobState, options, vidEncoder).Trim();
            if (string.IsNullOrWhiteSpace(filterParam))
            {
                throw new InvalidOperationException("EncodingHelper returned empty or invalid filter parameters.");
            }

            return ExtractVideoImagesOnIntervalInternal(inputArg, filterParam, vidEncoder, threads, qualityScale, priority, cancellationToken);
        }

        private async Task<string> ExtractVideoImagesOnIntervalInternal(
            string inputArg,
            string filterParam,
            string vidEncoder,
            int? outputThreads,
            int? qualityScale,
            ProcessPriorityClass? priority,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(inputArg))
            {
                throw new InvalidOperationException("Empty or invalid input argument.");
            }

            // ffmpeg qscale is a value from 1-31, with 1 being best quality and 31 being worst
            // jpeg quality is a value from 0-100, with 0 being worst quality and 100 being best
            var encoderQuality = Math.Clamp(qualityScale ?? 4, 1, 31);
            var encoderQualityOption = "-qscale:v ";

            if (vidEncoder.Contains("vaapi", StringComparison.OrdinalIgnoreCase)
                || vidEncoder.Contains("qsv", StringComparison.OrdinalIgnoreCase))
            {
                // vaapi and qsv's mjpeg encoder use jpeg quality as input, instead of ffmpeg defined qscale
                encoderQuality = 100 - ((encoderQuality - 1) * (100 / 30));
                encoderQualityOption = "-global_quality:v ";
            }

            if (vidEncoder.Contains("videotoolbox", StringComparison.OrdinalIgnoreCase))
            {
                // videotoolbox's mjpeg encoder uses jpeg quality scaled to QP2LAMBDA (118) instead of ffmpeg defined qscale
                encoderQuality = 118 - ((encoderQuality - 1) * (118 / 30));
            }

            if (vidEncoder.Contains("rkmpp", StringComparison.OrdinalIgnoreCase))
            {
                // rkmpp's mjpeg encoder uses jpeg quality as input (max is 99, not 100), instead of ffmpeg defined qscale
                encoderQuality = 99 - ((encoderQuality - 1) * (99 / 30));
                encoderQualityOption = "-qp_init:v ";
            }

            // Output arguments
            var targetDirectory = Path.Combine(_configurationManager.ApplicationPaths.TempDirectory, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(targetDirectory);
            var outputPath = Path.Combine(targetDirectory, "%08d.jpg");

            // Final command arguments
            var args = string.Format(
                CultureInfo.InvariantCulture,
                "-loglevel error {0} -an -sn {1} -threads {2} -c:v {3} {4}{5}{6}-f {7} \"{8}\"",
                inputArg,
                filterParam,
                outputThreads.GetValueOrDefault(_threads),
                vidEncoder,
                encoderQualityOption + encoderQuality + " ",
                vidEncoder.Contains("videotoolbox", StringComparison.InvariantCultureIgnoreCase) ? "-allow_sw 1 " : string.Empty, // allow_sw fallback for some intel macs
                EncodingHelper.GetVideoSyncOption("0", EncoderVersion).Trim() + " ", // passthrough timestamp
                "image2",
                outputPath);

            // Start ffmpeg process
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = _ffmpegPath,
                    Arguments = args,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                },
                EnableRaisingEvents = true
            };

            var processDescription = string.Format(CultureInfo.InvariantCulture, "{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            _logger.LogInformation("Trickplay generation: {ProcessDescription}", processDescription);

            using (var processWrapper = new ProcessWrapper(process, this))
            {
                bool ranToCompletion = false;

                using (await _thumbnailResourcePool.LockAsync(cancellationToken).ConfigureAwait(false))
                {
                    StartProcess(processWrapper);

                    // Set process priority
                    if (priority.HasValue)
                    {
                        try
                        {
                            processWrapper.Process.PriorityClass = priority.Value;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Unable to set process priority to {Priority} for {Description}", priority.Value, processDescription);
                        }
                    }

                    // Need to give ffmpeg enough time to make all the thumbnails, which could be a while,
                    // but we still need to detect if the process hangs.
                    // Making the assumption that as long as new jpegs are showing up, everything is good.

                    bool isResponsive = true;
                    int lastCount = 0;
                    var timeoutMs = _configurationManager.Configuration.ImageExtractionTimeoutMs;
                    timeoutMs = timeoutMs <= 0 ? DefaultHdrImageExtractionTimeout : timeoutMs;

                    while (isResponsive && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await process.WaitForExitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);

                            ranToCompletion = true;
                            break;
                        }
                        catch (OperationCanceledException)
                        {
                            // We don't actually expect the process to be finished in one timeout span, just that one image has been generated.
                        }

                        var jpegCount = _fileSystem.GetFilePaths(targetDirectory).Count();

                        isResponsive = jpegCount > lastCount;
                        lastCount = jpegCount;
                    }

                    if (!ranToCompletion)
                    {
                        if (!isResponsive)
                        {
                            _logger.LogInformation("Trickplay process unresponsive.");
                        }

                        _logger.LogInformation("Stopping trickplay extraction.");
                        StopProcess(processWrapper, 1000);
                    }
                }

                var exitCode = ranToCompletion ? processWrapper.ExitCode ?? 0 : -1;

                if (exitCode == -1)
                {
                    _logger.LogError("ffmpeg image extraction failed for {ProcessDescription}", processDescription);
                    // Cleanup temp folder here, because the targetDirectory is not returned and the cleanup for failed ffmpeg process is not possible for caller.
                    // Ideally the ffmpeg should not write any files if it fails, but it seems like it is not guaranteed.
                    try
                    {
                        Directory.Delete(targetDirectory, true);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to delete ffmpeg temp directory {TargetDirectory}", targetDirectory);
                    }

                    throw new FfmpegException(string.Format(CultureInfo.InvariantCulture, "ffmpeg image extraction failed for {0}", processDescription));
                }

                return targetDirectory;
            }
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

        private void StartProcess(ProcessWrapper process)
        {
            process.Process.Start();

            lock (_runningProcessesLock)
            {
                _runningProcesses.Add(process);
            }
        }

        private void StopProcess(ProcessWrapper process, int waitTimeMs)
        {
            try
            {
                if (process.Process.WaitForExit(waitTimeMs))
                {
                    return;
                }

                _logger.LogInformation("Killing ffmpeg process");

                process.Process.Kill();
            }
            catch (InvalidOperationException)
            {
                // The process has already exited or
                // there is no process associated with this Process object.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing process");
            }
        }

        private void StopProcesses()
        {
            List<ProcessWrapper> proceses;
            lock (_runningProcessesLock)
            {
                proceses = _runningProcesses.ToList();
                _runningProcesses.Clear();
            }

            foreach (var process in proceses)
            {
                if (!process.HasExited)
                {
                    StopProcess(process, 500);
                }
            }
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
                StopProcesses();
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
            // TODO is there ever a case when a subtitle can't be extracted??
            return true;
        }

        private sealed class ProcessWrapper : IDisposable
        {
            private readonly MediaEncoder _mediaEncoder;

            private bool _disposed = false;

            public ProcessWrapper(Process process, MediaEncoder mediaEncoder)
            {
                Process = process;
                _mediaEncoder = mediaEncoder;
                Process.Exited += OnProcessExited;
            }

            public Process Process { get; }

            public bool HasExited { get; private set; }

            public int? ExitCode { get; private set; }

            private void OnProcessExited(object sender, EventArgs e)
            {
                var process = (Process)sender;

                HasExited = true;

                try
                {
                    ExitCode = process.ExitCode;
                }
                catch
                {
                }

                DisposeProcess(process);
            }

            private void DisposeProcess(Process process)
            {
                lock (_mediaEncoder._runningProcessesLock)
                {
                    _mediaEncoder._runningProcesses.Remove(this);
                }

                process.Dispose();
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    if (Process is not null)
                    {
                        Process.Exited -= OnProcessExited;
                        DisposeProcess(Process);
                    }
                }

                _disposed = true;
            }
        }
    }
}
