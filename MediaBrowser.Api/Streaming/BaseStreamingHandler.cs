using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Represents a common base class for both progressive and hls streaming
    /// </summary>
    /// <typeparam name="TBaseItemType">The type of the T base item type.</typeparam>
    public abstract class BaseStreamingHandler<TBaseItemType> : BaseHandler<Kernel>
        where TBaseItemType : BaseItem, IHasMediaStreams, new()
    {
        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, IIsoMount isoMount);

        /// <summary>
        /// Gets or sets the log file stream.
        /// </summary>
        /// <value>The log file stream.</value>
        protected Stream LogFileStream { get; set; }

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected abstract TranscodingJobType TranscodingJobType { get; }

        /// <summary>
        /// Gets the output file extension.
        /// </summary>
        /// <value>The output file extension.</value>
        protected string OutputFileExtension
        {
            get
            {
                return Path.GetExtension(HttpListenerContext.Request.Url.LocalPath);
            }
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        /// <value>The output file path.</value>
        protected string OutputFilePath
        {
            get
            {
                return Path.Combine(Kernel.ApplicationPaths.FFMpegStreamCachePath, GetCommandLineArguments("dummy\\dummy", null).GetMD5() + OutputFileExtension.ToLower());
            }
        }

        /// <summary>
        /// Gets the audio codec to endoce to.
        /// </summary>
        /// <value>The audio encoding format.</value>
        protected virtual AudioCodecs? AudioCodec
        {
            get
            {
                if (string.IsNullOrEmpty(QueryString["audioCodec"]))
                {
                    return null;
                }

                return (AudioCodecs)Enum.Parse(typeof(AudioCodecs), QueryString["audioCodec"], true);
            }
        }

        /// <summary>
        /// Gets the video encoding codec.
        /// </summary>
        /// <value>The video codec.</value>
        protected VideoCodecs? VideoCodec
        {
            get
            {
                if (string.IsNullOrEmpty(QueryString["videoCodec"]))
                {
                    return null;
                }

                return (VideoCodecs)Enum.Parse(typeof(VideoCodecs), QueryString["videoCodec"], true);
            }
        }

        /// <summary>
        /// Gets the time, in ticks, in which playback should start
        /// </summary>
        /// <value>The start time ticks.</value>
        protected long? StartTimeTicks
        {
            get
            {
                string val = QueryString["StartTimeTicks"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return long.Parse(val);
            }
        }

        /// <summary>
        /// The fast seek offset seconds
        /// </summary>
        private const int FastSeekOffsetSeconds = 1;

        /// <summary>
        /// Gets the fast seek command line parameter.
        /// </summary>
        /// <value>The fast seek command line parameter.</value>
        protected string FastSeekCommandLineParameter
        {
            get
            {
                var time = StartTimeTicks;

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
        }

        /// <summary>
        /// Gets the slow seek command line parameter.
        /// </summary>
        /// <value>The slow seek command line parameter.</value>
        protected string SlowSeekCommandLineParameter
        {
            get
            {
                var time = StartTimeTicks;

                if (time.HasValue)
                {
                    if (TimeSpan.FromTicks(time.Value).TotalSeconds - FastSeekOffsetSeconds > 0)
                    {
                        return string.Format(" -ss {0}", FastSeekOffsetSeconds);
                    }
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the map args.
        /// </summary>
        /// <value>The map args.</value>
        protected virtual string MapArgs
        {
            get
            {
                var args = string.Empty;

                if (VideoStream != null)
                {
                    args += string.Format("-map 0:{0}", VideoStream.Index);
                }
                else
                {
                    args += "-map -0:v";
                }

                if (AudioStream != null)
                {
                    args += string.Format(" -map 0:{0}", AudioStream.Index);
                }
                else
                {
                    args += " -map -0:a";
                }

                if (SubtitleStream == null)
                {
                    args += " -map -0:s";
                }

                return args;
            }
        }

        /// <summary>
        /// The _library item
        /// </summary>
        private TBaseItemType _libraryItem;
        /// <summary>
        /// Gets the library item that will be played, if any
        /// </summary>
        /// <value>The library item.</value>
        protected TBaseItemType LibraryItem
        {
            get
            {
                return _libraryItem ?? (_libraryItem = (TBaseItemType)DtoBuilder.GetItemByClientId(QueryString["id"]));
            }
        }

        /// <summary>
        /// Gets or sets the iso mount.
        /// </summary>
        /// <value>The iso mount.</value>
        private IIsoMount IsoMount { get; set; }

        /// <summary>
        /// The _audio stream
        /// </summary>
        private MediaStream _audioStream;
        /// <summary>
        /// Gets the audio stream.
        /// </summary>
        /// <value>The audio stream.</value>
        protected MediaStream AudioStream
        {
            get { return _audioStream ?? (_audioStream = GetMediaStream(AudioStreamIndex, MediaStreamType.Audio)); }
        }

        /// <summary>
        /// The _video stream
        /// </summary>
        private MediaStream _videoStream;
        /// <summary>
        /// Gets the video stream.
        /// </summary>
        /// <value>The video stream.</value>
        protected MediaStream VideoStream
        {
            get
            {
                // No video streams here
                // Need to make this check to make sure we don't pickup embedded image streams (which are listed in the file as type video)
                if (LibraryItem is Audio)
                {
                    return null;
                }

                return _videoStream ?? (_videoStream = GetMediaStream(VideoStreamIndex, MediaStreamType.Video));
            }
        }

        /// <summary>
        /// The subtitle stream
        /// </summary>
        private MediaStream _subtitleStream;
        /// <summary>
        /// Gets the subtitle stream.
        /// </summary>
        /// <value>The subtitle stream.</value>
        protected MediaStream SubtitleStream
        {
            get
            {
                // No subtitle streams here
                if (LibraryItem is Audio)
                {
                    return null;
                }

                return _subtitleStream ?? (_subtitleStream = GetMediaStream(SubtitleStreamIndex, MediaStreamType.Subtitle, false));
            }
        }

        /// <summary>
        /// Determines which stream will be used for playback
        /// </summary>
        /// <param name="desiredIndex">Index of the desired.</param>
        /// <param name="type">The type.</param>
        /// <param name="returnFirstIfNoIndex">if set to <c>true</c> [return first if no index].</param>
        /// <returns>MediaStream.</returns>
        private MediaStream GetMediaStream(int? desiredIndex, MediaStreamType type, bool returnFirstIfNoIndex = true)
        {
            var streams = LibraryItem.MediaStreams.Where(s => s.Type == type).ToList();

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
        /// Gets the response info.
        /// </summary>
        /// <returns>Task{ResponseInfo}.</returns>
        protected override Task<ResponseInfo> GetResponseInfo()
        {
            var info = new ResponseInfo
            {
                ContentType = MimeTypes.GetMimeType(OutputFilePath),
                CompressResponse = false
            };

            return Task.FromResult(info);
        }

        /// <summary>
        /// Gets the client's desired audio bitrate
        /// </summary>
        /// <value>The audio bit rate.</value>
        protected int? AudioBitRate
        {
            get
            {
                var val = QueryString["AudioBitRate"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the client's desired video bitrate
        /// </summary>
        /// <value>The video bit rate.</value>
        protected int? VideoBitRate
        {
            get
            {
                var val = QueryString["VideoBitRate"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the desired audio stream index
        /// </summary>
        /// <value>The index of the audio stream.</value>
        private int? AudioStreamIndex
        {
            get
            {
                var val = QueryString["AudioStreamIndex"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the desired video stream index
        /// </summary>
        /// <value>The index of the video stream.</value>
        private int? VideoStreamIndex
        {
            get
            {
                var val = QueryString["VideoStreamIndex"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the desired subtitle stream index
        /// </summary>
        /// <value>The index of the subtitle stream.</value>
        private int? SubtitleStreamIndex
        {
            get
            {
                var val = QueryString["SubtitleStreamIndex"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the audio channels.
        /// </summary>
        /// <value>The audio channels.</value>
        public int? AudioChannels
        {
            get
            {
                var val = QueryString["audiochannels"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the audio sample rate.
        /// </summary>
        /// <value>The audio sample rate.</value>
        public int? AudioSampleRate
        {
            get
            {
                var val = QueryString["audiosamplerate"];

                if (string.IsNullOrEmpty(val))
                {
                    return 44100;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// If we're going to put a fixed size on the command line, this will calculate it
        /// </summary>
        /// <param name="outputVideoCodec">The output video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetOutputSizeParam(string outputVideoCodec)
        {
            // http://sonnati.wordpress.com/2012/10/19/ffmpeg-the-swiss-army-knife-of-internet-streaming-part-vi/

            var assSubtitleParam = string.Empty;

            if (SubtitleStream != null)
            {
                if (SubtitleStream.Codec.IndexOf("srt", StringComparison.OrdinalIgnoreCase) != -1 || SubtitleStream.Codec.IndexOf("subrip", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    assSubtitleParam = GetTextSubtitleParam(SubtitleStream);
                }
            }

            // If fixed dimensions were supplied
            if (Width.HasValue && Height.HasValue)
            {
                return string.Format(" -vf \"scale={0}:{1}{2}\"", Width.Value, Height.Value, assSubtitleParam);
            }

            var isH264Output = outputVideoCodec.Equals("libx264", StringComparison.OrdinalIgnoreCase);

            // If a fixed width was requested
            if (Width.HasValue)
            {
                return isH264Output ?
                    string.Format(" -vf \"scale={0}:trunc(ow/a/2)*2{1}\"", Width.Value, assSubtitleParam) :
                    string.Format(" -vf \"scale={0}:-1{1}\"", Width.Value, assSubtitleParam);
            }

            // If a max width was requested
            if (MaxWidth.HasValue && !MaxHeight.HasValue)
            {
                return isH264Output ?
                    string.Format(" -vf \"scale=min(iw\\,{0}):trunc(ow/a/2)*2{1}\"", MaxWidth.Value, assSubtitleParam) :
                    string.Format(" -vf \"scale=min(iw\\,{0}):-1{1}\"", MaxWidth.Value, assSubtitleParam);
            }

            // Need to perform calculations manually

            // Try to account for bad media info
            var currentHeight = VideoStream.Height ?? MaxHeight ?? Height ?? 0;
            var currentWidth = VideoStream.Width ?? MaxWidth ?? Width ?? 0;

            var outputSize = DrawingUtils.Resize(currentWidth, currentHeight, Width, Height, MaxWidth, MaxHeight);

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
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <returns>System.String.</returns>
        protected string GetTextSubtitleParam(MediaStream subtitleStream)
        {
            var path = subtitleStream.IsExternal ? GetConvertedAssPath(subtitleStream) : GetExtractedAssPath(subtitleStream);

            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var param = string.Format(",ass={0}", path);

            var time = StartTimeTicks;

            if (time.HasValue)
            {
                var seconds = Convert.ToInt32(TimeSpan.FromTicks(time.Value).TotalSeconds);
                param += string.Format(",setpts=PTS-{0}/TB", seconds);
            }

            return param;
        }

        /// <summary>
        /// Gets the extracted ass path.
        /// </summary>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <returns>System.String.</returns>
        private string GetExtractedAssPath(MediaStream subtitleStream)
        {
            var video = LibraryItem as Video;

            var path = Kernel.FFMpegManager.GetSubtitleCachePath(video, subtitleStream.Index, ".ass");

            if (!File.Exists(path))
            {
                var success = Kernel.FFMpegManager.ExtractTextSubtitle(video, subtitleStream.Index, path, CancellationToken.None).Result;

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
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <returns>System.String.</returns>
        private string GetConvertedAssPath(MediaStream subtitleStream)
        {
            var video = LibraryItem as Video;

            var path = Kernel.FFMpegManager.GetSubtitleCachePath(video, subtitleStream.Index, ".ass");

            if (!File.Exists(path))
            {
                var success = Kernel.FFMpegManager.ConvertTextSubtitle(subtitleStream, path, CancellationToken.None).Result;

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
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="videoCodec">The video codec.</param>
        /// <returns>System.String.</returns>
        protected string GetInternalGraphicalSubtitleParam(MediaStream subtitleStream, string videoCodec)
        {
            var outputSizeParam = string.Empty;

            // Add resolution params, if specified
            if (Width.HasValue || Height.HasValue || MaxHeight.HasValue || MaxWidth.HasValue)
            {
                outputSizeParam = GetOutputSizeParam(videoCodec).TrimEnd('"');
                outputSizeParam = "," + outputSizeParam.Substring(outputSizeParam.IndexOf("scale", StringComparison.OrdinalIgnoreCase));
            }

            return string.Format(" -filter_complex \"[0:{0}]format=yuva444p,lut=u=128:v=128:y=gammaval(.3)[sub] ; [0:0] [sub] overlay{1}\"", subtitleStream.Index, outputSizeParam);
        }

        /// <summary>
        /// Gets the fixed output video height, in pixels
        /// </summary>
        /// <value>The height.</value>
        protected int? Height
        {
            get
            {
                string val = QueryString["height"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the fixed output video width, in pixels
        /// </summary>
        /// <value>The width.</value>
        protected int? Width
        {
            get
            {
                string val = QueryString["width"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the maximum output video height, in pixels
        /// </summary>
        /// <value>The height of the max.</value>
        protected int? MaxHeight
        {
            get
            {
                string val = QueryString["maxheight"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the maximum output video width, in pixels
        /// </summary>
        /// <value>The width of the max.</value>
        protected int? MaxWidth
        {
            get
            {
                string val = QueryString["maxwidth"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return int.Parse(val);
            }
        }

        /// <summary>
        /// Gets the output video framerate
        /// </summary>
        /// <value>The max frame rate.</value>
        protected float? FrameRate
        {
            get
            {
                string val = QueryString["framerate"];

                if (string.IsNullOrEmpty(val))
                {
                    return null;
                }

                return float.Parse(val);
            }
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetSampleRateParam()
        {
            // If the user requested a max value
            if (AudioSampleRate.HasValue)
            {
                return AudioSampleRate.Value;
            }

            return null;
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <param name="audioCodec">The audio codec.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetNumAudioChannelsParam(string audioCodec)
        {
            if (AudioStream.Channels > 2)
            {
                if (audioCodec.Equals("libvo_aacenc"))
                {
                    // libvo_aacenc currently only supports two channel output
                    return 2;
                }
                if (audioCodec.Equals("wmav2"))
                {
                    // wmav2 currently only supports two channel output
                    return 2;
                }
            }

            return GetNumAudioChannelsParam();
        }

        /// <summary>
        /// Gets the number of audio channels to specify on the command line
        /// </summary>
        /// <returns>System.Nullable{System.Int32}.</returns>
        protected int? GetNumAudioChannelsParam()
        {
            // If the user requested a max number of channels
            if (AudioChannels.HasValue)
            {
                return AudioChannels.Value;
            }

            return null;
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
        /// <returns>System.String.</returns>
        protected string GetAudioCodec()
        {
            if (AudioCodec.HasValue)
            {
                if (AudioCodec == AudioCodecs.Aac)
                {
                    return "libvo_aacenc";
                }
                if (AudioCodec == AudioCodecs.Mp3)
                {
                    return "libmp3lame";
                }
                if (AudioCodec == AudioCodecs.Vorbis)
                {
                    return "libvorbis";
                }
                if (AudioCodec == AudioCodecs.Wma)
                {
                    return "wmav2";
                }
            }

            return "copy";
        }

        /// <summary>
        /// Gets the name of the output video codec
        /// </summary>
        /// <returns>System.String.</returns>
        protected string GetVideoCodec()
        {
            if (VideoCodec.HasValue)
            {
                if (VideoCodec == VideoCodecs.H264)
                {
                    return "libx264";
                }
                if (VideoCodec == VideoCodecs.Vpx)
                {
                    return "libvpx";
                }
                if (VideoCodec == VideoCodecs.Wmv)
                {
                    return "wmv2";
                }
                if (VideoCodec == VideoCodecs.Theora)
                {
                    return "libtheora";
                }
            }

            return "copy";
        }

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        protected string GetInputArgument(IIsoMount isoMount)
        {
            return isoMount == null ?
                Kernel.FFMpegManager.GetInputArgument(LibraryItem) :
                Kernel.FFMpegManager.GetInputArgument(LibraryItem as Video, IsoMount);
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <returns>Task.</returns>
        protected async Task StartFFMpeg(string outputPath)
        {
            var video = LibraryItem as Video;

            //if (video != null && video.VideoType == VideoType.Iso &&
            //    video.IsoType.HasValue && Kernel.IsoManager.CanMount(video.Path))
            //{
            //    IsoMount = await Kernel.IsoManager.Mount(video.Path, CancellationToken.None).ConfigureAwait(false);
            //}

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,

                    FileName = Kernel.FFMpegManager.FFMpegPath,
                    WorkingDirectory = Path.GetDirectoryName(Kernel.FFMpegManager.FFMpegPath),
                    Arguments = GetCommandLineArguments(outputPath, IsoMount),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            Plugin.Instance.OnTranscodeBeginning(outputPath, TranscodingJobType, process);

            //Logger.Info(process.StartInfo.FileName + " " + process.StartInfo.Arguments);

            var logFilePath = Path.Combine(Kernel.ApplicationPaths.LogDirectoryPath, "ffmpeg-" + Guid.NewGuid() + ".txt");

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            LogFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);

            process.Exited += OnFFMpegProcessExited;

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                //Logger.ErrorException("Error starting ffmpeg", ex);

                Plugin.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType);

                process.Exited -= OnFFMpegProcessExited;
                LogFileStream.Dispose();

                throw;
            }

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            process.BeginOutputReadLine();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            process.StandardError.BaseStream.CopyToAsync(LogFileStream);

            // Wait for the file to exist before proceeeding
            while (!File.Exists(outputPath))
            {
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void OnFFMpegProcessExited(object sender, EventArgs e)
        {
            if (IsoMount != null)
            {
                IsoMount.Dispose();
                IsoMount = null;
            }

            var outputFilePath = OutputFilePath;

            LogFileStream.Dispose();

            var process = (Process)sender;

            process.Exited -= OnFFMpegProcessExited;

            int? exitCode = null;

            try
            {
                exitCode = process.ExitCode;
                //Logger.Info("FFMpeg exited with code {0} for {1}", exitCode.Value, outputFilePath);
            }
            catch
            {
                //Logger.Info("FFMpeg exited with an error for {0}", outputFilePath);
            }

            process.Dispose();

            Plugin.Instance.OnTranscodingFinished(outputFilePath, TranscodingJobType);

            if (!exitCode.HasValue || exitCode.Value != 0)
            {
                //Logger.Info("Deleting partial stream file(s) {0}", outputFilePath);

                try
                {
                    DeletePartialStreamFiles(outputFilePath);
                }
                catch (IOException ex)
                {
                    //Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, outputFilePath);
                }
            }
            else
            {
                //Logger.Info("FFMpeg completed and exited normally for {0}", outputFilePath);
            }
        }

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        protected abstract void DeletePartialStreamFiles(string outputFilePath);
    }
}
