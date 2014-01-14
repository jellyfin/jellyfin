using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
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
using MediaBrowser.Model.LiveTv;
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
            var folder = ServerConfigurationManager.ApplicationPaths.TranscodingTempPath;

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

        protected EncodingQuality GetQualitySetting()
        {
            var quality = ServerConfigurationManager.Configuration.MediaEncodingQuality;

            if (quality == EncodingQuality.Auto)
            {
                var cpuCount = Environment.ProcessorCount;

                if (cpuCount >= 4)
                {
                    return EncodingQuality.HighQuality;
                }

                return EncodingQuality.HighSpeed;
            }

            return quality;
        }

        /// <summary>
        /// Gets the number of threads.
        /// </summary>
        /// <returns>System.Int32.</returns>
        /// <exception cref="System.Exception">Unrecognized MediaEncodingQuality value.</exception>
        protected int GetNumberOfThreads(bool isWebm)
        {
            // Webm: http://www.webmproject.org/docs/encoder-parameters/
            // The decoder will usually automatically use an appropriate number of threads according to how many cores are available but it can only use multiple threads 
            // for the coefficient data if the encoder selected --token-parts > 0 at encode time.

            switch (GetQualitySetting())
            {
                case EncodingQuality.HighSpeed:
                    return 2;
                case EncodingQuality.HighQuality:
                    return isWebm ? Math.Min(3, Environment.ProcessorCount - 1) : 2;
                case EncodingQuality.MaxQuality:
                    return isWebm ? Math.Max(2, Environment.ProcessorCount - 1) : 0;
                default:
                    throw new Exception("Unrecognized MediaEncodingQuality value.");
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
            // webm
            if (videoCodec.Equals("libvpx", StringComparison.OrdinalIgnoreCase))
            {
                // http://www.webmproject.org/docs/encoder-parameters/
                return "-speed 16 -quality good -profile:v 0 -slices 8";
            }

            // asf/wmv
            if (videoCodec.Equals("wmv2", StringComparison.OrdinalIgnoreCase))
            {
                return "-g 100 -qmax 15";
            }

            if (videoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase))
            {
                return "-preset superfast";
            }

            if (videoCodec.Equals("mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                return "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }

            return string.Empty;
        }

        protected string GetAudioFilterParam(StreamState state, bool isHls)
        {
            var volParam = string.Empty;
            var audioSampleRate = string.Empty;

            var channels = GetNumAudioChannelsParam(state.Request, state.AudioStream);
            
            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2 && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5)
            {
                volParam = ",volume=2.000000";
            }

            if (state.Request.AudioSampleRate.HasValue)
            {
                audioSampleRate = state.Request.AudioSampleRate.Value + ":";
            }

            var adelay = isHls ? "adelay=1," : string.Empty;

            var pts = string.Empty;

            if (state.SubtitleStream != null)
            {
                if (state.SubtitleStream.Codec.IndexOf("srt", StringComparison.OrdinalIgnoreCase) != -1 ||
                   state.SubtitleStream.Codec.IndexOf("subrip", StringComparison.OrdinalIgnoreCase) != -1 ||
                   string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase))
                {
                    var seconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds;

                    pts = string.Format(",asetpts=PTS-{0}/TB",
                Math.Round(seconds).ToString(UsCulture));
                }
            }

            return string.Format("-af \"{0}aresample={1}async=1{2}{3}\"", 

                adelay,
                audioSampleRate, 
                volParam,
                pts);
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
            var copyTsParam = string.Empty;
            var yadifParam = "yadif=0:-1:0,";

            var request = state.VideoRequest;

            if (state.SubtitleStream != null)
            {
                if (state.SubtitleStream.Codec.IndexOf("srt", StringComparison.OrdinalIgnoreCase) != -1 ||
                    state.SubtitleStream.Codec.IndexOf("subrip", StringComparison.OrdinalIgnoreCase) != -1 ||
                    string.Equals(state.SubtitleStream.Codec, "ass", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(state.SubtitleStream.Codec, "ssa", StringComparison.OrdinalIgnoreCase))
                {
                    assSubtitleParam = GetTextSubtitleParam(state, performTextSubtitleConversion);
                    copyTsParam = " -copyts";
                }
            }

            // If fixed dimensions were supplied
            if (request.Width.HasValue && request.Height.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);
                var heightParam = request.Height.Value.ToString(UsCulture);

                return string.Format("{4} -vf \"{0}scale=trunc({1}/2)*2:trunc({2}/2)*2{3}\"",yadifParam, widthParam, heightParam, assSubtitleParam, copyTsParam);
            }

            var isH264Output = outputVideoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase);

            // If a fixed width was requested
            if (request.Width.HasValue)
            {
                var widthParam = request.Width.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format("{3} -vf \"{0}scale={1}:trunc(ow/a/2)*2{2}\"",yadifParam, widthParam, assSubtitleParam, copyTsParam) :
                    string.Format("{3} -vf \"{0}scale={1}:-1{2}\"",yadifParam, widthParam, assSubtitleParam, copyTsParam);
            }

            // If a fixed height was requested
            if (request.Height.HasValue)
            {
                var heightParam = request.Height.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format("{3} -vf \"{0}scale=trunc(oh*a*2)/2:{1}{2}\"",yadifParam, heightParam, assSubtitleParam, copyTsParam) :
                    string.Format("{3} -vf \"{0}scale=-1:{1}{2}\"",yadifParam, heightParam, assSubtitleParam, copyTsParam);
            }

            // If a max width was requested
            if (request.MaxWidth.HasValue && (!request.MaxHeight.HasValue || state.VideoStream == null))
            {
                var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format("{3} -vf \"{0}scale=min(iw\\,{1}):trunc(ow/a/2)*2{2}\"",yadifParam, maxWidthParam, assSubtitleParam, copyTsParam) :
                    string.Format("{3} -vf \"{0}scale=min(iw\\,{1}):-1{2}\"",yadifParam, maxWidthParam, assSubtitleParam, copyTsParam);
            }

            // If a max height was requested
            if (request.MaxHeight.HasValue && (!request.MaxWidth.HasValue || state.VideoStream == null))
            {
                var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                return isH264Output ?
                    string.Format("{3} -vf \"{0}scale=trunc(oh*a*2)/2:min(ih\\,{1}){2}\"",yadifParam, maxHeightParam, assSubtitleParam, copyTsParam) :
                    string.Format("{3} -vf \"{0}scale=-1:min(ih\\,{1}){2}\"",yadifParam, maxHeightParam, assSubtitleParam, copyTsParam);
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

                return string.Format("{4} -vf \"{0}scale=trunc({1}/2)*2:trunc({2}/2)*2{3}\"",yadifParam, widthParam, heightParam, assSubtitleParam, copyTsParam);
            }

            // Otherwise use -vf scale since ffmpeg will ensure internally that the aspect ratio is preserved
            return string.Format("{3} -vf \"{0}scale={1}:-1{2}\"",yadifParam, Convert.ToInt32(outputSize.Width), assSubtitleParam, copyTsParam);
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(StreamState state, bool performConversion)
        {
            var path = state.SubtitleStream.IsExternal ? GetConvertedAssPath(state.MediaPath, state.SubtitleStream, performConversion) :
                GetExtractedAssPath(state, performConversion);

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var seconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds;

            return string.Format(",ass='{0}',setpts=PTS -{1}/TB", 
                path.Replace('\\', '/').Replace(":/", "\\:/"),
                Math.Round(seconds).ToString(UsCulture));
        }

        /// <summary>
        /// Gets the extracted ass path.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        private string GetExtractedAssPath(StreamState state, bool performConversion)
        {
            var path = FFMpegManager.Instance.GetSubtitleCachePath(state.MediaPath, state.SubtitleStream, ".ass");

            if (performConversion)
            {
                InputType type;

                var inputPath = MediaEncoderHelpers.GetInputArgument(state.MediaPath, state.IsRemote, state.VideoType, state.IsoType, null, state.PlayableStreamFileNames, out type);

                try
                {
                    var parentPath = Path.GetDirectoryName(path);

                    Directory.CreateDirectory(parentPath);

                    var task = MediaEncoder.ExtractTextSubtitle(inputPath, type, state.SubtitleStream.Index, path, CancellationToken.None);

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
        /// <param name="performConversion">if set to <c>true</c> [perform conversion].</param>
        /// <returns>System.String.</returns>
        private string GetConvertedAssPath(string mediaPath, MediaStream subtitleStream, bool performConversion)
        {
            var path = FFMpegManager.Instance.GetSubtitleCachePath(mediaPath, subtitleStream, ".ass");

            if (performConversion)
            {
                try
                {
                    var parentPath = Path.GetDirectoryName(path);

                    Directory.CreateDirectory(parentPath);

                    var task = MediaEncoder.ConvertTextSubtitleToAss(subtitleStream.Path, path, subtitleStream.Language, CancellationToken.None);

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

            var videoSizeParam = string.Empty;

            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                videoSizeParam = string.Format(",scale={0}:{1}", state.VideoStream.Width.Value.ToString(UsCulture), state.VideoStream.Height.Value.ToString(UsCulture));
            }

            return string.Format(" -filter_complex \"[0:{0}]format=yuva444p{3},lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:{1}] [sub] overlay{2}\"",
                state.SubtitleStream.Index,
                state.VideoStream.Index,
                outputSizeParam,
                videoSizeParam);
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
            var type = !isVideo ? MediaEncoderHelpers.GetInputType(null, null) :
                MediaEncoderHelpers.GetInputType(videoType, isoType);

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
            if (state.SendInputOverStandardInput)
            {
                return "-";
            }

            var type = InputType.File;

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
            if (!File.Exists(MediaEncoder.EncoderPath))
            {
                throw new InvalidOperationException("ffmpeg was not found at " + MediaEncoder.EncoderPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            if (state.IsInputVideo && state.VideoType == VideoType.Iso && state.IsoType.HasValue && IsoManager.CanMount(state.MediaPath))
            {
                state.IsoMount = await IsoManager.Mount(state.MediaPath, CancellationToken.None).ConfigureAwait(false);
            }

            var commandLineArgs = GetCommandLineArguments(outputPath, state, true);

            if (ServerConfigurationManager.Configuration.EnableDebugEncodingLogging)
            {
                commandLineArgs = "-loglevel debug " + commandLineArgs;
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
                    Arguments = commandLineArgs,

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false,

                    RedirectStandardInput = state.SendInputOverStandardInput
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

            if (state.SendInputOverStandardInput)
            {
                StreamToStandardInput(process, state);
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
                await Task.Delay(3000).ConfigureAwait(false);
            }
        }

        private async void StreamToStandardInput(Process process, StreamState state)
        {
            state.StandardInputCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await StreamToStandardInputInternal(process, state).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Stream to standard input closed normally.");
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error writing to standard input", ex);
            }
        }

        private async Task StreamToStandardInputInternal(Process process, StreamState state)
        {
            state.StandardInputCancellationTokenSource = new CancellationTokenSource();

            using (var fileStream = FileSystem.GetFileStream(state.MediaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                await new EndlessStreamCopy().CopyStream(fileStream, process.StandardInput.BaseStream, state.StandardInputCancellationTokenSource.Token).ConfigureAwait(false);
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
        protected async void OnFfMpegProcessExited(Process process, StreamState state)
        {
            if (state.IsoMount != null)
            {
                state.IsoMount.Dispose();
                state.IsoMount = null;
            }

            if (state.StandardInputCancellationTokenSource != null)
            {
                state.StandardInputCancellationTokenSource.Cancel();
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

            if (!string.IsNullOrEmpty(state.LiveTvStreamId))
            {
                try
                {
                    await LiveTvManager.CloseLiveStream(state.LiveTvStreamId, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error closing live tv stream", ex);
                }
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

            var item = DtoService.GetItemByDtoId(request.Id);

            if (item is ILiveTvRecording)
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
                    var streamInfo = await LiveTvManager.GetRecordingStream(request.Id, cancellationToken).ConfigureAwait(false);

                    state.LiveTvStreamId = streamInfo.Id;

                    if (!string.IsNullOrEmpty(streamInfo.Path) && File.Exists(streamInfo.Path))
                    {
                        state.MediaPath = streamInfo.Path;
                        state.IsRemote = false;
                    }
                    else if (!string.IsNullOrEmpty(streamInfo.Url))
                    {
                        state.MediaPath = streamInfo.Url;
                        state.IsRemote = true;
                    }
                }

                //state.RunTimeTicks = recording.RunTimeTicks;
                state.SendInputOverStandardInput = recording.RecordingInfo.Status == RecordingStatus.InProgress;
            }
            else if (item is LiveTvChannel)
            {
                var channel = LiveTvManager.GetInternalChannel(request.Id);

                state.VideoType = VideoType.VideoFile;
                state.IsInputVideo = string.Equals(channel.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);
                state.PlayableStreamFileNames = new List<string>();

                var streamInfo = await LiveTvManager.GetChannelStream(request.Id, cancellationToken).ConfigureAwait(false);

                state.LiveTvStreamId = streamInfo.Id;

                if (!string.IsNullOrEmpty(streamInfo.Path) && File.Exists(streamInfo.Path))
                {
                    state.MediaPath = streamInfo.Path;
                    state.IsRemote = false;
                }
                else if (!string.IsNullOrEmpty(streamInfo.Url))
                {
                    state.MediaPath = streamInfo.Url;
                    state.IsRemote = true;
                }

                state.SendInputOverStandardInput = true;
            }
            else
            {
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

                state.RunTimeTicks = item.RunTimeTicks;
            }

            var videoRequest = request as VideoStreamRequest;

            var mediaStreams = ItemRepository.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = item.Id

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
