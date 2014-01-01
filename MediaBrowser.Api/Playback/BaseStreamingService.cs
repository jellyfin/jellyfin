using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaInfo;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class BaseStreamingService
    /// </summary>
    public abstract class BaseStreamingService : BaseApiService
    {
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected IServerConfigurationManager ServerConfigurationManager { get; private set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        protected IUserManager UserManager { get; private set; }

        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        protected ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Gets or sets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        protected IIsoManager IsoManager { get; private set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        protected IMediaEncoder MediaEncoder { get; private set; }
        protected IDtoService DtoService { get; private set; }

        protected IFileSystem FileSystem { get; private set; }

        protected IItemRepository ItemRepository { get; private set; }
        protected ILiveTvManager LiveTvManager { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        /// <param name="serverConfig">The server configuration.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        /// <param name="dtoService">The dto service.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="itemRepository">The item repository.</param>
        protected BaseStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService, IFileSystem fileSystem, IItemRepository itemRepository, ILiveTvManager liveTvManager)
        {
            LiveTvManager = liveTvManager;
            ItemRepository = itemRepository;
            FileSystem = fileSystem;
            DtoService = dtoService;
            ServerConfigurationManager = serverConfig;
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
            MediaEncoder = mediaEncoder;
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="performSubtitleConversions">if set to <c>true</c> [perform subtitle conversions].</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, StreamState state, bool performSubtitleConversions);

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected abstract TranscodingJobType TranscodingJobType { get; }

        /// <summary>
        /// Gets the output file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetOutputFileExtension(StreamState state)
        {
            return Path.GetExtension(state.RequestedUrl);
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetOutputFilePath(StreamState state)
        {
            var folder = ServerConfigurationManager.ApplicationPaths.EncodedMediaCachePath;

            var outputFileExtension = GetOutputFileExtension(state);

            return Path.Combine(folder, GetCommandLineArguments("dummy\\dummy", state, false).GetMD5() + (outputFileExtension ?? string.Empty).ToLower());
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// The fast seek offset seconds
        /// </summary>
        private const int FastSeekOffsetSeconds = 1;

        /// <summary>
        /// Gets the fast seek command line parameter.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        /// <value>The fast seek command line parameter.</value>
        protected string GetFastSeekCommandLineParameter(StreamRequest request)
        {
            var time = request.StartTimeTicks;

            if (time.HasValue)
            {
                var seconds = TimeSpan.FromTicks(time.Value).TotalSeconds - FastSeekOffsetSeconds;

                if (seconds > 0)
                {
                    return string.Format("-ss {0}", seconds.ToString(UsCulture));
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the slow seek command line parameter.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        /// <value>The slow seek command line parameter.</value>
        protected string GetSlowSeekCommandLineParameter(StreamRequest request)
        {
            var time = request.StartTimeTicks;

            if (time.HasValue)
            {
                if (TimeSpan.FromTicks(time.Value).TotalSeconds - FastSeekOffsetSeconds > 0)
                {
                    return string.Format(" -ss {0}", FastSeekOffsetSeconds.ToString(UsCulture));
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetMapArgs(StreamState state)
        {
            var args = string.Empty;

            if (state.IsRemote || !state.HasMediaStreams)
            {
                return string.Empty;
            }

            if (state.VideoStream != null)
            {
                args += string.Format("-map 0:{0}", state.VideoStream.Index);
            }
            else
            {
                args += "-map -0:v";
            }

            if (state.AudioStream != null)
            {
                args += string.Format(" -map 0:{0}", state.AudioStream.Index);
            }

            else
            {
                args += " -map -0:a";
            }

            if (state.SubtitleStream == null)
            {
                args += " -map -0:s";
            }

            return args;
        }

        /// <summary>
        /// Determines which stream will be used for playback
        /// </summary>
        /// <param name="allStream">All stream.</param>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(IEnumerable<MediaStream> allStream, int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
        {
            var streams = allStream.Where(s => s.Type == type).OrderBy(i => i.Index).ToList();

            if (desiredIndex.HasValue)
            {
                var stream = streams.FirstOrDefault(s => s.Index == desiredIndex.Value);

                if (stream != null)
                {
                    return stream;
                }
            }

            if (returnFirstIfNoIndex && type == MediaStreamType.Audio)
            {
                return streams.FirstOrDefault(i => i.Channels.HasValue && i.Channels.Value > 0) ??
                       streams.FirstOrDefault();
            }

            // Just return the first one
            return returnFirstIfNoIndex ? streams.FirstOrDefault() : null;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">Unrecognized EncodingQuality value.</exception>
        protected int GetNumberOfThreads()
        {
            var quality = ServerConfigurationManager.Configuration.EncodingQuality;

            switch (quality)
            {
                case EncodingQuality.Auto:
                    return 0;
                case EncodingQuality.HighSpeed:
                    return 2;
                case EncodingQuality.HighQuality:
                    return 2;
                case EncodingQuality.MaxQuality:
                    return 0;
                default:
                    throw new Exception("Unrecognized EncodingQuality value.");
            }
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetVideoQualityParam(StreamState state, string videoCodec)
        {
            var args = string.Empty;

            // webm
            if (videoCodec.Equals("libvpx", StringComparison.OrdinalIgnoreCase))
            {
                args = "-speed 16 -quality good -profile:v 0 -slices 8";
            }

            // asf/wmv
            else if (videoCodec.Equals("wmv2", StringComparison.OrdinalIgnoreCase))
            {
                args = "-g 100 -qmax 15";
            }

            else if (videoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                args = "-preset superfast";
            }
            else if (videoCodec.Equals("mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                args = "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }

            return args.Trim();
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <param name="performTextSubtitleConversion">if set to <c>true</c> [perform text subtitle conversion].</param>
        /// <returns>System.String.</returns>
        protected string GetOutputSizeParam(StreamState state, string outputVideoCodec, bool performTextSubtitleConversion)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var assSubtitleParam = string.Empty;

            var request = state.VideoRequest;

            if (state.SubtitleStream != null)
            {
                if (state.SubtitleStream.Codec.IndexOf("srt", StringComparison.OrdinalIgnoreCase) != -1 ||
                    state.SubtitleStream.Codec.IndexOf("subrip", StringComparison.OrdinalIgnoreCase) != -1 ||
                    string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase))
                {
                    assSubtitleParam = GetTextSubtitleParam(state, request.StartTimeTicks, performTextSubtitleConversion);
                }
            }

            // If fixed dimensions were supplied
            if (request.Width.HasValue && request.Height.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);
                var heightParam = request.Height.Value.ToString(UsCulture);

                return string.Format(" -vf \"scale=trunc({0}/2)*2:trunc({1}/2)*2{2}\"", widthParam, heightParam, assSubtitleParam);
            }

            var isH264Output = outputVideoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase);

            // If a fixed width was requested
            if (request.Width.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format(" -vf \"scale={0}:trunc(ow/a/2)*2{1}\"", widthParam, assSubtitleParam) :
                    string.Format(" -vf \"scale={0}:-1{1}\"", widthParam, assSubtitleParam);
            }

            // If a fixed height was requested
            if (request.Height.HasValue)
            {
                var heightParam = request.Height.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format(" -vf \"scale=trunc(oh*a*2)/2:{0}{1}\"", heightParam, assSubtitleParam) :
                    string.Format(" -vf \"scale=-1:{0}{1}\"", heightParam, assSubtitleParam);
            }

            // If a max width was requested
            if (request.MaxWidth.HasValue && (!request.MaxHeight.HasValue || state.VideoStream == null))
            {
                var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format(" -vf \"scale=min(iw\\,{0}):trunc(ow/a/2)*2{1}\"", maxWidthParam, assSubtitleParam) :
                    string.Format(" -vf \"scale=min(iw\\,{0}):-1{1}\"", maxWidthParam, assSubtitleParam);
            }

            // If a max height was requested
            if (request.MaxHeight.HasValue && (!request.MaxWidth.HasValue || state.VideoStream == null))
            {
                var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format(" -vf \"scale=trunc(oh*a*2)/2:min(ih\\,{0}){1}\"", maxHeightParam, assSubtitleParam) :
                    string.Format(" -vf \"scale=-1:min(ih\\,{0}){1}\"", maxHeightParam, assSubtitleParam);
            }

            if (state.VideoStream == null)
            {
                // No way to figure this out
                return string.Empty;
            }

            // Need to perform calculations manually

            // Try to account for bad media info
            var currentHeight = state.VideoStream.Height ?? request.MaxHeight ?? request.Height ?? 0;
            var currentWidth = state.VideoStream.Width ?? request.MaxWidth ?? request.Width ?? 0;

            var outputSize = DrawingUtils.Resize(currentWidth, currentHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

            // If we're encoding with libx264, it can't handle odd numbered widths or heights, so we'll have to fix that
            if (isH264Output)
            {
                var widthParam = outputSize.Width.ToString(UsCulture);
                var heightParam = outputSize.Height.ToString(UsCulture);

                return string.Format(" -vf \"scale=trunc({0}/2)*2:trunc({1}/2)*2{2}\"", widthParam, heightParam, assSubtitleParam);
            }

            // Otherwise use -vf scale since ffmpeg will ensure internally that the aspect ratio is preserved
            return string.Format(" -vf \"scale={0}:-1{1}\"", Convert.ToInt32(outputSize.Width), assSubtitleParam);
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(StreamState state, long? startTimeTicks, bool performConversion)
        {
            var path = state.SubtitleStream.IsExternal ? GetConvertedAssPath(state.MediaPath, state.SubtitleStream, startTimeTicks, performConversion) :
                GetExtractedAssPath(state, startTimeTicks, performConversion);

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return string.Format(",ass='{0}'", path.Replace('\\', '/').Replace(":/", "\\:/"));
        }

        /// <summary>
        /// Gets the extracted ass path.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        private string GetExtractedAssPath(StreamState state, long? startTimeTicks, bool performConversion)
        {
            var offset = TimeSpan.FromTicks(startTimeTicks ?? 0);

            var path = FFMpegManager.Instance.GetSubtitleCachePath(state.MediaPath, state.SubtitleStream, offset, ".ass");

            if (performConversion)
            {
                InputType type;

                var inputPath = MediaEncoderHelpers.GetInputArgument(state.MediaPath, state.IsRemote, state.VideoType, state.IsoType, null, state.PlayableStreamFileNames, out type);

                try
                {
                    var parentPath = Path.GetDirectoryName(path);

                    Directory.CreateDirectory(parentPath);

                    var task = MediaEncoder.ExtractTextSubtitle(inputPath, type, state.SubtitleStream.Index, offset, path, CancellationToken.None);

                    Task.WaitAll(task);
                }
                catch
                {
                    return null;
                }
            }

            return path;
        }

        /// <summary>
        /// Gets the converted ass path.
        /// </summary>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        private string GetConvertedAssPath(string mediaPath, MediaStream subtitleStream, long? startTimeTicks, bool performConversion)
        {
            var offset = TimeSpan.FromTicks(startTimeTicks ?? 0);

            var path = FFMpegManager.Instance.GetSubtitleCachePath(mediaPath, subtitleStream, offset, ".ass");

            if (performConversion)
            {
                try
                {
                    var parentPath = Path.GetDirectoryName(path);

                    Directory.CreateDirectory(parentPath);

                    var task = MediaEncoder.ConvertTextSubtitleToAss(subtitleStream.Path, path, subtitleStream.Language, offset, CancellationToken.None);

                    Task.WaitAll(task);
                }
                catch
                {
                    return null;
                }
            }

            return path;
        }

        /// <summary>
        /// Gets the internal graphical subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetInternalGraphicalSubtitleParam(StreamState state, string outputVideoCodec)
        {
            var outputSizeParam = string.Empty;

            var request = state.VideoRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, outputVideoCodec, false).TrimEnd('"');
                outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
            }

            return string.Format(" -filter_complex \"[0:{0}]format=yuva444p,lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:{1}] [sub] overlay{2}\"", state.SubtitleStream.Index, state.VideoStream.Index, outputSizeParam);
        }

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="isVideo">if set to <c>true</c> [is video].</param>
        /// <param name="videoType">Type of the video.</param>
        /// <param name="isoType">Type of the iso.</param>
        /// <returns>System.String.</returns>
        protected string GetProbeSizeArgument(string mediaPath, bool isVideo, VideoType? videoType, IsoType? isoType)
        {
            var type = !isVideo ? MediaEncoderHelpers.GetInputType(mediaPath, null, null) :
                MediaEncoderHelpers.GetInputType(mediaPath, videoType, isoType);

            return MediaEncoder.GetProbeSizeArgument(type);
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetNumAudioChannelsParam(StreamRequest request, MediaStream audioStream)
        {
            if (audioStream != null)
            {
                if (audioStream.Channels > 2 && request.AudioCodec.HasValue)
                {
                    if (request.AudioCodec.Value == AudioCodecs.Wma)
                    {
                        // wmav2 currently only supports two channel output
                        return 2;
                    }
                }
            }

            return request.AudioChannels;
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        protected bool IsH264(MediaStream stream)
        {
            return stream.Codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   stream.Codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// Gets the name of the output audio codec
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        protected string GetAudioCodec(StreamRequest request)
        {
            var codec = request.AudioCodec;

            if (codec.HasValue)
            {
                if (codec == AudioCodecs.Aac)
                {
                    return "aac -strict experimental";
                }
                if (codec == AudioCodecs.Mp3)
                {
                    return "libmp3lame";
                }
                if (codec == AudioCodecs.Vorbis)
                {
                    return "libvorbis";
                }
                if (codec == AudioCodecs.Wma)
                {
                    return "wmav2";
                }

                return codec.ToString().ToLower();
            }

            return "copy";
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        protected string GetVideoCodec(VideoStreamRequest request)
        {
            var codec = request.VideoCodec;

            if (codec.HasValue)
            {
                if (codec == VideoCodecs.H264)
                {
                    return "libx264";
                }
                if (codec == VideoCodecs.Vpx)
                {
                    return "libvpx";
                }
                if (codec == VideoCodecs.Wmv)
                {
                    return "wmv2";
                }
                if (codec == VideoCodecs.Theora)
                {
                    return "libtheora";
                }

                return codec.ToString().ToLower();
            }

            return "copy";
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetInputArgument(StreamState state)
        {
            var type = InputType.AudioFile;

            var inputPath = new[] { state.MediaPath };

            if (state.IsInputVideo)
            {
                if (!(state.VideoType == VideoType.Iso && state.IsoMount == null))
                {
                    inputPath = MediaEncoderHelpers.GetInputArgument(state.MediaPath, state.IsRemote, state.VideoType, state.IsoType, state.IsoMount, state.PlayableStreamFileNames, out type);
                }
            }

            return MediaEncoder.GetInputArgument(inputPath, type);
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns>Task.</returns>
        protected async Task StartFfMpeg(StreamState state, string outputPath)
        {
            var parentPath = Path.GetDirectoryName(outputPath);

            Directory.CreateDirectory(parentPath);

            if (state.IsInputVideo && state.VideoType == VideoType.Iso && state.IsoType.HasValue && IsoManager.CanMount(state.MediaPath))
            {
                state.IsoMount = await IsoManager.Mount(state.MediaPath, CancellationToken.None).ConfigureAwait(false);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    FileName = MediaEncoder.EncoderPath,
                    WorkingDirectory = Path.GetDirectoryName(MediaEncoder.EncoderPath),
                    Arguments = GetCommandLineArguments(outputPath, state, true),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath, TranscodingJobType, process, state.IsInputVideo, state.Request.StartTimeTicks, state.MediaPath, state.Request.DeviceId);

            Logger.Info(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            var logFilePath = Path.Combine(ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, "ffmpeg-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            state.LogFileStream = FileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, state);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting ffmpeg", ex);

                ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType);

                state.LogFileStream.Dispose();

                throw;
            }

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginOutputReadLine();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            process.StandardError.BaseStream.CopyToAsync(state.LogFileStream);

            // Wait for the file to exist before proceeeding
            while (!File.Exists(outputPath))
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            // Allow a small amount of time to buffer a little
            if (state.IsInputVideo)
            {
                await Task.Delay(500).ConfigureAwait(false);
            }

            // This is arbitrary, but add a little buffer time when internet streaming
            if (state.IsRemote)
            {
                await Task.Delay(4000).ConfigureAwait(false);
            }
        }

        protected int? GetVideoBitrateParam(StreamState state)
        {
            return state.VideoRequest.VideoBitRate;
        }

        protected int? GetAudioBitrateParam(StreamState state)
        {
            if (state.Request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = state.AudioStream == null ? state.Request.AudioBitRate.Value : state.AudioStream.BitRate ?? state.Request.AudioBitRate.Value;

                return Math.Min(currentBitrate, state.Request.AudioBitRate.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets the user agent param.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        protected string GetUserAgentParam(string path)
        {
            var useragent = GetUserAgent(path);

            if (!string.IsNullOrEmpty(useragent))
            {
                return "-user-agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the user agent.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        protected string GetUserAgent(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");

            }
            if (path.IndexOf("apple.com", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return "QuickTime/7.7.4";
            }

            return string.Empty;
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="state">The state.</param>
        protected void OnFfMpegProcessExited(Process process, StreamState state)
        {
            if (state.IsoMount != null)
            {
                state.IsoMount.Dispose();
                state.IsoMount = null;
            }

            var outputFilePath = GetOutputFilePath(state);

            state.LogFileStream.Dispose();

            try
            {
                Logger.Info("FFMpeg exited with code {0} for {1}", process.ExitCode, outputFilePath);
            }
            catch
            {
                Logger.Info("FFMpeg exited with an error for {0}", outputFilePath);
            }
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>StreamState.</returns>
        protected async Task<StreamState> GetState(StreamRequest request, CancellationToken cancellationToken)
        {
            var url = Request.PathInfo;

            if (!request.AudioCodec.HasValue)
            {
                request.AudioCodec = InferAudioCodec(url);
            }

            var state = new StreamState
            {
                Request = request,
                RequestedUrl = url
            };

            Guid itemId;

            if (string.Equals(request.Type, "Recording", StringComparison.OrdinalIgnoreCase))
            {
                var recording = await LiveTvManager.GetInternalRecording(request.Id, cancellationToken).ConfigureAwait(false);

                state.VideoType = VideoType.VideoFile;
                state.IsInputVideo = string.Equals(recording.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
                state.PlayableStreamFileNames = new List<string>();

                if (!string.IsNullOrEmpty(recording.RecordingInfo.Path) && File.Exists(recording.RecordingInfo.Path))
                {
                    state.MediaPath = recording.RecordingInfo.Path;
                    state.IsRemote = false;
                }
                else if (!string.IsNullOrEmpty(recording.RecordingInfo.Url))
                {
                    state.MediaPath = recording.RecordingInfo.Url;
                    state.IsRemote = true;
                }
                else
                {
                    state.MediaPath = string.Format("http://localhost:{0}/mediabrowser/LiveTv/Recordings/{1}/Stream",
                        ServerConfigurationManager.Configuration.HttpServerPortNumber,
                        request.Id);

                    state.IsRemote = true;
                }

                itemId = recording.Id;
            }
            else if (string.Equals(request.Type, "Channel", StringComparison.OrdinalIgnoreCase))
            {
                var channel =  LiveTvManager.GetInternalChannel(request.Id);

                state.VideoType = VideoType.VideoFile;
                state.IsInputVideo = string.Equals(channel.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
                state.PlayableStreamFileNames = new List<string>();

                state.MediaPath = string.Format("http://localhost:{0}/mediabrowser/LiveTv/Channels/{1}/Stream",
                    ServerConfigurationManager.Configuration.HttpServerPortNumber,
                    request.Id);

                state.IsRemote = true;

                itemId = channel.Id;
            }
            else
            {
                var item = DtoService.GetItemByDtoId(request.Id);

                state.MediaPath = item.Path;
                state.IsRemote = item.LocationType == LocationType.Remote;

                var video = item as Video;

                if (video != null)
                {
                    state.IsInputVideo = true;
                    state.VideoType = video.VideoType;
                    state.IsoType = video.IsoType;

                    state.PlayableStreamFileNames = video.PlayableStreamFileNames == null
                        ? new List<string>()
                        : video.PlayableStreamFileNames.ToList();
                }

                itemId = item.Id;
            }

            var videoRequest = request as VideoStreamRequest;

            var mediaStreams = ItemRepository.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = itemId

            }).ToList();

            if (videoRequest != null)
            {
                if (!videoRequest.VideoCodec.HasValue)
                {
                    videoRequest.VideoCodec = InferVideoCodec(url);
                }

                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.HasMediaStreams = mediaStreams.Count > 0;

            return state;
        }

        /// <summary>
        /// Infers the audio codec based on the url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{AudioCodecs}.</returns>
        private AudioCodecs? InferAudioCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Mp3;
            }
            if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Aac;
            }
            if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Wma;
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Vorbis;
            }
            if (string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Vorbis;
            }
            if (string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Vorbis;
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Vorbis;
            }
            if (string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return AudioCodecs.Vorbis;
            }

            return null;
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        private VideoCodecs? InferVideoCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                return VideoCodecs.Wmv;
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return VideoCodecs.Vpx;
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return VideoCodecs.Theora;
            }
            if (string.Equals(ext, ".m3u8", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase))
            {
                return VideoCodecs.H264;
            }

            return VideoCodecs.Copy;
        }
    }
}
