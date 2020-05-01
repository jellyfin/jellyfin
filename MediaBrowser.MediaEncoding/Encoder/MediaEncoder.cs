using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MediaBrowser.MediaEncoding.Encoder
{
    /// <summary>
    /// Class MediaEncoder
    /// </summary>
    public class MediaEncoder : IMediaEncoder, IDisposable
    {
        /// <summary>
        /// The default image extraction timeout in milliseconds.
        /// </summary>
        internal const int DefaultImageExtractionTimeout = 5000;

        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly Lazy<EncodingHelper> _encodingHelperFactory;
        private readonly string _startupOptionFFmpegPath;

        private readonly SemaphoreSlim _thumbnailResourcePool = new SemaphoreSlim(2, 2);

        private readonly object _runningProcessesLock = new object();
        private readonly List<ProcessWrapper> _runningProcesses = new List<ProcessWrapper>();

        private string _ffmpegPath;
        private string _ffprobePath;

        public MediaEncoder(
            ILogger<MediaEncoder> logger,
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            ILocalizationManager localization,
            Lazy<EncodingHelper> encodingHelperFactory,
            string startupOptionsFFmpegPath)
        {
            _logger = logger;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
            _encodingHelperFactory = encodingHelperFactory;
            _startupOptionFFmpegPath = startupOptionsFFmpegPath;
        }

        private EncodingHelper EncodingHelper => _encodingHelperFactory.Value;

        /// <inheritdoc />
        public string EncoderPath => _ffmpegPath;

        /// <inheritdoc />
        public FFmpegLocation EncoderLocation { get; private set; }

        /// <summary>
        /// Run at startup or if the user removes a Custom path from transcode page.
        /// Sets global variables FFmpegPath.
        /// Precedence is: Config > CLI > $PATH
        /// </summary>
        public void SetFFmpegPath()
        {
            // 1) Custom path stored in config/encoding xml file under tag <EncoderAppPath> takes precedence
            if (!ValidatePath(_configurationManager.GetConfiguration<EncodingOptions>("encoding").EncoderAppPath, FFmpegLocation.Custom))
            {
                // 2) Check if the --ffmpeg CLI switch has been given
                if (!ValidatePath(_startupOptionFFmpegPath, FFmpegLocation.SetByArgument))
                {
                    // 3) Search system $PATH environment variable for valid FFmpeg
                    if (!ValidatePath(ExistsOnSystemPath("ffmpeg"), FFmpegLocation.System))
                    {
                        EncoderLocation = FFmpegLocation.NotFound;
                        _ffmpegPath = null;
                    }
                }
            }

            // Write the FFmpeg path to the config/encoding.xml file as <EncoderAppPathDisplay> so it appears in UI
            var config = _configurationManager.GetConfiguration<EncodingOptions>("encoding");
            config.EncoderAppPathDisplay = _ffmpegPath ?? string.Empty;
            _configurationManager.SaveConfiguration("encoding", config);

            // Only if mpeg path is set, try and set path to probe
            if (_ffmpegPath != null)
            {
                // Determine a probe path from the mpeg path
                _ffprobePath = Regex.Replace(_ffmpegPath, @"[^\/\\]+?(\.[^\/\\\n.]+)?$", @"ffprobe$1");

                // Interrogate to understand what coders are supported
                var validator = new EncoderValidator(_logger, _ffmpegPath);

                SetAvailableDecoders(validator.GetDecoders());
                SetAvailableEncoders(validator.GetEncoders());
            }

            _logger.LogInformation("FFmpeg: {EncoderLocation}: {FfmpegPath}", EncoderLocation, _ffmpegPath ?? string.Empty);
        }

        /// <summary>
        /// Triggered from the Settings > Transcoding UI page when users submits Custom FFmpeg path to use.
        /// Only write the new path to xml if it exists.  Do not perform validation checks on ffmpeg here.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        public void UpdateEncoderPath(string path, string pathType)
        {
            string newPath;

            _logger.LogInformation("Attempting to update encoder path to {Path}. pathType: {PathType}", path ?? string.Empty, pathType ?? string.Empty);

            if (!string.Equals(pathType, "custom", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Unexpected pathType value");
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                // User had cleared the custom path in UI
                newPath = string.Empty;
            }
            else if (File.Exists(path))
            {
                newPath = path;
            }
            else if (Directory.Exists(path))
            {
                // Given path is directory, so resolve down to filename
                newPath = GetEncoderPathFromDirectory(path, "ffmpeg");
            }
            else
            {
                throw new ResourceNotFoundException();
            }

            // Write the new ffmpeg path to the xml as <EncoderAppPath>
            // This ensures its not lost on next startup
            var config = _configurationManager.GetConfiguration<EncodingOptions>("encoding");
            config.EncoderAppPath = newPath;
            _configurationManager.SaveConfiguration("encoding", config);

            // Trigger SetFFmpegPath so we validate the new path and setup probe path
            SetFFmpegPath();
        }

        /// <summary>
        /// Validates the supplied FQPN to ensure it is a ffmpeg utility.
        /// If checks pass, global variable FFmpegPath and EncoderLocation are updated.
        /// </summary>
        /// <param name="path">FQPN to test</param>
        /// <param name="location">Location (External, Custom, System) of tool</param>
        /// <returns></returns>
        private bool ValidatePath(string path, FFmpegLocation location)
        {
            bool rc = false;

            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    rc = new EncoderValidator(_logger, path).ValidateVersion();

                    if (!rc)
                    {
                        _logger.LogWarning("FFmpeg: {Location}: Failed version check: {Path}", location, path);
                    }

                    // ToDo - Enable the ffmpeg validator.  At the moment any version can be used.
                    rc = true;

                    _ffmpegPath = path;
                    EncoderLocation = location;
                }
                else
                {
                    _logger.LogWarning("FFmpeg: {Location}: File not found: {Path}", location, path);
                }
            }

            return rc;
        }

        private string GetEncoderPathFromDirectory(string path, string filename, bool recursive = false)
        {
            try
            {
                var files = _fileSystem.GetFilePaths(path, recursive);

                var excludeExtensions = new[] { ".c" };

                return files.FirstOrDefault(i => string.Equals(Path.GetFileNameWithoutExtension(i), filename, StringComparison.OrdinalIgnoreCase)
                                                    && !excludeExtensions.Contains(Path.GetExtension(i) ?? string.Empty));
            }
            catch (Exception)
            {
                // Trap all exceptions, like DirNotExists, and return null
                return null;
            }
        }

        /// <summary>
        /// Search the system $PATH environment variable looking for given filename.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string ExistsOnSystemPath(string fileName)
        {
            var inJellyfinPath = GetEncoderPathFromDirectory(AppContext.BaseDirectory, fileName, recursive: true);
            if (!string.IsNullOrEmpty(inJellyfinPath))
            {
                return inJellyfinPath;
            }
            var values = Environment.GetEnvironmentVariable("PATH");

            foreach (var path in values.Split(Path.PathSeparator))
            {
                var candidatePath = GetEncoderPathFromDirectory(path, fileName);

                if (!string.IsNullOrEmpty(candidatePath))
                {
                    return candidatePath;
                }
            }

            return null;
        }

        private List<string> _encoders = new List<string>();
        public void SetAvailableEncoders(IEnumerable<string> list)
        {
            _encoders = list.ToList();
            //_logger.Info("Supported encoders: {0}", string.Join(",", list.ToArray()));
        }

        private List<string> _decoders = new List<string>();
        public void SetAvailableDecoders(IEnumerable<string> list)
        {
            _decoders = list.ToList();
            //_logger.Info("Supported decoders: {0}", string.Join(",", list.ToArray()));
        }

        public bool SupportsEncoder(string encoder)
        {
            return _encoders.Contains(encoder, StringComparer.OrdinalIgnoreCase);
        }

        public bool SupportsDecoder(string decoder)
        {
            return _decoders.Contains(decoder, StringComparer.OrdinalIgnoreCase);
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

        /// <summary>
        /// Gets the media info.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task<MediaInfo> GetMediaInfo(MediaInfoRequest request, CancellationToken cancellationToken)
        {
            var extractChapters = request.MediaType == DlnaProfileType.Video && request.ExtractChapters;

            var inputFiles = MediaEncoderHelpers.GetInputArgument(_fileSystem, request.MediaSource.Path, request.MountedIso, request.PlayableStreamFileNames);

            var probeSize = EncodingHelper.GetProbeSizeArgument(inputFiles.Length);
            string analyzeDuration;

            if (request.MediaSource.AnalyzeDurationMs > 0)
            {
                analyzeDuration = "-analyzeduration " +
                                  (request.MediaSource.AnalyzeDurationMs * 1000).ToString();
            }
            else
            {
                analyzeDuration = EncodingHelper.GetAnalyzeDurationArgument(inputFiles.Length);
            }

            probeSize = probeSize + " " + analyzeDuration;
            probeSize = probeSize.Trim();

            var forceEnableLogging = request.MediaSource.Protocol != MediaProtocol.File;

            return GetMediaInfoInternal(GetInputArgument(inputFiles, request.MediaSource.Protocol), request.MediaSource.Path, request.MediaSource.Protocol, extractChapters,
                probeSize, request.MediaType == DlnaProfileType.Audio, request.MediaSource.VideoType, forceEnableLogging, cancellationToken);
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentException">Unrecognized InputType</exception>
        public string GetInputArgument(IReadOnlyList<string> inputFiles, MediaProtocol protocol)
            => EncodingUtils.GetInputArgument(inputFiles, protocol);

        /// <summary>
        /// Gets the media info internal.
        /// </summary>
        /// <returns>Task{MediaInfoResult}.</returns>
        private async Task<MediaInfo> GetMediaInfoInternal(string inputPath,
            string primaryPath,
            MediaProtocol protocol,
            bool extractChapters,
            string probeSizeArgument,
            bool isAudio,
            VideoType? videoType,
            bool forceEnableLogging,
            CancellationToken cancellationToken)
        {
            var args = extractChapters
                ? "{0} -i {1} -threads 0 -v warning -print_format json -show_streams -show_chapters -show_format"
                : "{0} -i {1} -threads 0 -v warning -print_format json -show_streams -show_format";
            args = string.Format(args, probeSizeArgument, inputPath).Trim();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
                    RedirectStandardOutput = true,

                    FileName = _ffprobePath,
                    Arguments = args,


                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,
                },
                EnableRaisingEvents = true
            };

            if (forceEnableLogging)
            {
                _logger.LogInformation("{ProcessFileName} {ProcessArgs}", process.StartInfo.FileName, process.StartInfo.Arguments);
            }
            else
            {
                _logger.LogDebug("{ProcessFileName} {ProcessArgs}", process.StartInfo.FileName, process.StartInfo.Arguments);
            }

            using (var processWrapper = new ProcessWrapper(process, this))
            {
                _logger.LogDebug("Starting ffprobe with args {Args}", args);
                StartProcess(processWrapper);

                InternalMediaInfoResult result;
                try
                {
                    result = await JsonSerializer.DeserializeAsync<InternalMediaInfoResult>(
                                        process.StandardOutput.BaseStream,
                                        cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    StopProcess(processWrapper, 100);

                    throw;
                }

                if (result == null || (result.Streams == null && result.Format == null))
                {
                    throw new Exception("ffprobe failed - streams and format are both null.");
                }

                if (result.Streams != null)
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

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        public Task<string> ExtractAudioImage(string path, int? imageStreamIndex, CancellationToken cancellationToken)
        {
            return ExtractImage(new[] { path }, null, null, imageStreamIndex, MediaProtocol.File, true, null, null, cancellationToken);
        }

        public Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, MediaStream videoStream, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFiles, container, videoStream, null, protocol, false, threedFormat, offset, cancellationToken);
        }

        public Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, MediaStream imageStream, int? imageStreamIndex, CancellationToken cancellationToken)
        {
            return ExtractImage(inputFiles, container, imageStream, imageStreamIndex, protocol, false, null, null, cancellationToken);
        }

        private async Task<string> ExtractImage(string[] inputFiles, string container, MediaStream videoStream, int? imageStreamIndex, MediaProtocol protocol, bool isAudio,
            Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken)
        {
            var inputArgument = GetInputArgument(inputFiles, protocol);

            if (isAudio)
            {
                if (imageStreamIndex.HasValue && imageStreamIndex.Value > 0)
                {
                    // It seems for audio files we need to subtract 1 (for the audio stream??)
                    imageStreamIndex = imageStreamIndex.Value - 1;
                }
            }
            else
            {
                try
                {
                    return await ExtractImageInternal(inputArgument, container, videoStream, imageStreamIndex, threedFormat, offset, true, cancellationToken).ConfigureAwait(false);
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

            return await ExtractImageInternal(inputArgument, container, videoStream, imageStreamIndex, threedFormat, offset, false, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> ExtractImageInternal(string inputPath, string container, MediaStream videoStream, int? imageStreamIndex, Video3DFormat? threedFormat, TimeSpan? offset, bool useIFrame, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(inputPath))
            {
                throw new ArgumentNullException(nameof(inputPath));
            }

            var tempExtractPath = Path.Combine(_configurationManager.ApplicationPaths.TempDirectory, Guid.NewGuid() + ".jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(tempExtractPath));

            // apply some filters to thumbnail extracted below (below) crop any black lines that we made and get the correct ar then scale to width 600.
            // This filter chain may have adverse effects on recorded tv thumbnails if ar changes during presentation ex. commercials @ diff ar
            var vf = "scale=600:trunc(600/dar/2)*2";

            if (threedFormat.HasValue)
            {
                switch (threedFormat.Value)
                {
                    case Video3DFormat.HalfSideBySide:
                        vf = "crop=iw/2:ih:0:0,scale=(iw*2):ih,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        // hsbs crop width in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to 600. Work out the correct height based on the display aspect it will maintain the aspect where -1 in this case (3d) may not.
                        break;
                    case Video3DFormat.FullSideBySide:
                        vf = "crop=iw/2:ih:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        //fsbs crop width in half,set the display aspect,crop out any black bars we may have made the scale width to 600.
                        break;
                    case Video3DFormat.HalfTopAndBottom:
                        vf = "crop=iw:ih/2:0:0,scale=(iw*2):ih),setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        //htab crop heigh in half,scale to correct size, set the display aspect,crop out any black bars we may have made the scale width to 600
                        break;
                    case Video3DFormat.FullTopAndBottom:
                        vf = "crop=iw:ih/2:0:0,setdar=dar=a,crop=min(iw\\,ih*dar):min(ih\\,iw/dar):(iw-min(iw\\,iw*sar))/2:(ih - min (ih\\,ih/sar))/2,setsar=sar=1,scale=600:trunc(600/dar/2)*2";
                        // ftab crop heigt in half, set the display aspect,crop out any black bars we may have made the scale width to 600
                        break;
                    default:
                        break;
                }
            }

            var mapArg = imageStreamIndex.HasValue ? (" -map 0:v:" + imageStreamIndex.Value.ToString(CultureInfo.InvariantCulture)) : string.Empty;

            var enableThumbnail = !new List<string> { "wtv" }.Contains(container ?? string.Empty, StringComparer.OrdinalIgnoreCase);
            // Use ffmpeg to sample 100 (we can drop this if required using thumbnail=50 for 50 frames) frames and pick the best thumbnail. Have a fall back just in case.
            var thumbnail = enableThumbnail ? ",thumbnail=24" : string.Empty;

            var args = useIFrame ? string.Format("-i {0}{3} -threads 0 -v quiet -vframes 1 -vf \"{2}{4}\" -f image2 \"{1}\"", inputPath, tempExtractPath, vf, mapArg, thumbnail) :
                string.Format("-i {0}{3} -threads 0 -v quiet -vframes 1 -vf \"{2}\" -f image2 \"{1}\"", inputPath, tempExtractPath, vf, mapArg);

            var probeSizeArgument = EncodingHelper.GetProbeSizeArgument(1);
            var analyzeDurationArgument = EncodingHelper.GetAnalyzeDurationArgument(1);

            if (!string.IsNullOrWhiteSpace(probeSizeArgument))
            {
                args = probeSizeArgument + " " + args;
            }

            if (!string.IsNullOrWhiteSpace(analyzeDurationArgument))
            {
                args = analyzeDurationArgument + " " + args;
            }

            if (offset.HasValue)
            {
                args = string.Format("-ss {0} ", GetTimeParameter(offset.Value)) + args;
            }

            if (videoStream != null)
            {
                /* fix
                var decoder = encodinghelper.GetHardwareAcceleratedVideoDecoder(VideoType.VideoFile, videoStream, GetEncodingOptions());
                if (!string.IsNullOrWhiteSpace(decoder))
                {
                    args = decoder + " " + args;
                }
                */
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
                bool ranToCompletion;

                await _thumbnailResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    StartProcess(processWrapper);

                    var timeoutMs = _configurationManager.Configuration.ImageExtractionTimeoutMs;
                    if (timeoutMs <= 0)
                    {
                        timeoutMs = DefaultImageExtractionTimeout;
                    }

                    ranToCompletion = await process.WaitForExitAsync(TimeSpan.FromMilliseconds(timeoutMs)).ConfigureAwait(false);

                    if (!ranToCompletion)
                    {
                        StopProcess(processWrapper, 1000);
                    }
                }
                finally
                {
                    _thumbnailResourcePool.Release();
                }

                var exitCode = ranToCompletion ? processWrapper.ExitCode ?? 0 : -1;
                var file = _fileSystem.GetFileInfo(tempExtractPath);

                if (exitCode == -1 || !file.Exists || file.Length == 0)
                {
                    var msg = string.Format("ffmpeg image extraction failed for {0}", inputPath);

                    _logger.LogError(msg);

                    throw new Exception(msg);
                }

                return tempExtractPath;
            }
        }

        public string GetTimeParameter(long ticks)
        {
            var time = TimeSpan.FromTicks(ticks);

            return GetTimeParameter(time);
        }

        public string GetTimeParameter(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss\.fff", UsCulture);
        }

        public async Task ExtractVideoImagesOnInterval(
            string[] inputFiles,
            string container,
            MediaStream videoStream,
            MediaProtocol protocol,
            Video3DFormat? threedFormat,
            TimeSpan interval,
            string targetDirectory,
            string filenamePrefix,
            int? maxWidth,
            CancellationToken cancellationToken)
        {
            var inputArgument = GetInputArgument(inputFiles, protocol);

            var vf = "fps=fps=1/" + interval.TotalSeconds.ToString(UsCulture);

            if (maxWidth.HasValue)
            {
                var maxWidthParam = maxWidth.Value.ToString(UsCulture);

                vf += string.Format(",scale=min(iw\\,{0}):trunc(ow/dar/2)*2", maxWidthParam);
            }

            Directory.CreateDirectory(targetDirectory);
            var outputPath = Path.Combine(targetDirectory, filenamePrefix + "%05d.jpg");

            var args = string.Format("-i {0} -threads 0 -v quiet -vf \"{2}\" -f image2 \"{1}\"", inputArgument, outputPath, vf);

            var probeSizeArgument = EncodingHelper.GetProbeSizeArgument(1);
            var analyzeDurationArgument = EncodingHelper.GetAnalyzeDurationArgument(1);

            if (!string.IsNullOrWhiteSpace(probeSizeArgument))
            {
                args = probeSizeArgument + " " + args;
            }

            if (!string.IsNullOrWhiteSpace(analyzeDurationArgument))
            {
                args = analyzeDurationArgument + " " + args;
            }

            if (videoStream != null)
            {
                /* fix
                var decoder = encodinghelper.GetHardwareAcceleratedVideoDecoder(VideoType.VideoFile, videoStream, GetEncodingOptions());
                if (!string.IsNullOrWhiteSpace(decoder))
                {
                    args = decoder + " " + args;
                }
                */
            }

            if (!string.IsNullOrWhiteSpace(container))
            {
                var inputFormat = EncodingHelper.GetInputFormat(container);
                if (!string.IsNullOrWhiteSpace(inputFormat))
                {
                    args = "-f " + inputFormat + " " + args;
                }
            }

            var processStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = _ffmpegPath,
                Arguments = args,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };

            _logger.LogInformation(processStartInfo.FileName + " " + processStartInfo.Arguments);

            await _thumbnailResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            bool ranToCompletion = false;

            var process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            using (var processWrapper = new ProcessWrapper(process, this))
            {
                try
                {
                    StartProcess(processWrapper);

                    // Need to give ffmpeg enough time to make all the thumbnails, which could be a while,
                    // but we still need to detect if the process hangs.
                    // Making the assumption that as long as new jpegs are showing up, everything is good.

                    bool isResponsive = true;
                    int lastCount = 0;

                    while (isResponsive)
                    {
                        if (await process.WaitForExitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false))
                        {
                            ranToCompletion = true;
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        var jpegCount = _fileSystem.GetFilePaths(targetDirectory)
                            .Count(i => string.Equals(Path.GetExtension(i), ".jpg", StringComparison.OrdinalIgnoreCase));

                        isResponsive = jpegCount > lastCount;
                        lastCount = jpegCount;
                    }

                    if (!ranToCompletion)
                    {
                        StopProcess(processWrapper, 1000);
                    }
                }
                finally
                {
                    _thumbnailResourcePool.Release();
                }

                var exitCode = ranToCompletion ? processWrapper.ExitCode ?? 0 : -1;

                if (exitCode == -1)
                {
                    var msg = string.Format("ffmpeg image extraction failed for {0}", inputArgument);

                    _logger.LogError(msg);

                    throw new Exception(msg);
                }
            }
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

            return path.Replace('\\', '/').Replace(":", "\\:").Replace("'", "'\\\\\\''");
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
            }
        }

        /// <inheritdoc />
        public Task ConvertImage(string inputPath, string outputPath)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<string> GetPrimaryPlaylistVobFiles(string path, IIsoMount isoMount, uint? titleNumber)
        {
            // min size 300 mb
            const long MinPlayableSize = 314572800;

            var root = isoMount != null ? isoMount.MountedPath : path;

            // Try to eliminate menus and intros by skipping all files at the front of the list that are less than the minimum size
            // Once we reach a file that is at least the minimum, return all subsequent ones
            var allVobs = _fileSystem.GetFiles(root, true)
                .Where(file => string.Equals(file.Extension, ".vob", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.FullName)
                .ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (allVobs.Count == 0)
            {
                _logger.LogWarning("No vobs found in dvd structure.");
                return Enumerable.Empty<string>();
            }

            if (titleNumber.HasValue)
            {
                var prefix = string.Format(
                    CultureInfo.InvariantCulture,
                    titleNumber.Value >= 10 ? "VTS_{0}_" : "VTS_0{0}_",
                    titleNumber.Value);
                var vobs = allVobs.Where(i => i.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

                if (vobs.Count > 0)
                {
                    var minSizeVobs = vobs
                        .SkipWhile(f => f.Length < MinPlayableSize)
                        .ToList();

                    return minSizeVobs.Count == 0 ? vobs.Select(i => i.FullName) : minSizeVobs.Select(i => i.FullName);
                }

                _logger.LogWarning("Could not determine vob file list for {Path} using DvdLib. Will scan using file sizes.", path);
            }

            var files = allVobs
                .SkipWhile(f => f.Length < MinPlayableSize)
                .ToList();

            // If we didn't find any satisfying the min length, just take them all
            if (files.Count == 0)
            {
                _logger.LogWarning("Vob size filter resulted in zero matches. Taking all vobs.");
                files = allVobs;
            }

            // Assuming they're named "vts_05_01", take all files whose second part matches that of the first file
            if (files.Count > 0)
            {
                var parts = _fileSystem.GetFileNameWithoutExtension(files[0]).Split('_');

                if (parts.Length == 3)
                {
                    var title = parts[1];

                    files = files.TakeWhile(f =>
                    {
                        var fileParts = _fileSystem.GetFileNameWithoutExtension(f).Split('_');

                        return fileParts.Length == 3 && string.Equals(title, fileParts[1], StringComparison.OrdinalIgnoreCase);

                    }).ToList();

                    // If this resulted in not getting any vobs, just take them all
                    if (files.Count == 0)
                    {
                        _logger.LogWarning("Vob filename filter resulted in zero matches. Taking all vobs.");
                        files = allVobs;
                    }
                }
            }

            return files.Select(i => i.FullName);
        }

        public bool CanExtractSubtitles(string codec)
        {
            // TODO is there ever a case when a subtitle can't be extracted??
            return true;
        }

        private class ProcessWrapper : IDisposable
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

                try
                {
                    process.Dispose();
                }
                catch
                {
                }
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    if (Process != null)
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
