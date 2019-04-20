using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Encoder
{
    /// <summary>
    /// Class MediaEncoder
    /// </summary>
    public class MediaEncoder : IMediaEncoder, IDisposable
    {
        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        public string EncoderPath => FFmpegPath;

        /// <summary>
        /// The location of the discovered FFmpeg tool.
        /// </summary>
        public FFmpegLocation EncoderLocation { get; private set; }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private string FFmpegPath;
        private string FFprobePath;
        protected readonly IServerConfigurationManager ConfigurationManager;
        protected readonly IFileSystem FileSystem;
        protected readonly Func<ISubtitleEncoder> SubtitleEncoder;
        protected readonly Func<IMediaSourceManager> MediaSourceManager;
        private readonly IProcessFactory _processFactory;
        private readonly int DefaultImageExtractionTimeoutMs;
        private readonly string StartupOptionFFmpegPath;

        private readonly SemaphoreSlim _thumbnailResourcePool = new SemaphoreSlim(1, 1);
        private readonly List<ProcessWrapper> _runningProcesses = new List<ProcessWrapper>();
        private readonly ILocalizationManager _localization;

        public MediaEncoder(
            ILoggerFactory loggerFactory,
            IJsonSerializer jsonSerializer,
            string startupOptionsFFmpegPath,
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            Func<ISubtitleEncoder> subtitleEncoder,
            Func<IMediaSourceManager> mediaSourceManager,
            IProcessFactory processFactory,
            int defaultImageExtractionTimeoutMs,
            ILocalizationManager localization)
        {
            _logger = loggerFactory.CreateLogger(nameof(MediaEncoder));
            _jsonSerializer = jsonSerializer;
            StartupOptionFFmpegPath = startupOptionsFFmpegPath;
            ConfigurationManager = configurationManager;
            FileSystem = fileSystem;
            SubtitleEncoder = subtitleEncoder;
            _processFactory = processFactory;
            DefaultImageExtractionTimeoutMs = defaultImageExtractionTimeoutMs;
            _localization = localization;
        }

        /// <summary>
        /// Run at startup or if the user removes a Custom path from transcode page.
        /// Sets global variables FFmpegPath.
        /// Precedence is: Config > CLI > $PATH
        /// </summary>
        public void SetFFmpegPath()
        {
            // 1) Custom path stored in config/encoding xml file under tag <EncoderAppPath> takes precedence
            if (!ValidatePath(ConfigurationManager.GetConfiguration<EncodingOptions>("encoding").EncoderAppPath, FFmpegLocation.Custom))
            {
                // 2) Check if the --ffmpeg CLI switch has been given
                if (!ValidatePath(StartupOptionFFmpegPath, FFmpegLocation.SetByArgument))
                {
                    // 3) Search system $PATH environment variable for valid FFmpeg
                    if (!ValidatePath(ExistsOnSystemPath("ffmpeg"), FFmpegLocation.System))
                    {
                        EncoderLocation = FFmpegLocation.NotFound;
                        FFmpegPath = null;
                    }
                }
            }

            // Write the FFmpeg path to the config/encoding.xml file as <EncoderAppPathDisplay> so it appears in UI
            var config = ConfigurationManager.GetConfiguration<EncodingOptions>("encoding");
            config.EncoderAppPathDisplay = FFmpegPath ?? string.Empty;
            ConfigurationManager.SaveConfiguration("encoding", config);

            // Only if mpeg path is set, try and set path to probe
            if (FFmpegPath != null)
            {
                // Determine a probe path from the mpeg path
                FFprobePath = Regex.Replace(FFmpegPath, @"[^\/\\]+?(\.[^\/\\\n.]+)?$", @"ffprobe$1");

                // Interrogate to understand what coders are supported
                var result = new EncoderValidator(_logger, _processFactory).GetAvailableCoders(FFmpegPath);

                SetAvailableDecoders(result.decoders);
                SetAvailableEncoders(result.encoders);
            }

            _logger.LogInformation("FFmpeg: {0}: {1}", EncoderLocation.ToString(), FFmpegPath ?? string.Empty);
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

            _logger.LogInformation("Attempting to update encoder path to {0}. pathType: {1}", path ?? string.Empty, pathType ?? string.Empty);

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
            var config = ConfigurationManager.GetConfiguration<EncodingOptions>("encoding");
            config.EncoderAppPath = newPath;
            ConfigurationManager.SaveConfiguration("encoding", config);

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
                    rc = new EncoderValidator(_logger, _processFactory).ValidateVersion(path, true);

                    if (!rc)
                    {
                        _logger.LogWarning("FFmpeg: {0}: Failed version check: {1}", location.ToString(), path);
                    }

                    // ToDo - Enable the ffmpeg validator.  At the moment any version can be used.
                    rc = true;

                    FFmpegPath = path;
                    EncoderLocation = location;
                }
                else
                {
                    _logger.LogWarning("FFmpeg: {0}: File not found: {1}", location.ToString(), path);
                }
            }

            return rc;
        }

        private string GetEncoderPathFromDirectory(string path, string filename)
        {
            try
            {
                var files = FileSystem.GetFilePaths(path);

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
        private string ExistsOnSystemPath(string filename)
        {
            string inJellyfinPath = GetEncoderPathFromDirectory(System.AppContext.BaseDirectory, filename);
            if (!string.IsNullOrEmpty(inJellyfinPath))
            {
                return inJellyfinPath;
            }
            var values = Environment.GetEnvironmentVariable("PATH");

            foreach (var path in values.Split(Path.PathSeparator))
            {
                var candidatePath = GetEncoderPathFromDirectory(path, filename);

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

            var inputFiles = MediaEncoderHelpers.GetInputArgument(FileSystem, request.MediaSource.Path, request.MediaSource.Protocol, request.MountedIso, request.PlayableStreamFileNames);

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

            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
                RedirectStandardOutput = true,

                FileName = FFprobePath,
                Arguments = args,


                IsHidden = true,
                ErrorDialog = false,
                EnableRaisingEvents = true
            });

            if (forceEnableLogging)
            {
                _logger.LogInformation("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            }
            else
            {
                _logger.LogDebug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);
            }

            using (var processWrapper = new ProcessWrapper(process, this, _logger))
            {
                _logger.LogDebug("Starting ffprobe with args {Args}", args);
                StartProcess(processWrapper);

                InternalMediaInfoResult result;
                try
                {
                    result = await _jsonSerializer.DeserializeFromStreamAsync<InternalMediaInfoResult>(
                                        process.StandardOutput.BaseStream).ConfigureAwait(false);
                }
                catch
                {
                    StopProcess(processWrapper, 100);

                    throw;
                }

                if (result == null || (result.streams == null && result.format == null))
                {
                    throw new Exception("ffprobe failed - streams and format are both null.");
                }

                if (result.streams != null)
                {
                    // Normalize aspect ratio if invalid
                    foreach (var stream in result.streams)
                    {
                        if (string.Equals(stream.display_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.display_aspect_ratio = string.Empty;
                        }

                        if (string.Equals(stream.sample_aspect_ratio, "0:1", StringComparison.OrdinalIgnoreCase))
                        {
                            stream.sample_aspect_ratio = string.Empty;
                        }
                    }
                }

                return new ProbeResultNormalizer(_logger, FileSystem, _localization).GetMediaInfo(result, videoType, isAudio, primaryPath, protocol);
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
                    _logger.LogError(ex, "I-frame image extraction failed, will attempt standard way. Input: {arguments}", inputArgument);
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

            var tempExtractPath = Path.Combine(ConfigurationManager.ApplicationPaths.TempDirectory, Guid.NewGuid() + ".jpg");
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

            var encodinghelper = new EncodingHelper(this, FileSystem, SubtitleEncoder());
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
                var inputFormat = encodinghelper.GetInputFormat(container);
                if (!string.IsNullOrWhiteSpace(inputFormat))
                {
                    args = "-f " + inputFormat + " " + args;
                }
            }

            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = FFmpegPath,
                Arguments = args,
                IsHidden = true,
                ErrorDialog = false,
                EnableRaisingEvents = true
            });

            _logger.LogDebug("{0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            using (var processWrapper = new ProcessWrapper(process, this, _logger))
            {
                bool ranToCompletion;

                StartProcess(processWrapper);

                var timeoutMs = ConfigurationManager.Configuration.ImageExtractionTimeoutMs;
                if (timeoutMs <= 0)
                {
                    timeoutMs = DefaultImageExtractionTimeoutMs;
                }

                ranToCompletion = await process.WaitForExitAsync(timeoutMs).ConfigureAwait(false);

                if (!ranToCompletion)
                {
                    StopProcess(processWrapper, 1000);
                }

                var exitCode = ranToCompletion ? processWrapper.ExitCode ?? 0 : -1;
                var file = FileSystem.GetFileInfo(tempExtractPath);

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

        public async Task ExtractVideoImagesOnInterval(string[] inputFiles,
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
            var resourcePool = _thumbnailResourcePool;

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

            var encodinghelper = new EncodingHelper(this, FileSystem, SubtitleEncoder());
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
                var inputFormat = encodinghelper.GetInputFormat(container);
                if (!string.IsNullOrWhiteSpace(inputFormat))
                {
                    args = "-f " + inputFormat + " " + args;
                }
            }

            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = FFmpegPath,
                Arguments = args,
                IsHidden = true,
                ErrorDialog = false,
                EnableRaisingEvents = true
            });

            _logger.LogInformation(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            await resourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            bool ranToCompletion = false;

            using (var processWrapper = new ProcessWrapper(process, this, _logger))
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
                        if (await process.WaitForExitAsync(30000).ConfigureAwait(false))
                        {
                            ranToCompletion = true;
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        var jpegCount = FileSystem.GetFilePaths(targetDirectory)
                            .Count(i => string.Equals(Path.GetExtension(i), ".jpg", StringComparison.OrdinalIgnoreCase));

                        isResponsive = (jpegCount > lastCount);
                        lastCount = jpegCount;
                    }

                    if (!ranToCompletion)
                    {
                        StopProcess(processWrapper, 1000);
                    }
                }
                finally
                {
                    resourcePool.Release();
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

            lock (_runningProcesses)
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WaitForExit");
            }

            try
            {
                _logger.LogInformation("Killing ffmpeg process");

                process.Process.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error killing process");
            }
        }

        private void StopProcesses()
        {
            List<ProcessWrapper> proceses;
            lock (_runningProcesses)
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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

        public Task ConvertImage(string inputPath, string outputPath)
        {
            throw new NotImplementedException();
        }

        public string[] GetPlayableStreamFileNames(string path, VideoType videoType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetPrimaryPlaylistVobFiles(string path, IIsoMount isoMount, uint? titleNumber)
        {
            throw new NotImplementedException();
        }

        public bool CanExtractSubtitles(string codec)
        {
            // TODO is there ever a case when a subtitle can't be extracted??
            return true;
        }

        private class ProcessWrapper : IDisposable
        {
            public readonly IProcess Process;
            public bool HasExited;
            public int? ExitCode;
            private readonly MediaEncoder _mediaEncoder;
            private readonly ILogger _logger;

            public ProcessWrapper(IProcess process, MediaEncoder mediaEncoder, ILogger logger)
            {
                Process = process;
                _mediaEncoder = mediaEncoder;
                _logger = logger;
                Process.Exited += Process_Exited;
            }

            void Process_Exited(object sender, EventArgs e)
            {
                var process = (IProcess)sender;

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

            private void DisposeProcess(IProcess process)
            {
                lock (_mediaEncoder._runningProcesses)
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

            private bool _disposed;
            private readonly object _syncLock = new object();
            public void Dispose()
            {
                lock (_syncLock)
                {
                    if (!_disposed)
                    {
                        if (Process != null)
                        {
                            Process.Exited -= Process_Exited;
                            DisposeProcess(Process);
                        }
                    }

                    _disposed = true;
                }
            }
        }
    }
}
