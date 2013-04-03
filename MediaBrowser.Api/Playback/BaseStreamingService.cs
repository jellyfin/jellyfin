using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        protected IServerApplicationPaths ApplicationPaths { get; set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        protected IUserManager UserManager { get; set; }

        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        protected ILibraryManager LibraryManager { get; set; }

        /// <summary>
        /// Gets or sets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        protected IIsoManager IsoManager { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        protected BaseStreamingService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager)
        {
            ApplicationPaths = appPaths;
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, StreamState state);

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
            return Path.GetExtension(state.Url);
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected string GetOutputFilePath(StreamState state)
        {
            var folder = ApplicationPaths.EncodedMediaCachePath;
            return Path.Combine(folder, GetCommandLineArguments("dummy\\dummy", state).GetMD5() + GetOutputFileExtension(state).ToLower());
        }

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
                    return string.Format("-ss {0}", seconds);
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
                    return string.Format(" -ss {0}", FastSeekOffsetSeconds);
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
            var streams = allStream.Where(s => s.Type == type).ToList();

            if (desiredIndex.HasValue)
            {
                var stream = streams.FirstOrDefault(s => s.Index == desiredIndex.Value);

                if (stream != null)
                {
                    return stream;
                }
            }

            // Just return the first one
            return returnFirstIfNoIndex ? streams.FirstOrDefault() : null;
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetOutputSizeParam(StreamState state, string outputVideoCodec)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var assSubtitleParam = string.Empty;

            var request = state.VideoRequest;

            if (state.SubtitleStream != null)
            {
                if (state.SubtitleStream.Codec.IndexOf("srt", StringComparison.OrdinalIgnoreCase) != -1 || state.SubtitleStream.Codec.IndexOf("subrip", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    assSubtitleParam = GetTextSubtitleParam((Video)state.Item, state.SubtitleStream, request.StartTimeTicks);
                }
            }

            // If fixed dimensions were supplied
            if (request.Width.HasValue && request.Height.HasValue)
            {
                return string.Format(" -vf \"scale={0}:{1}{2}\"", request.Width.Value, request.Height.Value, assSubtitleParam);
            }

            var isH264Output = outputVideoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase);

            // If a fixed width was requested
            if (request.Width.HasValue)
            {
                return isH264Output ?
                    string.Format(" -vf \"scale={0}:trunc(ow/a/2)*2{1}\"", request.Width.Value, assSubtitleParam) :
                    string.Format(" -vf \"scale={0}:-1{1}\"", request.Width.Value, assSubtitleParam);
            }

            // If a max width was requested
            if (request.MaxWidth.HasValue && !request.MaxHeight.HasValue)
            {
                return isH264Output ?
                    string.Format(" -vf \"scale=min(iw\\,{0}):trunc(ow/a/2)*2{1}\"", request.MaxWidth.Value, assSubtitleParam) :
                    string.Format(" -vf \"scale=min(iw\\,{0}):-1{1}\"", request.MaxWidth.Value, assSubtitleParam);
            }

            // Need to perform calculations manually

            // Try to account for bad media info
            var currentHeight = state.VideoStream.Height ?? request.MaxHeight ?? request.Height ?? 0;
            var currentWidth = state.VideoStream.Width ?? request.MaxWidth ?? request.Width ?? 0;

            var outputSize = DrawingUtils.Resize(currentWidth, currentHeight, request.Width, request.Height, request.MaxWidth, request.MaxHeight);

            // If we're encoding with libx264, it can't handle odd numbered widths or heights, so we'll have to fix that
            if (isH264Output)
            {
                return string.Format(" -vf \"scale=trunc({0}/2)*2:trunc({1}/2)*2{2}\"", outputSize.Width, outputSize.Height, assSubtitleParam);
            }

            // Otherwise use -vf scale since ffmpeg will ensure internally that the aspect ratio is preserved
            return string.Format(" -vf \"scale={0}:-1{1}\"", Convert.ToInt32(outputSize.Width), assSubtitleParam);
        }

        /// <summary>
        /// Gets the text subtitle param.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(Video video, MediaStream subtitleStream, long? startTimeTicks)
        {
            var path = subtitleStream.IsExternal ? GetConvertedAssPath(video, subtitleStream) : GetExtractedAssPath(video, subtitleStream);

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var param = string.Format(",ass={0}", path.Replace('\\', '/').Replace(":/", "\\:/"));

            if (startTimeTicks.HasValue)
            {
                var seconds = Convert.ToInt32(TimeSpan.FromTicks(startTimeTicks.Value).TotalSeconds);
                param += string.Format(",setpts=PTS-{0}/TB", seconds);
            }

            return param;
        }

        /// <summary>
        /// Gets the extracted ass path.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <returns>System.String.</returns>
        private string GetExtractedAssPath(Video video, MediaStream subtitleStream)
        {
            var path = Kernel.Instance.FFMpegManager.GetSubtitleCachePath(video, subtitleStream.Index, ".ass");

            if (!File.Exists(path))
            {
                var success = Kernel.Instance.FFMpegManager.ExtractTextSubtitle(video, subtitleStream.Index, path, CancellationToken.None).Result;

                if (!success)
                {
                    return null;
                }
            }

            return path;
        }

        /// <summary>
        /// Gets the converted ass path.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <returns>System.String.</returns>
        private string GetConvertedAssPath(Video video, MediaStream subtitleStream)
        {
            var path = Kernel.Instance.FFMpegManager.GetSubtitleCachePath(video, subtitleStream.Index, ".ass");

            if (!File.Exists(path))
            {
                var success = Kernel.Instance.FFMpegManager.ConvertTextSubtitle(subtitleStream, path, CancellationToken.None).Result;

                if (!success)
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
                outputSizeParam = GetOutputSizeParam(state, outputVideoCodec).TrimEnd('"');
                outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
            }

            return string.Format(" -filter_complex \"[0:{0}]format=yuva444p,lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:0] [sub] overlay{1}\"", state.SubtitleStream.Index, outputSizeParam);
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="audioStream">The audio stream.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetNumAudioChannelsParam(StreamRequest request, MediaStream audioStream)
        {
            if (audioStream.Channels > 2 && request.AudioCodec.HasValue)
            {
                if (request.AudioCodec.Value == AudioCodecs.Aac)
                {
                    // libvo_aacenc currently only supports two channel output
                    return 2;
                }
                if (request.AudioCodec.Value == AudioCodecs.Wma)
                {
                    // wmav2 currently only supports two channel output
                    return 2;
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
                    return "libvo_aacenc";
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
            }

            return "copy";
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        protected string GetInputArgument(BaseItem item, IIsoMount isoMount)
        {
            return isoMount == null ?
                Kernel.Instance.FFMpegManager.GetInputArgument(item) :
                Kernel.Instance.FFMpegManager.GetInputArgument(item as Video, isoMount);
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        /// <returns>Task.</returns>
        protected async Task StartFFMpeg(StreamState state, string outputPath)
        {
            var video = state.Item as Video;

            if (video != null && video.VideoType == VideoType.Iso && video.IsoType.HasValue && IsoManager.CanMount(video.Path))
            {
                state.IsoMount = await IsoManager.Mount(video.Path, CancellationToken.None).ConfigureAwait(false);
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

                    FileName = Kernel.Instance.FFMpegManager.FFMpegPath,
                    WorkingDirectory = Path.GetDirectoryName(Kernel.Instance.FFMpegManager.FFMpegPath),
                    Arguments = GetCommandLineArguments(outputPath, state),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath, TranscodingJobType, process);

            Logger.Info(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            var logFilePath = Path.Combine(ApplicationPaths.LogDirectoryPath, "ffmpeg-" + Guid.NewGuid() + ".txt");

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            state.LogFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);

            process.Exited += (sender, args) => OnFFMpegProcessExited(process, state);

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
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
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="state">The state.</param>
        protected void OnFFMpegProcessExited(Process process, StreamState state)
        {
            if (state.IsoMount != null)
            {
                state.IsoMount.Dispose();
                state.IsoMount = null;
            }

            var outputFilePath = GetOutputFilePath(state);

            state.LogFileStream.Dispose();

            int? exitCode = null;

            try
            {
                exitCode = process.ExitCode;
                Logger.Info("FFMpeg exited with code {0} for {1}", exitCode.Value, outputFilePath);
            }
            catch
            {
                Logger.Info("FFMpeg exited with an error for {0}", outputFilePath);
            }

            process.Dispose();

            ApiEntryPoint.Instance.OnTranscodingFinished(outputFilePath, TranscodingJobType);

            if (!exitCode.HasValue || exitCode.Value != 0)
            {
                Logger.Info("Deleting partial stream file(s) {0}", outputFilePath);

                try
                {
                    DeletePartialStreamFiles(outputFilePath);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, outputFilePath);
                }
            }
            else
            {
                Logger.Info("FFMpeg completed and exited normally for {0}", outputFilePath);
            }
        }

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        protected abstract void DeletePartialStreamFiles(string outputFilePath);

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>StreamState.</returns>
        protected StreamState GetState(StreamRequest request)
        {
            var item = DtoBuilder.GetItemByClientId(request.Id, UserManager, LibraryManager);

            var media = (IHasMediaStreams)item;

            var url = RequestContext.PathInfo;

            if (!request.AudioCodec.HasValue)
            {
                request.AudioCodec = InferAudioCodec(url);
            }

            var state = new StreamState
            {
                Item = item,
                Request = request,
                Url = url
            };

            var videoRequest = request as VideoStreamRequest;

            if (videoRequest != null)
            {
                if (!videoRequest.VideoCodec.HasValue)
                {
                    videoRequest.VideoCodec = InferVideoCodec(url);
                }

                state.VideoStream = GetMediaStream(media.MediaStreams, videoRequest.VideoStreamIndex, MediaStreamType.Video, true);
                state.SubtitleStream = GetMediaStream(media.MediaStreams, videoRequest.SubtitleStreamIndex, MediaStreamType.Subtitle, false);
            }

            state.AudioStream = GetMediaStream(media.MediaStreams, null, MediaStreamType.Audio, true);
            
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

            return null;
        }
    }
}
