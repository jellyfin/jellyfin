using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
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
        /// External: path supplied via command line
        /// Custom: coming from UI or config/encoding.xml file
        /// System: FFmpeg found in system $PATH
        /// null: No FFmpeg found
        /// </summary>
        public string EncoderLocationType { get; private set; }

        private readonly ILogger _logger;
        private readonly IJsonSerializer _jsonSerializer;
        private string FFmpegPath { get; set; }
        private string FFprobePath { get; set; }
        protected readonly IServerConfigurationManager ConfigurationManager;
        protected readonly IFileSystem FileSystem;
        protected readonly Func<ISubtitleEncoder> SubtitleEncoder;
        protected readonly Func<IMediaSourceManager> MediaSourceManager;
        private readonly IProcessFactory _processFactory;
        private readonly int DefaultImageExtractionTimeoutMs;
        private readonly string StartupOptionFFmpegPath;
        private readonly string StartupOptionFFprobePath;

        private readonly SemaphoreSlim _thumbnailResourcePool = new SemaphoreSlim(1, 1);
        private readonly List<ProcessWrapper> _runningProcesses = new List<ProcessWrapper>();

        public MediaEncoder(
            ILoggerFactory loggerFactory,
            IJsonSerializer jsonSerializer,
            string startupOptionsFFmpegPath,
            string startupOptionsFFprobePath,
            IServerConfigurationManager configurationManager,
            IFileSystem fileSystem,
            Func<ISubtitleEncoder> subtitleEncoder,
            Func<IMediaSourceManager> mediaSourceManager,
            IProcessFactory processFactory,
            int defaultImageExtractionTimeoutMs)
        {
            _logger = loggerFactory.CreateLogger(nameof(MediaEncoder));
            _jsonSerializer = jsonSerializer;
            StartupOptionFFmpegPath = startupOptionsFFmpegPath;
            StartupOptionFFprobePath = startupOptionsFFprobePath;
            ConfigurationManager = configurationManager;
            FileSystem = fileSystem;
            SubtitleEncoder = subtitleEncoder;
            _processFactory = processFactory;
            DefaultImageExtractionTimeoutMs = defaultImageExtractionTimeoutMs;
        }

        /// <summary>
        /// Run at startup or if the user removes a Custom path from transcode page.
        /// Sets global variables FFmpegPath and EncoderLocationType.
        /// If startup options --ffprobe is given then FFprobePath is set too.
        /// </summary>
        public void Init()
        {
            // 1) If given, use the --ffmpeg CLI switch
            if (ValidatePathFFmpeg("From CLI Switch", StartupOptionFFmpegPath))
            {
                _logger.LogInformation("FFmpeg: Using path from command line switch --ffmpeg");
                EncoderLocationType = "External";
            }

            // 2) Try Custom path stroed in config/encoding xml file under tag <EncoderAppPathCustom>
            else if (ValidatePathFFmpeg("From Config File", ConfigurationManager.GetConfiguration<EncodingOptions>("encoding").EncoderAppPathCustom))
            {
                _logger.LogInformation("FFmpeg: Using path from config/encoding.xml file");
                EncoderLocationType = "Custom";
            }

            // 3) Search system $PATH environment variable for valid FFmpeg
            else if (ValidatePathFFmpeg("From $PATH", ExistsOnSystemPath("ffmpeg")))
            {
                _logger.LogInformation("FFmpeg: Using system $PATH for FFmpeg");
                EncoderLocationType = "System";
            }
            else
            {
                _logger.LogError("FFmpeg: No suitable executable found");
                FFmpegPath = null;
                EncoderLocationType = null;
            }

            // If given, use the --ffprobe CLI switch
            if (ValidatePathFFprobe("CLI Switch", StartupOptionFFprobePath))
            {
                _logger.LogInformation("FFprobe: Using path from command line switch --ffprobe");
            }
            else
            {
                // FFprobe path from command line is no good, so set to null and let ReInit() try
                // and set using the FFmpeg path.
                FFprobePath = null;
            }

            ReInit();
        }

        /// <summary>
        /// Writes the currently used FFmpeg to config/encoding.xml file.
        /// Sets the FFprobe path if not currently set.
        /// Interrogates the FFmpeg tool to identify what encoders/decodres are available.
        /// </summary>
        private void ReInit()
        {
            // Write the FFmpeg path to the config/encoding.xml file so it appears in UI
            var config = ConfigurationManager.GetConfiguration<EncodingOptions>("encoding");
            config.EncoderAppPath = FFmpegPath ?? string.Empty;
            ConfigurationManager.SaveConfiguration("encoding", config);

            // Only if mpeg path is set, try and set path to probe
            if (FFmpegPath != null)
            {
                // Probe would be null here if no valid --ffprobe path was given
                // at startup, or we're performing ReInit following mpeg path update from UI
                if (FFprobePath == null)
                {
                    // Use the mpeg path to create a probe path
                    if (ValidatePathFFprobe("Copied from FFmpeg:", GetProbePathFromEncoderPath(FFmpegPath)))
                    {
                        _logger.LogInformation("FFprobe: Using FFprobe in same folders as FFmpeg");
                    }
                    else
                    {
                        _logger.LogError("FFprobe: No suitable executable found");
                    }
                }

                // Interrogate to understand what coders it supports
                var result = new EncoderValidator(_logger, _processFactory).GetAvailableCoders(FFmpegPath);

                SetAvailableDecoders(result.decoders);
                SetAvailableEncoders(result.encoders);
            }

            // Stamp FFmpeg paths to the log file
            LogPaths();
        }

        /// <summary>
        /// Triggered from the Settings > Trascoding UI page when users sumits Custom FFmpeg path to use.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathType"></param>
        public void UpdateEncoderPath(string path, string pathType)
        {
            _logger.LogInformation("Attempting to update encoder path to {0}. pathType: {1}", path ?? string.Empty, pathType ?? string.Empty);

            if (!string.Equals(pathType, "custom", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Unexpected pathType value");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    // User had cleared the cutom path in UI.  Clear the Custom config
                    // setting and peform full Init to relook any CLI switches and system $PATH
                    var config = ConfigurationManager.GetConfiguration<EncodingOptions>("encoding");
                    config.EncoderAppPathCustom = string.Empty;
                    ConfigurationManager.SaveConfiguration("encoding", config);

                    Init();
                }
                else if (!File.Exists(path) && !Directory.Exists(path))
                {
                    // Given path is neither file or folder
                    throw new ResourceNotFoundException();
                }
                else
                {
                    // Supplied path could be either file path or folder path.
                    // Resolve down to file path and validate
                    path = GetEncoderPath(path);

                    if (path == null)
                    {
                        throw new ResourceNotFoundException("FFmpeg not found");
                    }
                    else if (!ValidatePathFFmpeg("New From UI", path))
                    {
                        throw new ResourceNotFoundException("Failed validation checks.  Version 4.0 or greater is required");
                    }
                    else
                    {
                        EncoderLocationType = "Custom";

                        // Write the validated mpeg path to the xml as <EncoderAppPathCustom>
                        // This ensures its not lost on new startup
                        var config = ConfigurationManager.GetConfiguration<EncodingOptions>("encoding");
                        config.EncoderAppPathCustom = FFmpegPath;
                        ConfigurationManager.SaveConfiguration("encoding", config);

                        FFprobePath = null; // Clear probe path so it gets relooked in ReInit()

                        ReInit();
                    }
                }
            }
        }

        private bool ValidatePath(string type, string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                {
                    var valid = new EncoderValidator(_logger, _processFactory).ValidateVersion(path, true);

                    if (valid == true)
                    {
                        return true;
                    }
                    else
                    {
                        _logger.LogError("{0}: Failed validation checks.  Version 4.0 or greater is required: {1}", type, path);
                    }
                }
                else
                {
                    _logger.LogError("{0}: File not found: {1}", type, path);
                }
            }

            return false;
        }

        private bool ValidatePathFFmpeg(string comment, string path)
        {
            if (ValidatePath("FFmpeg: " + comment, path) == true)
            {
                FFmpegPath = path;
                return true;
            }

            return false;
        }

        private bool ValidatePathFFprobe(string comment, string path)
        {
            if (ValidatePath("FFprobe: " + comment, path) == true)
            {
                FFprobePath = path;
                return true;
            }

            return false;
        }

        private string GetEncoderPath(string path)
        {
            if (Directory.Exists(path))
            {
                return GetEncoderPathFromDirectory(path);
            }

            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }

        private string GetEncoderPathFromDirectory(string path)
        {
            try
            {
                var files = FileSystem.GetFilePaths(path);

                var excludeExtensions = new[] { ".c" };

                return files.FirstOrDefault(i => string.Equals(Path.GetFileNameWithoutExtension(i), "ffmpeg", StringComparison.OrdinalIgnoreCase) && !excludeExtensions.Contains(Path.GetExtension(i) ?? string.Empty));
            }
            catch (Exception)
            {
                // Trap all exceptions, like DirNotExists, and return null
                return null;
            }
        }

        private string GetProbePathFromEncoderPath(string appPath)
        {
            return FileSystem.GetFilePaths(Path.GetDirectoryName(appPath))
                .FirstOrDefault(i => string.Equals(Path.GetFileNameWithoutExtension(i), "ffprobe", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Search the system $PATH environment variable looking for given filename.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string ExistsOnSystemPath(string fileName)
        {
            var values = Environment.GetEnvironmentVariable("PATH");

            foreach (var path in values.Split(Path.PathSeparator))
            {
                var candidatePath = GetEncoderPathFromDirectory(path);

                if (ValidatePath("Found on PATH", candidatePath))
                {
                    return candidatePath;
                }
            }
            return null;
        }

        private void LogPaths()
        {
            _logger.LogInformation("FFMpeg: {0}", FFmpegPath ?? "not found");
            _logger.LogInformation("FFProbe: {0}", FFprobePath ?? "not found");
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
        public string GetInputArgument(string[] inputFiles, MediaProtocol protocol)
        {
            return EncodingUtils.GetInputArgument(inputFiles.ToList(), protocol);
        }

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
                ? "{0} -i {1} -threads 0 -v info -print_format json -show_streams -show_chapters -show_format"
                : "{0} -i {1} -threads 0 -v info -print_format json -show_streams -show_format";

            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                // Must consume both or ffmpeg may hang due to deadlocks. See comments below.
                RedirectStandardOutput = true,
                FileName = FFprobePath,
                Arguments = string.Format(args, probeSizeArgument, inputPath).Trim(),

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
                StartProcess(processWrapper);

                try
                {
                    //process.BeginErrorReadLine();

                    var result = await _jsonSerializer.DeserializeFromStreamAsync<InternalMediaInfoResult>(process.StandardOutput.BaseStream).ConfigureAwait(false);

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

                    return new ProbeResultNormalizer(_logger, FileSystem).GetMediaInfo(result, videoType, isAudio, primaryPath, protocol);
                }
                catch
                {
                    StopProcess(processWrapper, 100);

                    throw;
                }
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
