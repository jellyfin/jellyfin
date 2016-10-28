using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;

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

        protected IFileSystem FileSystem { get; private set; }

        protected IDlnaManager DlnaManager { get; private set; }
        protected IDeviceManager DeviceManager { get; private set; }
        protected ISubtitleEncoder SubtitleEncoder { get; private set; }
        protected IMediaSourceManager MediaSourceManager { get; private set; }
        protected IZipClient ZipClient { get; private set; }
        protected IJsonSerializer JsonSerializer { get; private set; }

        public static IServerApplicationHost AppHost;
        public static IHttpClient HttpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        protected BaseStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer)
        {
            JsonSerializer = jsonSerializer;
            ZipClient = zipClient;
            MediaSourceManager = mediaSourceManager;
            DeviceManager = deviceManager;
            SubtitleEncoder = subtitleEncoder;
            DlnaManager = dlnaManager;
            FileSystem = fileSystem;
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
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding);

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
        private string GetOutputFilePath(StreamState state)
        {
            var folder = ServerConfigurationManager.ApplicationPaths.TranscodingTempPath;

            var outputFileExtension = GetOutputFileExtension(state);

            var data = GetCommandLineArguments("dummy\\dummy", state, false);

            data += "-" + (state.Request.DeviceId ?? string.Empty);
            data += "-" + (state.Request.PlaySessionId ?? string.Empty);

            var dataHash = data.GetMD5().ToString("N");

            if (EnableOutputInSubFolder)
            {
                return Path.Combine(folder, dataHash, dataHash + (outputFileExtension ?? string.Empty).ToLower());
            }

            return Path.Combine(folder, dataHash + (outputFileExtension ?? string.Empty).ToLower());
        }

        protected virtual bool EnableOutputInSubFolder
        {
            get { return false; }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the fast seek command line parameter.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        /// <value>The fast seek command line parameter.</value>
        protected string GetFastSeekCommandLineParameter(StreamRequest request)
        {
            var time = request.StartTimeTicks ?? 0;

            if (time > 0)
            {
                return string.Format("-ss {0}", MediaEncoder.GetTimeParameter(time));
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
            // If we don't have known media info
            // If input is video, use -sn to drop subtitles
            // Otherwise just return empty
            if (state.VideoStream == null && state.AudioStream == null)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            // We have media info, but we don't know the stream indexes
            if (state.VideoStream != null && state.VideoStream.Index == -1)
            {
                return "-sn";
            }

            // We have media info, but we don't know the stream indexes
            if (state.AudioStream != null && state.AudioStream.Index == -1)
            {
                return state.IsInputVideo ? "-sn" : string.Empty;
            }

            var args = string.Empty;

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

            if (state.SubtitleStream == null || state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Hls)
            {
                args += " -map -0:s";
            }
            else if (state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
            {
                args += " -map 1:0 -sn";
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

            if (type == MediaStreamType.Video)
            {
                streams = streams.Where(i => !string.Equals(i.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase)).ToList();
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
        protected int GetNumberOfThreads(StreamState state, bool isWebm)
        {
            var threads = ApiEntryPoint.Instance.GetEncodingOptions().EncodingThreadCount;

            if (isWebm)
            {
                // Recommended per docs
                return Math.Max(Environment.ProcessorCount - 1, 2);
            }

            // Automatic
            if (threads == -1)
            {
                return 0;
            }

            return threads;
        }

        protected string GetH264Encoder(StreamState state)
        {
            var defaultEncoder = "libx264";

            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType == VideoType.VideoFile)
            {
                var encodingOptions = ApiEntryPoint.Instance.GetEncodingOptions();
                var hwType = encodingOptions.HardwareAccelerationType;

                if (string.Equals(hwType, "qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(hwType, "h264_qsv", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_qsv", defaultEncoder);
                }

                if (string.Equals(hwType, "nvenc", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_nvenc", defaultEncoder);
                }
                if (string.Equals(hwType, "h264_omx", StringComparison.OrdinalIgnoreCase))
                {
                    return GetAvailableEncoder("h264_omx", defaultEncoder);
                }
                if (string.Equals(hwType, "vaapi", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(encodingOptions.VaapiDevice))
                {
                    if (IsVaapiSupported(state))
                    {
                        return GetAvailableEncoder("h264_vaapi", defaultEncoder);
                    }
                }
            }

            return defaultEncoder;
        }

        private bool IsVaapiSupported(StreamState state)
        {
            var videoStream = state.VideoStream;

            if (videoStream != null)
            {
                // vaapi will throw an error with this input
                // [vaapi @ 0x7faed8000960] No VAAPI support for codec mpeg4 profile -99.
                if (string.Equals(videoStream.Codec, "mpeg4", StringComparison.OrdinalIgnoreCase) && videoStream.Level == -99)
                {
                    return false;
                }
            }
            return true;
        }

        private string GetAvailableEncoder(string preferredEncoder, string defaultEncoder)
        {
            if (MediaEncoder.SupportsEncoder(preferredEncoder))
            {
                return preferredEncoder;
            }
            return defaultEncoder;
        }

        /// <summary>
        /// Gets the video bitrate to specify on the command line
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoEncoder">The video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetVideoQualityParam(StreamState state, string videoEncoder)
        {
            var param = string.Empty;

            var isVc1 = state.VideoStream != null &&
                string.Equals(state.VideoStream.Codec, "vc1", StringComparison.OrdinalIgnoreCase);

            var encodingOptions = ApiEntryPoint.Instance.GetEncodingOptions();

            if (string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(encodingOptions.H264Preset))
                {
                    param += "-preset " + encodingOptions.H264Preset;
                }
                else
                {
                    param += "-preset superfast";
                }

                if (encodingOptions.H264Crf >= 0 && encodingOptions.H264Crf <= 51)
                {
                    param += " -crf " + encodingOptions.H264Crf.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    param += " -crf 23";
                }

                param += " -tune zerolatency";
            }

            else if (string.Equals(videoEncoder, "libx265", StringComparison.OrdinalIgnoreCase))
            {
                param += "-preset fast";

                param += " -crf 28";
            }

            // h264 (h264_qsv)
            else if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase))
            {
                param += "-preset 7 -look_ahead 0";

            }

            // h264 (h264_nvenc)
            else if (string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase))
            {
                param += "-preset default";
            }

            // webm
            else if (string.Equals(videoEncoder, "libvpx", StringComparison.OrdinalIgnoreCase))
            {
                // Values 0-3, 0 being highest quality but slower
                var profileScore = 0;

                string crf;
                var qmin = "0";
                var qmax = "50";

                crf = "10";

                if (isVc1)
                {
                    profileScore++;
                }

                // Max of 2
                profileScore = Math.Min(profileScore, 2);

                // http://www.webmproject.org/docs/encoder-parameters/
                param += string.Format("-speed 16 -quality good -profile:v {0} -slices 8 -crf {1} -qmin {2} -qmax {3}",
                    profileScore.ToString(UsCulture),
                    crf,
                    qmin,
                    qmax);
            }

            else if (string.Equals(videoEncoder, "mpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += "-mbd rd -flags +mv4+aic -trellis 2 -cmp 2 -subcmp 2 -bf 2";
            }

            // asf/wmv
            else if (string.Equals(videoEncoder, "wmv2", StringComparison.OrdinalIgnoreCase))
            {
                param += "-qmin 2";
            }

            else if (string.Equals(videoEncoder, "msmpeg4", StringComparison.OrdinalIgnoreCase))
            {
                param += "-mbd 2";
            }

            param += GetVideoBitrateParam(state, videoEncoder);

            var framerate = GetFramerateParam(state);
            if (framerate.HasValue)
            {
                param += string.Format(" -r {0}", framerate.Value.ToString(UsCulture));
            }

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                param += " -vsync " + state.OutputVideoSync;
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Profile))
            {
                if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    // not supported by h264_omx
                    param += " -profile:v " + state.VideoRequest.Profile;
                }
            }

            if (!string.IsNullOrEmpty(state.VideoRequest.Level))
            {
                var level = NormalizeTranscodingLevel(state.OutputVideoCodec, state.VideoRequest.Level);

                // h264_qsv and h264_nvenc expect levels to be expressed as a decimal. libx264 supports decimal and non-decimal format
                // also needed for libx264 due to https://trac.ffmpeg.org/ticket/3307
                if (string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(videoEncoder, "libx264", StringComparison.OrdinalIgnoreCase))
                {
                    switch (level)
                    {
                        case "30":
                            param += " -level 3.0";
                            break;
                        case "31":
                            param += " -level 3.1";
                            break;
                        case "32":
                            param += " -level 3.2";
                            break;
                        case "40":
                            param += " -level 4.0";
                            break;
                        case "41":
                            param += " -level 4.1";
                            break;
                        case "42":
                            param += " -level 4.2";
                            break;
                        case "50":
                            param += " -level 5.0";
                            break;
                        case "51":
                            param += " -level 5.1";
                            break;
                        case "52":
                            param += " -level 5.2";
                            break;
                        default:
                            param += " -level " + level;
                            break;
                    }
                }
                else if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase))
                {
                    param += " -level " + level;
                }
            }

            if (!string.Equals(videoEncoder, "h264_omx", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_qsv", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_nvenc", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(videoEncoder, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                param = "-pix_fmt yuv420p " + param;
            }

            return param;
        }

        private string NormalizeTranscodingLevel(string videoCodec, string level)
        {
            double requestLevel;

            // Clients may direct play higher than level 41, but there's no reason to transcode higher
            if (double.TryParse(level, NumberStyles.Any, UsCulture, out requestLevel))
            {
                if (string.Equals(videoCodec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    if (requestLevel > 41)
                    {
                        return "41";
                    }
                }
            }

            return level;
        }

        protected string GetAudioFilterParam(StreamState state, bool isHls)
        {
            var volParam = string.Empty;
            var audioSampleRate = string.Empty;

            var channels = state.OutputAudioChannels;

            // Boost volume to 200% when downsampling from 6ch to 2ch
            if (channels.HasValue && channels.Value <= 2)
            {
                if (state.AudioStream != null && state.AudioStream.Channels.HasValue && state.AudioStream.Channels.Value > 5 && !ApiEntryPoint.Instance.GetEncodingOptions().DownMixAudioBoost.Equals(1))
                {
                    volParam = ",volume=" + ApiEntryPoint.Instance.GetEncodingOptions().DownMixAudioBoost.ToString(UsCulture);
                }
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                audioSampleRate = state.OutputAudioSampleRate.Value + ":";
            }

            var adelay = isHls ? "adelay=1," : string.Empty;

            var pts = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode && !state.VideoRequest.CopyTimestamps)
            {
                var seconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds;

                pts = string.Format(",asetpts=PTS-{0}/TB", Math.Round(seconds).ToString(UsCulture));
            }

            return string.Format("-af \"{0}aresample={1}async={4}{2}{3}\"",

                adelay,
                audioSampleRate,
                volParam,
                pts,
                state.OutputAudioSync);
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <param name="allowTimeStampCopy">if set to <c>true</c> [allow time stamp copy].</param>
        /// <returns>System.String.</returns>
        protected string GetOutputSizeParam(StreamState state,
            string outputVideoCodec,
            bool allowTimeStampCopy = true)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var request = state.VideoRequest;

            var filters = new List<string>();

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("format=nv12|vaapi");
                filters.Add("hwupload");
            }
            else if (state.DeInterlace && !string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add("yadif=0:-1:0");
            }

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
            {
                // Work around vaapi's reduced scaling features
                var scaler = "scale_vaapi";

                // Given the input dimensions (inputWidth, inputHeight), determine the output dimensions
                // (outputWidth, outputHeight). The user may request precise output dimensions or maximum
                // output dimensions. Output dimensions are guaranteed to be even.
                decimal inputWidth = Convert.ToDecimal(state.VideoStream.Width);
                decimal inputHeight = Convert.ToDecimal(state.VideoStream.Height);
                decimal outputWidth = request.Width.HasValue ? Convert.ToDecimal(request.Width.Value) : inputWidth;
                decimal outputHeight = request.Height.HasValue ? Convert.ToDecimal(request.Height.Value) : inputHeight;
                decimal maximumWidth = request.MaxWidth.HasValue ? Convert.ToDecimal(request.MaxWidth.Value) : outputWidth;
                decimal maximumHeight = request.MaxHeight.HasValue ? Convert.ToDecimal(request.MaxHeight.Value) : outputHeight;

                if (outputWidth > maximumWidth || outputHeight > maximumHeight)
                {
                    var scale = Math.Min(maximumWidth / outputWidth, maximumHeight / outputHeight);
                    outputWidth = Math.Min(maximumWidth, Math.Truncate(outputWidth * scale));
                    outputHeight = Math.Min(maximumHeight, Math.Truncate(outputHeight * scale));
                }

                outputWidth = 2 * Math.Truncate(outputWidth / 2);
                outputHeight = 2 * Math.Truncate(outputHeight / 2);

                if (outputWidth != inputWidth || outputHeight != inputHeight)
                {
                    filters.Add(string.Format("{0}=w={1}:h={2}", scaler, outputWidth.ToString(UsCulture), outputHeight.ToString(UsCulture)));
                }
            }
            else
            {
                // If fixed dimensions were supplied
                if (request.Width.HasValue && request.Height.HasValue)
                {
                    var widthParam = request.Width.Value.ToString(UsCulture);
                    var heightParam = request.Height.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc({0}/2)*2:trunc({1}/2)*2", widthParam, heightParam));
                }

                // If Max dimensions were supplied, for width selects lowest even number between input width and width req size and selects lowest even number from in width*display aspect and requested size
                else if (request.MaxWidth.HasValue && request.MaxHeight.HasValue)
                {
                    var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);
                    var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,min({0}\\,{1}*dar))/2)*2:trunc(min(max(iw/dar\\,ih)\\,min({0}/dar\\,{1}))/2)*2", maxWidthParam, maxHeightParam));
                }

                // If a fixed width was requested
                else if (request.Width.HasValue)
                {
                    var widthParam = request.Width.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale={0}:trunc(ow/a/2)*2", widthParam));
                }

                // If a fixed height was requested
                else if (request.Height.HasValue)
                {
                    var heightParam = request.Height.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc(oh*a/2)*2:{0}", heightParam));
                }

                // If a max width was requested
                else if (request.MaxWidth.HasValue)
                {
                    var maxWidthParam = request.MaxWidth.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc(min(max(iw\\,ih*dar)\\,{0})/2)*2:trunc(ow/dar/2)*2", maxWidthParam));
                }

                // If a max height was requested
                else if (request.MaxHeight.HasValue)
                {
                    var maxHeightParam = request.MaxHeight.Value.ToString(UsCulture);

                    filters.Add(string.Format("scale=trunc(oh*a/2)*2:min(max(iw/dar\\,ih)\\,{0})", maxHeightParam));
                }
            }

            var output = string.Empty;

            if (state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode)
            {
                var subParam = GetTextSubtitleParam(state);

                filters.Add(subParam);

                if (allowTimeStampCopy)
                {
                    output += " -copyts";
                }
            }

            if (filters.Count > 0)
            {
                output += string.Format(" -vf \"{0}\"", string.Join(",", filters.ToArray()));
            }

            return output;
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(StreamState state)
        {
            var seconds = Math.Round(TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds);

            var setPtsParam = state.VideoRequest.CopyTimestamps
                ? string.Empty
                : string.Format(",setpts=PTS -{0}/TB", seconds.ToString(UsCulture));

            if (state.SubtitleStream.IsExternal)
            {
                var subtitlePath = state.SubtitleStream.Path;

                var charsetParam = string.Empty;

                if (!string.IsNullOrEmpty(state.SubtitleStream.Language))
                {
                    var charenc = SubtitleEncoder.GetSubtitleFileCharacterSet(subtitlePath, state.SubtitleStream.Language, state.MediaSource.Protocol, CancellationToken.None).Result;

                    if (!string.IsNullOrEmpty(charenc))
                    {
                        charsetParam = ":charenc=" + charenc;
                    }
                }

                // TODO: Perhaps also use original_size=1920x800 ??
                return string.Format("subtitles=filename='{0}'{1}{2}",
                    MediaEncoder.EscapeSubtitleFilterPath(subtitlePath),
                    charsetParam,
                    setPtsParam);
            }

            var mediaPath = state.MediaPath ?? string.Empty;

            return string.Format("subtitles='{0}:si={1}'{2}",
                MediaEncoder.EscapeSubtitleFilterPath(mediaPath),
                state.InternalSubtitleStreamOffset.ToString(UsCulture),
                setPtsParam);
        }

        /// <summary>
        /// Gets the internal graphical subtitle param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetGraphicalSubtitleParam(StreamState state, string outputVideoCodec)
        {
            var outputSizeParam = string.Empty;

            var request = state.VideoRequest;

            // Add resolution params, if specified
            if (request.Width.HasValue || request.Height.HasValue || request.MaxHeight.HasValue || request.MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(state, outputVideoCodec).TrimEnd('"');

                if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase))
                {
                    outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("format", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
                }
            }

            if (string.Equals(outputVideoCodec, "h264_vaapi", StringComparison.OrdinalIgnoreCase) && outputSizeParam.Length == 0)
            {
                outputSizeParam = ",format=nv12|vaapi,hwupload";
            }

            var videoSizeParam = string.Empty;

            if (state.VideoStream != null && state.VideoStream.Width.HasValue && state.VideoStream.Height.HasValue)
            {
                videoSizeParam = string.Format(",scale={0}:{1}", state.VideoStream.Width.Value.ToString(UsCulture), state.VideoStream.Height.Value.ToString(UsCulture));
            }

            var mapPrefix = state.SubtitleStream.IsExternal ?
                1 :
                0;

            var subtitleStreamIndex = state.SubtitleStream.IsExternal
                ? 0
                : state.SubtitleStream.Index;

            return string.Format(" -filter_complex \"[{0}:{1}]format=yuva444p{4},lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:{2}] [sub] overlay{3}\"",
                mapPrefix.ToString(UsCulture),
                subtitleStreamIndex.ToString(UsCulture),
                state.VideoStream.Index.ToString(UsCulture),
                outputSizeParam,
                videoSizeParam);
        }

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetProbeSizeArgument(StreamState state)
        {
            if (state.PlayableStreamFileNames.Count > 0)
            {
                return MediaEncoder.GetProbeSizeAndAnalyzeDurationArgument(state.PlayableStreamFileNames.ToArray(), state.InputProtocol);
            }

            return MediaEncoder.GetProbeSizeAndAnalyzeDurationArgument(new[] { state.MediaPath }, state.InputProtocol);
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <param name="outputAudioCodec">The output audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private int? GetNumAudioChannelsParam(StreamRequest request, MediaStream audioStream, string outputAudioCodec)
        {
            var inputChannels = audioStream == null
                ? null
                : audioStream.Channels;

            if (inputChannels <= 0)
            {
                inputChannels = null;
            }

            int? resultChannels = null;
            var codec = outputAudioCodec ?? string.Empty;

            if (codec.IndexOf("wma", StringComparison.OrdinalIgnoreCase) != -1)
            {
                // wmav2 currently only supports two channel output
                resultChannels = Math.Min(2, inputChannels ?? 2);
            }

            else if (request.MaxAudioChannels.HasValue)
            {
                var channelLimit = codec.IndexOf("mp3", StringComparison.OrdinalIgnoreCase) != -1
                   ? 2
                   : 6;

                if (inputChannels.HasValue)
                {
                    channelLimit = Math.Min(channelLimit, inputChannels.Value);
                }

                // If we don't have any media info then limit it to 5 to prevent encoding errors due to asking for too many channels
                resultChannels = Math.Min(request.MaxAudioChannels.Value, channelLimit);
            }

            if (request.TranscodingMaxAudioChannels.HasValue && !string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                resultChannels = Math.Min(request.TranscodingMaxAudioChannels.Value, resultChannels ?? inputChannels ?? request.TranscodingMaxAudioChannels.Value);
            }

            return resultChannels ?? request.AudioChannels;
        }

        /// <summary>
        /// Determines whether the specified stream is H264.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns><c>true</c> if the specified stream is H264; otherwise, <c>false</c>.</returns>
        protected bool IsH264(MediaStream stream)
        {
            var codec = stream.Codec ?? string.Empty;

            return codec.IndexOf("264", StringComparison.OrdinalIgnoreCase) != -1 ||
                   codec.IndexOf("avc", StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// Gets the audio encoder.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetAudioEncoder(StreamState state)
        {
            var codec = state.OutputAudioCodec;

            if (string.Equals(codec, "aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac -strict experimental";
            }
            if (string.Equals(codec, "mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "libmp3lame";
            }
            if (string.Equals(codec, "vorbis", StringComparison.OrdinalIgnoreCase))
            {
                return "libvorbis";
            }
            if (string.Equals(codec, "wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wmav2";
            }

            return codec.ToLower();
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetVideoEncoder(StreamState state)
        {
            var codec = state.OutputVideoCodec;

            if (!string.IsNullOrEmpty(codec))
            {
                if (string.Equals(codec, "h264", StringComparison.OrdinalIgnoreCase))
                {
                    return GetH264Encoder(state);
                }
                if (string.Equals(codec, "vpx", StringComparison.OrdinalIgnoreCase))
                {
                    return "libvpx";
                }
                if (string.Equals(codec, "wmv", StringComparison.OrdinalIgnoreCase))
                {
                    return "wmv2";
                }
                if (string.Equals(codec, "theora", StringComparison.OrdinalIgnoreCase))
                {
                    return "libtheora";
                }

                return codec.ToLower();
            }

            return "copy";
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetVideoDecoder(StreamState state)
        {
            if (string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Only use alternative encoders for video files.
            // When using concat with folder rips, if the mfx session fails to initialize, ffmpeg will be stuck retrying and will not exit gracefully
            // Since transcoding of folder rips is expiremental anyway, it's not worth adding additional variables such as this.
            if (state.VideoType != VideoType.VideoFile)
            {
                return null;
            }

            if (state.VideoStream != null && !string.IsNullOrWhiteSpace(state.VideoStream.Codec))
            {
                if (string.Equals(ApiEntryPoint.Instance.GetEncodingOptions().HardwareAccelerationType, "qsv", StringComparison.OrdinalIgnoreCase))
                {
                    switch (state.MediaSource.VideoStream.Codec.ToLower())
                    {
                        case "avc":
                        case "h264":
                            if (MediaEncoder.SupportsDecoder("h264_qsv"))
                            {
                                // Seeing stalls and failures with decoding. Not worth it compared to encoding.
                                return "-c:v h264_qsv ";
                            }
                            break;
                        case "mpeg2video":
                            if (MediaEncoder.SupportsDecoder("mpeg2_qsv"))
                            {
                                return "-c:v mpeg2_qsv ";
                            }
                            break;
                        case "vc1":
                            if (MediaEncoder.SupportsDecoder("vc1_qsv"))
                            {
                                return "-c:v vc1_qsv ";
                            }
                            break;
                    }
                }
            }

            // leave blank so ffmpeg will decide
            return null;
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetInputArgument(StreamState state)
        {
            var arg = string.Format("-i {0}", GetInputPathArgument(state));

            if (state.SubtitleStream != null && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode)
            {
                if (state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream)
                {
                    if (state.VideoStream != null && state.VideoStream.Width.HasValue)
                    {
                        // This is hacky but not sure how to get the exact subtitle resolution
                        double height = state.VideoStream.Width.Value;
                        height /= 16;
                        height *= 9;

                        arg += string.Format(" -canvas_size {0}:{1}", state.VideoStream.Width.Value.ToString(CultureInfo.InvariantCulture), Convert.ToInt32(height).ToString(CultureInfo.InvariantCulture));
                    }
                    arg += " -i \"" + state.SubtitleStream.Path + "\"";
                }
            }

            if (state.VideoRequest != null)
            {
                var encodingOptions = ApiEntryPoint.Instance.GetEncodingOptions();
                if (GetVideoEncoder(state).IndexOf("vaapi", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode;
                    var hwOutputFormat = "vaapi";

                    if (hasGraphicalSubs)
                    {
                        hwOutputFormat = "yuv420p";
                    }

                    arg = "-hwaccel vaapi -hwaccel_output_format " + hwOutputFormat + " -vaapi_device " + encodingOptions.VaapiDevice + " " + arg;
                }
            }

            return arg.Trim();
        }

        private string GetInputPathArgument(StreamState state)
        {
            var protocol = state.InputProtocol;
            var mediaPath = state.MediaPath ?? string.Empty;

            var inputPath = new[] { mediaPath };

            if (state.IsInputVideo)
            {
                if (!(state.VideoType == VideoType.Iso && state.IsoMount == null))
                {
                    inputPath = MediaEncoderHelpers.GetInputArgument(FileSystem, mediaPath, state.InputProtocol, state.IsoMount, state.PlayableStreamFileNames);
                }
            }

            return MediaEncoder.GetInputArgument(inputPath, protocol);
        }

        private async Task AcquireResources(StreamState state, CancellationTokenSource cancellationTokenSource)
        {
            if (state.VideoType == VideoType.Iso && state.IsoType.HasValue && IsoManager.CanMount(state.MediaPath))
            {
                state.IsoMount = await IsoManager.Mount(state.MediaPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.MediaSource.RequiresOpening && string.IsNullOrWhiteSpace(state.Request.LiveStreamId))
            {
                var liveStreamResponse = await MediaSourceManager.OpenLiveStream(new LiveStreamRequest
                {
                    OpenToken = state.MediaSource.OpenToken

                }, false, cancellationTokenSource.Token).ConfigureAwait(false);

                AttachMediaSourceInfo(state, liveStreamResponse.MediaSource, state.VideoRequest, state.RequestedUrl);

                if (state.VideoRequest != null)
                {
                    TryStreamCopy(state, state.VideoRequest);
                }
            }

            if (state.MediaSource.BufferMs.HasValue)
            {
                await Task.Delay(state.MediaSource.BufferMs.Value, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>Task.</returns>
        protected async Task<TranscodingJob> StartFfMpeg(StreamState state,
            string outputPath,
            CancellationTokenSource cancellationTokenSource,
            string workingDirectory = null)
        {
            FileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

            await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

            if (state.VideoRequest != null && !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                var auth = AuthorizationContext.GetAuthorizationInfo(Request);
                if (!string.IsNullOrWhiteSpace(auth.UserId))
                {
                    var user = UserManager.GetUserById(auth.UserId);
                    if (!user.Policy.EnableVideoPlaybackTranscoding)
                    {
                        ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                        throw new ArgumentException("User does not have access to video transcoding");
                    }
                }
            }

            var transcodingId = Guid.NewGuid().ToString("N");
            var commandLineArgs = GetCommandLineArguments(outputPath, state, true);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    //RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = MediaEncoder.EncoderPath,
                    Arguments = commandLineArgs,

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            var transcodingJob = ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath,
                state.Request.PlaySessionId,
                state.MediaSource.LiveStreamId,
                transcodingId,
                TranscodingJobType,
                process,
                state.Request.DeviceId,
                state,
                cancellationTokenSource);

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            Logger.Info(commandLineLogMessage);

            var logFilePrefix = "ffmpeg-transcode";
            if (state.VideoRequest != null && string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) && string.Equals(state.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                logFilePrefix = "ffmpeg-directstream";
            }
            else if (state.VideoRequest != null && string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                logFilePrefix = "ffmpeg-remux";
            }

            var logFilePath = Path.Combine(ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, logFilePrefix + "-" + Guid.NewGuid() + ".txt");
            FileSystem.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            state.LogFileStream = FileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(Request.AbsoluteUri + Environment.NewLine + Environment.NewLine + JsonSerializer.SerializeToString(state.MediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            await state.LogFileStream.WriteAsync(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length, cancellationTokenSource.Token).ConfigureAwait(false);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, transcodingJob, state);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting ffmpeg", ex);

                ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                throw;
            }

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            //process.BeginOutputReadLine();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            var task = Task.Run(() => StartStreamingLog(transcodingJob, state, process.StandardError.BaseStream, state.LogFileStream));

            // Wait for the file to exist before proceeeding
            while (!FileSystem.FileExists(state.WaitForPath ?? outputPath) && !transcodingJob.HasExited)
            {
                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.IsInputVideo && transcodingJob.Type == TranscodingJobType.Progressive && !transcodingJob.HasExited)
            {
                await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                if (state.ReadInputAtNativeFramerate && !transcodingJob.HasExited)
                {
                    await Task.Delay(1500, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }

            if (!transcodingJob.HasExited)
            {
                StartThrottler(state, transcodingJob);
            }

            ReportUsage(state);

            return transcodingJob;
        }

        private void StartThrottler(StreamState state, TranscodingJob transcodingJob)
        {
            if (EnableThrottling(state))
            {
                transcodingJob.TranscodingThrottler = state.TranscodingThrottler = new TranscodingThrottler(transcodingJob, Logger, ServerConfigurationManager);
                state.TranscodingThrottler.Start();
            }
        }

        private bool EnableThrottling(StreamState state)
        {
            // do not use throttling with hardware encoders
            return state.InputProtocol == MediaProtocol.File &&
                state.RunTimeTicks.HasValue &&
                state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks &&
                state.IsInputVideo &&
                state.VideoType == VideoType.VideoFile &&
                !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(GetVideoEncoder(state), "libx264", StringComparison.OrdinalIgnoreCase);
        }

        private async Task StartStreamingLog(TranscodingJob transcodingJob, StreamState state, Stream source, Stream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        ParseLogLine(line, transcodingJob, state);

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        await target.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        await target.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Don't spam the log. This doesn't seem to throw in windows, but sometimes under linux
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error reading ffmpeg log", ex);
            }
        }

        private void ParseLogLine(string line, TranscodingJob transcodingJob, StreamState state)
        {
            float? framerate = null;
            double? percent = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;
            int? bitRate = null;

            var parts = line.Split(' ');

            var totalMs = state.RunTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds
                : 0;

            var startMs = state.Request.StartTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.Request.StartTimeTicks.Value).TotalMilliseconds
                : 0;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (string.Equals(part, "fps=", StringComparison.OrdinalIgnoreCase) &&
                    (i + 1 < parts.Length))
                {
                    var rate = parts[i + 1];
                    float val;

                    if (float.TryParse(rate, NumberStyles.Any, UsCulture, out val))
                    {
                        framerate = val;
                    }
                }
                else if (state.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = part.Split(new[] { '=' }, 2).Last();
                    TimeSpan val;

                    if (TimeSpan.TryParse(time, UsCulture, out val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        var percentVal = currentMs / totalMs;
                        percent = 100 * percentVal;

                        transcodingPosition = val;
                    }
                }
                else if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                {
                    var size = part.Split(new[] { '=' }, 2).Last();

                    int? scale = null;
                    if (size.IndexOf("kb", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        scale = 1024;
                        size = size.Replace("kb", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        long val;

                        if (long.TryParse(size, NumberStyles.Any, UsCulture, out val))
                        {
                            bytesTranscoded = val * scale.Value;
                        }
                    }
                }
                else if (part.StartsWith("bitrate=", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = part.Split(new[] { '=' }, 2).Last();

                    int? scale = null;
                    if (rate.IndexOf("kbits/s", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        scale = 1024;
                        rate = rate.Replace("kbits/s", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        float val;

                        if (float.TryParse(rate, NumberStyles.Any, UsCulture, out val))
                        {
                            bitRate = (int)Math.Ceiling(val * scale.Value);
                        }
                    }
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                ApiEntryPoint.Instance.ReportTranscodingProgress(transcodingJob, state, transcodingPosition, framerate, percent, bytesTranscoded, bitRate);
            }
        }

        private int? GetVideoBitrateParamValue(VideoStreamRequest request, MediaStream videoStream, string outputVideoCodec)
        {
            var bitrate = request.VideoBitRate;

            if (videoStream != null)
            {
                var isUpscaling = request.Height.HasValue && videoStream.Height.HasValue &&
                                   request.Height.Value > videoStream.Height.Value;

                if (request.Width.HasValue && videoStream.Width.HasValue &&
                    request.Width.Value > videoStream.Width.Value)
                {
                    isUpscaling = true;
                }

                // Don't allow bitrate increases unless upscaling
                if (!isUpscaling)
                {
                    if (bitrate.HasValue && videoStream.BitRate.HasValue)
                    {
                        bitrate = Math.Min(bitrate.Value, videoStream.BitRate.Value);
                    }
                }
            }

            if (bitrate.HasValue)
            {
                var inputVideoCodec = videoStream == null ? null : videoStream.Codec;
                bitrate = ResolutionNormalizer.ScaleBitrate(bitrate.Value, inputVideoCodec, outputVideoCodec);

                // If a max bitrate was requested, don't let the scaled bitrate exceed it
                if (request.VideoBitRate.HasValue)
                {
                    bitrate = Math.Min(bitrate.Value, request.VideoBitRate.Value);
                }
            }

            return bitrate;
        }

        protected string GetVideoBitrateParam(StreamState state, string videoCodec)
        {
            var bitrate = state.OutputVideoBitrate;

            if (bitrate.HasValue)
            {
                if (string.Equals(videoCodec, "libvpx", StringComparison.OrdinalIgnoreCase))
                {
                    // With vpx when crf is used, b:v becomes a max rate
                    // https://trac.ffmpeg.org/wiki/vpxEncodingGuide. But higher bitrate source files -b:v causes judder so limite the bitrate but dont allow it to "saturate" the bitrate. So dont contrain it down just up.
                    return string.Format(" -maxrate:v {0} -bufsize:v ({0}*2) -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                if (string.Equals(videoCodec, "msmpeg4", StringComparison.OrdinalIgnoreCase))
                {
                    return string.Format(" -b:v {0}", bitrate.Value.ToString(UsCulture));
                }

                // h264
                return string.Format(" -b:v {0} -maxrate {0} -bufsize {1}",
                    bitrate.Value.ToString(UsCulture),
                    (bitrate.Value * 2).ToString(UsCulture));
            }

            return string.Empty;
        }

        private int? GetAudioBitrateParam(StreamRequest request, MediaStream audioStream)
        {
            if (request.AudioBitRate.HasValue)
            {
                // Make sure we don't request a bitrate higher than the source
                var currentBitrate = audioStream == null ? request.AudioBitRate.Value : audioStream.BitRate ?? request.AudioBitRate.Value;

                // Don't encode any higher than this
                return Math.Min(384000, request.AudioBitRate.Value);
                //return Math.Min(currentBitrate, request.AudioBitRate.Value);
            }

            return null;
        }

        /// <summary>
        /// Gets the user agent param.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        private string GetUserAgentParam(StreamState state)
        {
            string useragent = null;

            state.RemoteHttpHeaders.TryGetValue("User-Agent", out useragent);

            if (!string.IsNullOrWhiteSpace(useragent))
            {
                return "-user-agent \"" + useragent + "\"";
            }

            return string.Empty;
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="job">The job.</param>
        /// <param name="state">The state.</param>
        private void OnFfMpegProcessExited(Process process, TranscodingJob job, StreamState state)
        {
            if (job != null)
            {
                job.HasExited = true;
            }

            Logger.Debug("Disposing stream resources");
            state.Dispose();

            try
            {
                Logger.Info("FFMpeg exited with code {0}", process.ExitCode);
            }
            catch
            {
                Logger.Error("FFMpeg exited with an error.");
            }

            // This causes on exited to be called twice:
            //try
            //{
            //    // Dispose the process
            //    process.Dispose();
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorException("Error disposing ffmpeg.", ex);
            //}
        }

        protected double? GetFramerateParam(StreamState state)
        {
            if (state.VideoRequest != null)
            {
                if (state.VideoRequest.Framerate.HasValue)
                {
                    return state.VideoRequest.Framerate.Value;
                }

                var maxrate = state.VideoRequest.MaxFramerate;

                if (maxrate.HasValue && state.VideoStream != null)
                {
                    var contentRate = state.VideoStream.AverageFrameRate ?? state.VideoStream.RealFrameRate;

                    if (contentRate.HasValue && contentRate.Value > maxrate.Value)
                    {
                        return maxrate;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the parameters.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseParams(StreamRequest request)
        {
            var vals = request.Params.Split(';');

            var videoRequest = request as VideoStreamRequest;

            for (var i = 0; i < vals.Length; i++)
            {
                var val = vals[i];

                if (string.IsNullOrWhiteSpace(val))
                {
                    continue;
                }

                if (i == 0)
                {
                    request.DeviceProfileId = val;
                }
                else if (i == 1)
                {
                    request.DeviceId = val;
                }
                else if (i == 2)
                {
                    request.MediaSourceId = val;
                }
                else if (i == 3)
                {
                    request.Static = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                }
                else if (i == 4)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoCodec = val;
                    }
                }
                else if (i == 5)
                {
                    request.AudioCodec = val;
                }
                else if (i == 6)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.AudioStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 7)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.SubtitleStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 8)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoBitRate = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 9)
                {
                    request.AudioBitRate = int.Parse(val, UsCulture);
                }
                else if (i == 10)
                {
                    request.MaxAudioChannels = int.Parse(val, UsCulture);
                }
                else if (i == 11)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxFramerate = float.Parse(val, UsCulture);
                    }
                }
                else if (i == 12)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxWidth = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 13)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxHeight = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 14)
                {
                    request.StartTimeTicks = long.Parse(val, UsCulture);
                }
                else if (i == 15)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.Level = val;
                    }
                }
                else if (i == 16)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxRefFrames = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 17)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxVideoBitDepth = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 18)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.Profile = val;
                    }
                }
                else if (i == 19)
                {
                    // cabac no longer used
                }
                else if (i == 20)
                {
                    request.PlaySessionId = val;
                }
                else if (i == 21)
                {
                    // api_key
                }
                else if (i == 22)
                {
                    request.LiveStreamId = val;
                }
                else if (i == 23)
                {
                    // Duplicating ItemId because of MediaMonkey
                }
                else if (i == 24)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.CopyTimestamps = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (i == 25)
                {
                    if (!string.IsNullOrWhiteSpace(val) && videoRequest != null)
                    {
                        SubtitleDeliveryMethod method;
                        if (Enum.TryParse(val, out method))
                        {
                            videoRequest.SubtitleMethod = method;
                        }
                    }
                }
                else if (i == 26)
                {
                    request.TranscodingMaxAudioChannels = int.Parse(val, UsCulture);
                }
                else if (i == 27)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.EnableSubtitlesInManifest = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (i == 28)
                {
                    request.Tag = val;
                }
                else if (i == 29)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.EnableSplittingOnNonKeyFrames = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        /// <summary>
        /// Parses the dlna headers.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseDlnaHeaders(StreamRequest request)
        {
            if (!request.StartTimeTicks.HasValue)
            {
                var timeSeek = GetHeader("TimeSeekRange.dlna.org");

                request.StartTimeTicks = ParseTimeSeekHeader(timeSeek);
            }
        }

        /// <summary>
        /// Parses the time seek header.
        /// </summary>
        private long? ParseTimeSeekHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value.IndexOf("npt=", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new ArgumentException("Invalid timeseek header");
            }
            value = value.Substring(4).Split(new[] { '-' }, 2)[0];

            if (value.IndexOf(':') == -1)
            {
                // Parses npt times in the format of '417.33'
                double seconds;
                if (double.TryParse(value, NumberStyles.Any, UsCulture, out seconds))
                {
                    return TimeSpan.FromSeconds(seconds).Ticks;
                }

                throw new ArgumentException("Invalid timeseek header");
            }

            // Parses npt times in the format of '10:19:25.7'
            var tokens = value.Split(new[] { ':' }, 3);
            double secondsSum = 0;
            var timeFactor = 3600;

            foreach (var time in tokens)
            {
                double digit;
                if (double.TryParse(time, NumberStyles.Any, UsCulture, out digit))
                {
                    secondsSum += digit * timeFactor;
                }
                else
                {
                    throw new ArgumentException("Invalid timeseek header");
                }
                timeFactor /= 60;
            }
            return TimeSpan.FromSeconds(secondsSum).Ticks;
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>StreamState.</returns>
        protected async Task<StreamState> GetState(StreamRequest request, CancellationToken cancellationToken)
        {
            ParseDlnaHeaders(request);

            if (!string.IsNullOrWhiteSpace(request.Params))
            {
                ParseParams(request);
            }

            var url = Request.PathInfo;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = InferAudioCodec(url);
            }

            var state = new StreamState(MediaSourceManager, Logger)
            {
                Request = request,
                RequestedUrl = url,
                UserAgent = Request.UserAgent
            };

            //if ((Request.UserAgent ?? string.Empty).IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipod", StringComparison.OrdinalIgnoreCase) != -1)
            //{
            //    state.SegmentLength = 6;
            //}

            if (state.VideoRequest != null)
            {
                if (!string.IsNullOrWhiteSpace(state.VideoRequest.VideoCodec))
                {
                    state.SupportedVideoCodecs = state.VideoRequest.VideoCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                    state.VideoRequest.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => MediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            var item = LibraryManager.GetItemById(request.Id);

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            MediaSourceInfo mediaSource = null;
            if (string.IsNullOrWhiteSpace(request.LiveStreamId))
            {
                TranscodingJob currentJob = !string.IsNullOrWhiteSpace(request.PlaySessionId) ?
                    ApiEntryPoint.Instance.GetTranscodingJob(request.PlaySessionId)
                    : null;

                if (currentJob != null)
                {
                    mediaSource = currentJob.MediaSource;
                }

                if (mediaSource == null)
                {
                    var mediaSources = (await MediaSourceManager.GetPlayackMediaSources(request.Id, null, false, new[] { MediaType.Audio, MediaType.Video }, cancellationToken).ConfigureAwait(false)).ToList();

                    mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
                       ? mediaSources.First()
                       : mediaSources.FirstOrDefault(i => string.Equals(i.Id, request.MediaSourceId));

                    if (mediaSource == null && string.Equals(request.Id, request.MediaSourceId, StringComparison.OrdinalIgnoreCase))
                    {
                        mediaSource = mediaSources.First();
                    }
                }
            }
            else
            {
                var liveStreamInfo = await MediaSourceManager.GetLiveStreamWithDirectStreamProvider(request.LiveStreamId, cancellationToken).ConfigureAwait(false);
                mediaSource = liveStreamInfo.Item1;
                state.DirectStreamProvider = liveStreamInfo.Item2;
            }

            var videoRequest = request as VideoStreamRequest;

            AttachMediaSourceInfo(state, mediaSource, videoRequest, url);

            var container = Path.GetExtension(state.RequestedUrl);

            if (string.IsNullOrEmpty(container))
            {
                container = request.Static ?
                    state.InputContainer :
                    (Path.GetExtension(GetOutputFilePath(state)) ?? string.Empty).TrimStart('.');
            }

            state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = GetAudioBitrateParam(state.Request, state.AudioStream);
            state.OutputAudioSampleRate = request.AudioSampleRate;

            state.OutputAudioCodec = state.Request.AudioCodec;

            state.OutputAudioChannels = GetNumAudioChannelsParam(state.Request, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.VideoRequest.VideoCodec;
                state.OutputVideoBitrate = GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream, state.OutputVideoCodec);

                if (state.OutputVideoBitrate.HasValue)
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream == null ? (int?)null : state.VideoStream.BitRate,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream == null ? null : state.VideoStream.Codec,
                        state.OutputVideoCodec,
                        videoRequest.MaxWidth,
                        videoRequest.MaxHeight);

                    videoRequest.MaxWidth = resolution.MaxWidth;
                    videoRequest.MaxHeight = resolution.MaxHeight;
                }
            }

            ApplyDeviceProfileSettings(state);

            if (videoRequest != null)
            {
                TryStreamCopy(state, videoRequest);
            }

            state.OutputFilePath = GetOutputFilePath(state);

            return state;
        }

        private void TryStreamCopy(StreamState state, VideoStreamRequest videoRequest)
        {
            if (state.VideoStream != null && CanStreamCopyVideo(state))
            {
                state.OutputVideoCodec = "copy";
            }
            else
            {
                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                var auth = AuthorizationContext.GetAuthorizationInfo(Request);
                if (!string.IsNullOrWhiteSpace(auth.UserId))
                {
                    var user = UserManager.GetUserById(auth.UserId);
                    if (!user.Policy.EnableVideoPlaybackTranscoding)
                    {
                        state.OutputVideoCodec = "copy";
                    }
                }
            }

            if (state.AudioStream != null && CanStreamCopyAudio(state, state.SupportedAudioCodecs))
            {
                state.OutputAudioCodec = "copy";
            }
            else
            {
                // If the user doesn't have access to transcoding, then force stream copy, regardless of whether it will be compatible or not
                var auth = AuthorizationContext.GetAuthorizationInfo(Request);
                if (!string.IsNullOrWhiteSpace(auth.UserId))
                {
                    var user = UserManager.GetUserById(auth.UserId);
                    if (!user.Policy.EnableAudioPlaybackTranscoding)
                    {
                        state.OutputAudioCodec = "copy";
                    }
                }
            }
        }

        private void AttachMediaSourceInfo(StreamState state,
          MediaSourceInfo mediaSource,
          VideoStreamRequest videoRequest,
          string requestedUrl)
        {
            state.MediaPath = mediaSource.Path;
            state.InputProtocol = mediaSource.Protocol;
            state.InputContainer = mediaSource.Container;
            state.InputFileSize = mediaSource.Size;
            state.InputBitrate = mediaSource.Bitrate;
            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;

            if (mediaSource.VideoType.HasValue)
            {
                state.VideoType = mediaSource.VideoType.Value;
            }

            state.IsoType = mediaSource.IsoType;

            state.PlayableStreamFileNames = mediaSource.PlayableStreamFileNames.ToList();

            if (mediaSource.Timestamp.HasValue)
            {
                state.InputTimestamp = mediaSource.Timestamp.Value;
            }

            state.InputProtocol = mediaSource.Protocol;
            state.MediaPath = mediaSource.Path;
            state.RunTimeTicks = mediaSource.RunTimeTicks;
            state.RemoteHttpHeaders = mediaSource.RequiredHttpHeaders;
            state.InputBitrate = mediaSource.Bitrate;
            state.InputFileSize = mediaSource.Size;
            state.ReadInputAtNativeFramerate = mediaSource.ReadAtNativeFramerate;

            if (state.ReadInputAtNativeFramerate ||
                mediaSource.Protocol == MediaProtocol.File && string.Equals(mediaSource.Container, "wtv", StringComparison.OrdinalIgnoreCase))
            {
                state.OutputAudioSync = "1000";
                state.InputVideoSync = "-1";
                state.InputAudioSync = "1";
            }

            if (string.Equals(mediaSource.Container, "wma", StringComparison.OrdinalIgnoreCase))
            {
                // Seeing some stuttering when transcoding wma to audio-only HLS
                state.InputAudioSync = "1";
            }

            var mediaStreams = mediaSource.MediaStreams;

            if (videoRequest != null)
            {
                if (string.IsNullOrEmpty(videoRequest.VideoCodec))
                {
                    videoRequest.VideoCodec = InferVideoCodec(requestedUrl);
                }

                state.VideoStream = GetMediaStream(mediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video);
                state.SubtitleStream = GetMediaStream(mediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
                state.AudioStream = GetMediaStream(mediaStreams, videoRequest.AudioStreamIndex, MediaStreamType.Audio);

                if (state.SubtitleStream != null && !state.SubtitleStream.IsExternal)
                {
                    state.InternalSubtitleStreamOffset = mediaStreams.Where(i => i.Type == MediaStreamType.Subtitle && !i.IsExternal).ToList().IndexOf(state.SubtitleStream);
                }

                if (state.VideoStream != null && state.VideoStream.IsInterlaced)
                {
                    state.DeInterlace = true;
                }

                EnforceResolutionLimit(state, videoRequest);
            }
            else
            {
                state.AudioStream = GetMediaStream(mediaStreams, null, MediaStreamType.Audio, true);
            }

            state.MediaSource = mediaSource;
        }

        protected virtual bool CanStreamCopyVideo(StreamState state)
        {
            var request = state.VideoRequest;
            var videoStream = state.VideoStream;

            if (videoStream.IsInterlaced)
            {
                return false;
            }

            if (videoStream.IsAnamorphic ?? false)
            {
                return false;
            }

            // Can't stream copy if we're burning in subtitles
            if (request.SubtitleStreamIndex.HasValue)
            {
                if (request.SubtitleMethod == SubtitleDeliveryMethod.Encode)
                {
                    return false;
                }
            }

            if (string.Equals("h264", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
            {
                if (videoStream.IsAVC.HasValue && !videoStream.IsAVC.Value)
                {
                    Logger.Debug("Cannot stream copy video. Stream is marked as not AVC");
                    return false;
                }
            }

            // Source and target codecs must match
            if (string.IsNullOrEmpty(videoStream.Codec) || !state.SupportedVideoCodecs.Contains(videoStream.Codec, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // If client is requesting a specific video profile, it must match the source
            if (!string.IsNullOrEmpty(request.Profile))
            {
                if (string.IsNullOrEmpty(videoStream.Profile))
                {
                    //return false;
                }

                if (!string.IsNullOrEmpty(videoStream.Profile) && !string.Equals(request.Profile, videoStream.Profile, StringComparison.OrdinalIgnoreCase))
                {
                    var currentScore = GetVideoProfileScore(videoStream.Profile);
                    var requestedScore = GetVideoProfileScore(request.Profile);

                    if (currentScore == -1 || currentScore > requestedScore)
                    {
                        return false;
                    }
                }
            }

            // Video width must fall within requested value
            if (request.MaxWidth.HasValue)
            {
                if (!videoStream.Width.HasValue || videoStream.Width.Value > request.MaxWidth.Value)
                {
                    return false;
                }
            }

            // Video height must fall within requested value
            if (request.MaxHeight.HasValue)
            {
                if (!videoStream.Height.HasValue || videoStream.Height.Value > request.MaxHeight.Value)
                {
                    return false;
                }
            }

            // Video framerate must fall within requested value
            var requestedFramerate = request.MaxFramerate ?? request.Framerate;
            if (requestedFramerate.HasValue)
            {
                var videoFrameRate = videoStream.AverageFrameRate ?? videoStream.RealFrameRate;

                if (!videoFrameRate.HasValue || videoFrameRate.Value > requestedFramerate.Value)
                {
                    return false;
                }
            }

            // Video bitrate must fall within requested value
            if (request.VideoBitRate.HasValue)
            {
                if (!videoStream.BitRate.HasValue || videoStream.BitRate.Value > request.VideoBitRate.Value)
                {
                    return false;
                }
            }

            if (request.MaxVideoBitDepth.HasValue)
            {
                if (videoStream.BitDepth.HasValue && videoStream.BitDepth.Value > request.MaxVideoBitDepth.Value)
                {
                    return false;
                }
            }

            if (request.MaxRefFrames.HasValue)
            {
                if (videoStream.RefFrames.HasValue && videoStream.RefFrames.Value > request.MaxRefFrames.Value)
                {
                    return false;
                }
            }

            // If a specific level was requested, the source must match or be less than
            if (!string.IsNullOrEmpty(request.Level))
            {
                double requestLevel;

                if (double.TryParse(request.Level, NumberStyles.Any, UsCulture, out requestLevel))
                {
                    if (!videoStream.Level.HasValue)
                    {
                        //return false;
                    }

                    if (videoStream.Level.HasValue && videoStream.Level.Value > requestLevel)
                    {
                        return false;
                    }
                }
            }

            return request.EnableAutoStreamCopy;
        }

        private int GetVideoProfileScore(string profile)
        {
            var list = new List<string>
            {
                "Constrained Baseline",
                "Baseline",
                "Extended",
                "Main",
                "High",
                "Progressive High",
                "Constrained High"
            };

            return Array.FindIndex(list.ToArray(), t => string.Equals(t, profile, StringComparison.OrdinalIgnoreCase));
        }

        protected virtual bool CanStreamCopyAudio(StreamState state, List<string> supportedAudioCodecs)
        {
            var request = state.VideoRequest;
            var audioStream = state.AudioStream;

            // Source and target codecs must match
            if (string.IsNullOrEmpty(audioStream.Codec) || !supportedAudioCodecs.Contains(audioStream.Codec, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // Video bitrate must fall within requested value
            if (request.AudioBitRate.HasValue)
            {
                if (!audioStream.BitRate.HasValue || audioStream.BitRate.Value <= 0)
                {
                    return false;
                }
                if (audioStream.BitRate.Value > request.AudioBitRate.Value)
                {
                    return false;
                }
            }

            // Channels must fall within requested value
            var channels = request.AudioChannels ?? request.MaxAudioChannels;
            if (channels.HasValue)
            {
                if (!audioStream.Channels.HasValue || audioStream.Channels.Value <= 0)
                {
                    return false;
                }
                if (audioStream.Channels.Value > channels.Value)
                {
                    return false;
                }
            }

            // Sample rate must fall within requested value
            if (request.AudioSampleRate.HasValue)
            {
                if (!audioStream.SampleRate.HasValue || audioStream.SampleRate.Value <= 0)
                {
                    return false;
                }
                if (audioStream.SampleRate.Value > request.AudioSampleRate.Value)
                {
                    return false;
                }
            }

            return request.EnableAutoStreamCopy;
        }

        private void ApplyDeviceProfileSettings(StreamState state)
        {
            var headers = new Dictionary<string, string>();
            foreach (var key in Request.Headers.AllKeys)
            {
                headers[key] = Request.Headers[key];
            }

            if (!string.IsNullOrWhiteSpace(state.Request.DeviceProfileId))
            {
                state.DeviceProfile = DlnaManager.GetProfile(state.Request.DeviceProfileId);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
                {
                    var caps = DeviceManager.GetCapabilities(state.Request.DeviceId);

                    if (caps != null)
                    {
                        state.DeviceProfile = caps.DeviceProfile;
                    }
                    else
                    {
                        state.DeviceProfile = DlnaManager.GetProfile(headers);
                    }
                }
            }

            var profile = state.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var audioCodec = state.ActualOutputAudioCodec;
            var videoCodec = state.ActualOutputVideoCodec;

            var mediaProfile = state.VideoRequest == null ?
                profile.GetAudioMediaProfile(state.OutputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate) :
                profile.GetVideoMediaProfile(state.OutputContainer,
                audioCodec,
                videoCodec,
                state.OutputWidth,
                state.OutputHeight,
                state.TargetVideoBitDepth,
                state.OutputVideoBitrate,
                state.TargetVideoProfile,
                state.TargetVideoLevel,
                state.TargetFramerate,
                state.TargetPacketLength,
                state.TargetTimestamp,
                state.IsTargetAnamorphic,
                state.TargetRefFrames,
                state.TargetVideoStreamCount,
                state.TargetAudioStreamCount,
                state.TargetVideoCodecTag,
                state.IsTargetAVC);

            if (mediaProfile != null)
            {
                state.MimeType = mediaProfile.MimeType;
            }

            if (!state.Request.Static)
            {
                var transcodingProfile = state.VideoRequest == null ?
                    profile.GetAudioTranscodingProfile(state.OutputContainer, audioCodec) :
                    profile.GetVideoTranscodingProfile(state.OutputContainer, audioCodec, videoCodec);

                if (transcodingProfile != null)
                {
                    state.EstimateContentLength = transcodingProfile.EstimateContentLength;
                    state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                    state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                    if (state.VideoRequest != null)
                    {
                        state.VideoRequest.CopyTimestamps = transcodingProfile.CopyTimestamps;
                        state.VideoRequest.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
                        state.VideoRequest.EnableSplittingOnNonKeyFrames = transcodingProfile.EnableSplittingOnNonKeyFrames;
                    }
                }
            }
        }

        private async void ReportUsage(StreamState state)
        {
            try
            {
                await ReportUsageInternal(state).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private Task ReportUsageInternal(StreamState state)
        {
            if (!ServerConfigurationManager.Configuration.EnableAnonymousUsageReporting)
            {
                return Task.FromResult(true);
            }

            if (!MediaEncoder.IsDefaultEncoderPath)
            {
                return Task.FromResult(true);
            }

            var dict = new Dictionary<string, string>();

            var outputAudio = GetAudioEncoder(state);
            if (!string.IsNullOrWhiteSpace(outputAudio))
            {
                dict["outputAudio"] = outputAudio;
            }

            var outputVideo = GetVideoEncoder(state);
            if (!string.IsNullOrWhiteSpace(outputVideo))
            {
                dict["outputVideo"] = outputVideo;
            }

            if (ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputAudio ?? string.Empty, StringComparer.OrdinalIgnoreCase) &&
                ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputVideo ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return Task.FromResult(true);
            }

            dict["id"] = AppHost.SystemId;
            dict["type"] = state.VideoRequest == null ? "Audio" : "Video";

            var audioStream = state.AudioStream;
            if (audioStream != null && !string.IsNullOrWhiteSpace(audioStream.Codec))
            {
                dict["inputAudio"] = audioStream.Codec;
            }

            var videoStream = state.VideoStream;
            if (videoStream != null && !string.IsNullOrWhiteSpace(videoStream.Codec))
            {
                dict["inputVideo"] = videoStream.Codec;
            }

            var cert = GetType().Assembly.GetModules().First().GetSignerCertificate();
            if (cert != null)
            {
                dict["assemblySig"] = cert.GetCertHashString();
                dict["certSubject"] = cert.Subject ?? string.Empty;
                dict["certIssuer"] = cert.Issuer ?? string.Empty;
            }
            else
            {
                return Task.FromResult(true);
            }

            if (state.SupportedAudioCodecs.Count > 0)
            {
                dict["supportedAudioCodecs"] = string.Join(",", state.SupportedAudioCodecs.ToArray());
            }

            var auth = AuthorizationContext.GetAuthorizationInfo(Request);

            dict["appName"] = auth.Client ?? string.Empty;
            dict["appVersion"] = auth.Version ?? string.Empty;
            dict["device"] = auth.Device ?? string.Empty;
            dict["deviceId"] = auth.DeviceId ?? string.Empty;
            dict["context"] = "streaming";

            //Logger.Info(JsonSerializer.SerializeToString(dict));
            if (!ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputAudio ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                var list = ServerConfigurationManager.Configuration.CodecsUsed.ToList();
                list.Add(outputAudio);
                ServerConfigurationManager.Configuration.CodecsUsed = list.ToArray();
            }

            if (!ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputVideo ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                var list = ServerConfigurationManager.Configuration.CodecsUsed.ToList();
                list.Add(outputVideo);
                ServerConfigurationManager.Configuration.CodecsUsed = list.ToArray();
            }

            ServerConfigurationManager.SaveConfiguration();

            //Logger.Info(JsonSerializer.SerializeToString(dict));
            var options = new HttpRequestOptions()
            {
                Url = "https://mb3admin.com/admin/service/transcoding/report",
                CancellationToken = CancellationToken.None,
                LogRequest = false,
                LogErrors = false,
                BufferContent = false
            };
            options.RequestContent = JsonSerializer.SerializeToString(dict);
            options.RequestContentType = "application/json";

            return HttpClient.Post(options);
        }

        /// <summary>
        /// Adds the dlna headers.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isStaticallyStreamed">if set to <c>true</c> [is statically streamed].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected void AddDlnaHeaders(StreamState state, IDictionary<string, string> responseHeaders, bool isStaticallyStreamed)
        {
            var profile = state.DeviceProfile;

            var transferMode = GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;
            responseHeaders["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            if (string.Equals(GetHeader("getMediaInfo.sec"), "1", StringComparison.OrdinalIgnoreCase))
            {
                if (state.RunTimeTicks.HasValue)
                {
                    var ms = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds;
                    responseHeaders["MediaInfo.sec"] = string.Format("SEC_Duration={0};", Convert.ToInt32(ms).ToString(CultureInfo.InvariantCulture));
                }
            }

            if (state.RunTimeTicks.HasValue && !isStaticallyStreamed && profile != null)
            {
                AddTimeSeekResponseHeaders(state, responseHeaders);
            }

            if (profile == null)
            {
                profile = DlnaManager.GetDefaultProfile();
            }

            var audioCodec = state.ActualOutputAudioCodec;

            if (state.VideoRequest == null)
            {
                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildAudioHeader(
                    state.OutputContainer,
                    audioCodec,
                    state.OutputAudioBitrate,
                    state.OutputAudioSampleRate,
                    state.OutputAudioChannels,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TranscodeSeekInfo
                    );
            }
            else
            {
                var videoCodec = state.ActualOutputVideoCodec;

                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(
                    state.OutputContainer,
                    videoCodec,
                    audioCodec,
                    state.OutputWidth,
                    state.OutputHeight,
                    state.TargetVideoBitDepth,
                    state.OutputVideoBitrate,
                    state.TargetTimestamp,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TargetVideoProfile,
                    state.TargetVideoLevel,
                    state.TargetFramerate,
                    state.TargetPacketLength,
                    state.TranscodeSeekInfo,
                    state.IsTargetAnamorphic,
                    state.TargetRefFrames,
                    state.TargetVideoStreamCount,
                    state.TargetAudioStreamCount,
                    state.TargetVideoCodecTag,
                    state.IsTargetAVC

                    ).FirstOrDefault() ?? string.Empty;
            }

            foreach (var item in responseHeaders)
            {
                Request.Response.AddHeader(item.Key, item.Value);
            }
        }

        private void AddTimeSeekResponseHeaders(StreamState state, IDictionary<string, string> responseHeaders)
        {
            var runtimeSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds.ToString(UsCulture);
            var startSeconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds.ToString(UsCulture);

            responseHeaders["TimeSeekRange.dlna.org"] = string.Format("npt={0}-{1}/{1}", startSeconds, runtimeSeconds);
            responseHeaders["X-AvailableSeekRange"] = string.Format("1 npt={0}-{1}", startSeconds, runtimeSeconds);
        }

        /// <summary>
        /// Enforces the resolution limit.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="videoRequest">The video request.</param>
        private void EnforceResolutionLimit(StreamState state, VideoStreamRequest videoRequest)
        {
            // Switch the incoming params to be ceilings rather than fixed values
            videoRequest.MaxWidth = videoRequest.MaxWidth ?? videoRequest.Width;
            videoRequest.MaxHeight = videoRequest.MaxHeight ?? videoRequest.Height;

            videoRequest.Width = null;
            videoRequest.Height = null;
        }

        protected string GetInputModifier(StreamState state, bool genPts = true)
        {
            var inputModifier = string.Empty;

            var probeSize = GetProbeSizeArgument(state);
            inputModifier += " " + probeSize;
            inputModifier = inputModifier.Trim();

            var userAgentParam = GetUserAgentParam(state);

            if (!string.IsNullOrWhiteSpace(userAgentParam))
            {
                inputModifier += " " + userAgentParam;
            }

            inputModifier = inputModifier.Trim();

            inputModifier += " " + GetFastSeekCommandLineParameter(state.Request);
            inputModifier = inputModifier.Trim();

            //inputModifier += " -fflags +genpts+ignidx+igndts";
            if (state.VideoRequest != null && genPts)
            {
                inputModifier += " -fflags +genpts";
            }

            if (!string.IsNullOrEmpty(state.InputAudioSync))
            {
                inputModifier += " -async " + state.InputAudioSync;
            }

            if (!string.IsNullOrEmpty(state.InputVideoSync))
            {
                inputModifier += " -vsync " + state.InputVideoSync;
            }

            if (state.ReadInputAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            var videoDecoder = GetVideoDecoder(state);
            if (!string.IsNullOrWhiteSpace(videoDecoder))
            {
                inputModifier += " " + videoDecoder;
            }

            if (state.VideoRequest != null)
            {
                // Important: If this is ever re-enabled, make sure not to use it with wtv because it breaks seeking
                if (string.Equals(state.OutputContainer, "mkv", StringComparison.OrdinalIgnoreCase) && state.VideoRequest.CopyTimestamps)
                {
                    //inputModifier += " -noaccurate_seek";
                }
            }

            return inputModifier;
        }

        /// <summary>
        /// Infers the audio codec based on the url
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{AudioCodecs}.</returns>
        private string InferAudioCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return "mp3";
            }
            if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                return "aac";
            }
            if (string.Equals(ext, ".wma", StringComparison.OrdinalIgnoreCase))
            {
                return "wma";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".oga", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }
            if (string.Equals(ext, ".webma", StringComparison.OrdinalIgnoreCase))
            {
                return "vorbis";
            }

            return "copy";
        }

        /// <summary>
        /// Infers the video codec.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.Nullable{VideoCodecs}.</returns>
        private string InferVideoCodec(string url)
        {
            var ext = Path.GetExtension(url);

            if (string.Equals(ext, ".asf", StringComparison.OrdinalIgnoreCase))
            {
                return "wmv";
            }
            if (string.Equals(ext, ".webm", StringComparison.OrdinalIgnoreCase))
            {
                return "vpx";
            }
            if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ogv", StringComparison.OrdinalIgnoreCase))
            {
                return "theora";
            }
            if (string.Equals(ext, ".m3u8", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".ts", StringComparison.OrdinalIgnoreCase))
            {
                return "h264";
            }

            return "copy";
        }
    }
}
