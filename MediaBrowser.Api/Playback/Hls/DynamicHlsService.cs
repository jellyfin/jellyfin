using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    [Route("/Videos/{Id}/master.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetMasterHlsVideoStream : VideoStreamRequest
    {
        [ApiMember(Name = "BaselineStreamAudioBitRate", Description = "Optional. Specify the audio bitrate for the baseline stream.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? BaselineStreamAudioBitRate { get; set; }

        [ApiMember(Name = "AppendBaselineStream", Description = "Optional. Whether or not to include a baseline audio-only stream in the master playlist.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool AppendBaselineStream { get; set; }
    }

    [Route("/Videos/{Id}/main.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetMainHlsVideoStream : VideoStreamRequest
    {
    }

    [Route("/Videos/{Id}/baseline.m3u8", "GET")]
    [Api(Description = "Gets a video stream using HTTP live streaming.")]
    public class GetBaselineHlsVideoStream : VideoStreamRequest
    {
    }

    /// <summary>
    /// Class GetHlsVideoSegment
    /// </summary>
    [Route("/Videos/{Id}/hlsdynamic/{PlaylistId}/{SegmentId}.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetDynamicHlsVideoSegment : VideoStreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    public class DynamicHlsService : BaseHlsService
    {
        public DynamicHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
        {
        }

        public object Get(GetMasterHlsVideoStream request)
        {
            var result = GetAsync(request).Result;

            return result;
        }

        public object Get(GetDynamicHlsVideoSegment request)
        {
            if (string.Equals("baseline", request.PlaylistId, StringComparison.OrdinalIgnoreCase))
            {
                return GetDynamicSegment(request, false).Result;
            }

            return GetDynamicSegment(request, true).Result;
        }

        private static readonly SemaphoreSlim FfmpegStartLock = new SemaphoreSlim(1, 1);
        private async Task<object> GetDynamicSegment(GetDynamicHlsVideoSegment request, bool isMain)
        {
            if ((request.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var index = int.Parse(request.SegmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

            var segmentPath = GetSegmentPath(playlistPath, index);

            if (File.Exists(segmentPath))
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);
                return await GetSegmentResult(playlistPath, segmentPath, index, cancellationToken).ConfigureAwait(false);
            }

            await FfmpegStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                if (File.Exists(segmentPath))
                {
                    ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType.Hls);
                    return await GetSegmentResult(playlistPath, segmentPath, index, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath);

                    if (currentTranscodingIndex == null || index < currentTranscodingIndex.Value || (index - currentTranscodingIndex.Value) > 3)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            if (currentTranscodingIndex.HasValue)
                            {
                                ApiEntryPoint.Instance.KillTranscodingJobs(state.Request.DeviceId, FileDeleteMode.None);

                                DeleteLastFile(playlistPath, 0);
                            }

                            var startSeconds = index * state.SegmentLength;
                            request.StartTimeTicks = TimeSpan.FromSeconds(startSeconds).Ticks;

                            await StartFfMpeg(state, playlistPath, cancellationTokenSource).ConfigureAwait(false);
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        await WaitForMinimumSegmentCount(playlistPath, 1, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                FfmpegStartLock.Release();
            }

            Logger.Info("waiting for {0}", segmentPath);
            while (!File.Exists(segmentPath))
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            Logger.Info("returning {0}", segmentPath);
            return await GetSegmentResult(playlistPath, segmentPath, index, cancellationToken).ConfigureAwait(false);
        }

        public int? GetCurrentTranscodingIndex(string playlist)
        {
            var file = GetLastTranscodingFile(playlist, FileSystem);

            if (file == null)
            {
                return null;
            }

            var playlistFilename = Path.GetFileNameWithoutExtension(playlist);

            var indexString = Path.GetFileNameWithoutExtension(file.Name).Substring(playlistFilename.Length);

            return int.Parse(indexString, NumberStyles.Integer, UsCulture);
        }

        private void DeleteLastFile(string path, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            var file = GetLastTranscodingFile(path, FileSystem);

            if (file != null)
            {
                try
                {
                    File.Delete(file.FullName);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);

                    Thread.Sleep(100);
                    DeleteLastFile(path, retryCount + 1);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);
                }
            }
        }

        private static FileInfo GetLastTranscodingFile(string playlist, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            try
            {
                return new DirectoryInfo(folder)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, ".ts", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        protected override int GetStartNumber(StreamState state)
        {
            var request = (GetDynamicHlsVideoSegment)state.Request;

            return int.Parse(request.SegmentId, NumberStyles.Integer, UsCulture);
        }

        private string GetSegmentPath(string playlist, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filename = Path.GetFileNameWithoutExtension(playlist);

            return Path.Combine(folder, filename + index.ToString(UsCulture) + ".ts");
        }

        private async Task<object> GetSegmentResult(string playlistPath, string segmentPath, int segmentIndex, CancellationToken cancellationToken)
        {
            // If all transcoding has completed, just return immediately
            if (!IsTranscoding(playlistPath))
            {
                return ResultFactory.GetStaticFileResult(Request, segmentPath, FileShare.ReadWrite);
            }

            var segmentFilename = Path.GetFileName(segmentPath);

            // If it appears in the playlist, it's done
            if (File.ReadAllText(playlistPath).IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
            {
                return ResultFactory.GetStaticFileResult(Request, segmentPath, FileShare.ReadWrite);
            }

            // if a different file is encoding, it's done
            //var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath);
            //if (currentTranscodingIndex > segmentIndex)
            //{
            //    return ResultFactory.GetStaticFileResult(Request, segmentPath, FileShare.ReadWrite);
            //}

            // Wait for the file to stop being written to, then stream it
            var length = new FileInfo(segmentPath).Length;
            var eofCount = 0;

            while (eofCount < 10)
            {
                var info = new FileInfo(segmentPath);

                if (!info.Exists)
                {
                    break;
                }

                var newLength = info.Length;

                if (newLength == length)
                {
                    eofCount++;
                }
                else
                {
                    eofCount = 0;
                }

                length = newLength;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            return ResultFactory.GetStaticFileResult(Request, segmentPath, FileShare.ReadWrite);
        }

        private bool IsTranscoding(string playlistPath)
        {
            var job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);

            return job != null && !job.HasExited;
        }

        private async Task<object> GetAsync(GetMasterHlsVideoStream request)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            int audioBitrate;
            int videoBitrate;
            GetPlaylistBitrates(state, out audioBitrate, out videoBitrate);

            var appendBaselineStream = false;
            var baselineStreamBitrate = 64000;

            var hlsVideoRequest = state.VideoRequest as GetMasterHlsVideoStream;
            if (hlsVideoRequest != null)
            {
                appendBaselineStream = hlsVideoRequest.AppendBaselineStream;
                baselineStreamBitrate = hlsVideoRequest.BaselineStreamAudioBitRate ?? baselineStreamBitrate;
            }

            var playlistText = GetMasterPlaylistFileText(videoBitrate + audioBitrate, appendBaselineStream, baselineStreamBitrate);

            return ResultFactory.GetResult(playlistText, Common.Net.MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetMasterPlaylistFileText(int bitrate, bool includeBaselineStream, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.05);

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            // Main stream
            builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + paddedBitrate.ToString(UsCulture));
            var playlistUrl = "main.m3u8" + queryString;
            builder.AppendLine(playlistUrl);

            // Low bitrate stream
            if (includeBaselineStream)
            {
                builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + baselineStreamBitrate.ToString(UsCulture));
                playlistUrl = "baseline.m3u8" + queryString;
                builder.AppendLine(playlistUrl);
            }

            return builder.ToString();
        }

        public object Get(GetMainHlsVideoStream request)
        {
            var result = GetPlaylistAsync(request, "main").Result;

            return result;
        }

        public object Get(GetBaselineHlsVideoStream request)
        {
            var result = GetPlaylistAsync(request, "baseline").Result;

            return result;
        }

        private async Task<object> GetPlaylistAsync(VideoStreamRequest request, string name)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");
            builder.AppendLine("#EXT-X-VERSION:3");
            builder.AppendLine("#EXT-X-TARGETDURATION:" + state.SegmentLength.ToString(UsCulture));
            builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            var seconds = TimeSpan.FromTicks(state.RunTimeTicks ?? 0).TotalSeconds;

            var index = 0;

            while (seconds > 0)
            {
                var length = seconds >= state.SegmentLength ? state.SegmentLength : seconds;

                builder.AppendLine("#EXTINF:" + length.ToString(UsCulture));

                builder.AppendLine(string.Format("hlsdynamic/{0}/{1}.ts{2}",

                    name,
                    index.ToString(UsCulture),
                    queryString));

                seconds -= state.SegmentLength;
                index++;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            var playlistText = builder.ToString();

            return ResultFactory.GetResult(playlistText, Common.Net.MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        protected override string GetAudioArguments(StreamState state)
        {
            var codec = state.OutputAudioCodec;

            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }

            var args = "-codec:a:0 " + codec;

            var channels = state.OutputAudioChannels;

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(UsCulture);
            }

            args += " " + GetAudioFilterParam(state, true);

            return args;
        }

        protected override string GetVideoArguments(StreamState state)
        {
            var codec = state.OutputVideoCodec;

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                // TOOD: Switch to  -bsf dump_extra?
                return IsH264(state.VideoStream) ? "-codec:v:0 copy -bsf h264_mp4toannexb" : "-codec:v:0 copy";
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                state.SegmentLength.ToString(UsCulture));

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            var args = "-codec:v:0 " + codec + " " + GetVideoQualityParam(state, "libx264", true) + keyFrameArg;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec, CancellationToken.None);
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetInternalGraphicalSubtitleParam(state, codec);
            }

            return args;
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return ".ts";
        }

        protected override TranscodingJobType TranscodingJobType
        {
            get
            {
                return TranscodingJobType.Hls;
            }
        }
    }
}
