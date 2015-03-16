using MediaBrowser.Api.Playback.Hls;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Diagnostics;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

namespace MediaBrowser.Api.Playback.Dash
{
    /// <summary>
    /// Options is needed for chromecast. Threw Head in there since it's related
    /// </summary>
    [Route("/Videos/{Id}/master.mpd", "GET", Summary = "Gets a video stream using Mpeg dash.")]
    [Route("/Videos/{Id}/master.mpd", "HEAD", Summary = "Gets a video stream using Mpeg dash.")]
    public class GetMasterManifest : VideoStreamRequest
    {
        public bool EnableAdaptiveBitrateStreaming { get; set; }

        public GetMasterManifest()
        {
            EnableAdaptiveBitrateStreaming = true;
        }
    }

    [Route("/Videos/{Id}/dash/{SegmentType}/{SegmentId}.m4s", "GET")]
    public class GetDashSegment : VideoStreamRequest
    {
        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }

        /// <summary>
        /// Gets or sets the type of the segment.
        /// </summary>
        /// <value>The type of the segment.</value>
        public string SegmentType { get; set; }
    }

    public class MpegDashService : BaseHlsService
    {
        public MpegDashService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IProcessManager processManager, IMediaSourceManager mediaSourceManager, INetworkManager networkManager)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, subtitleEncoder, deviceManager, processManager, mediaSourceManager)
        {
            NetworkManager = networkManager;
        }

        protected INetworkManager NetworkManager { get; private set; }

        public object Get(GetMasterManifest request)
        {
            var result = GetAsync(request, "GET").Result;

            return result;
        }

        public object Head(GetMasterManifest request)
        {
            var result = GetAsync(request, "HEAD").Result;

            return result;
        }

        protected override bool EnableOutputInSubFolder
        {
            get
            {
                return true;
            }
        }

        private async Task<object> GetAsync(GetMasterManifest request, string method)
        {
            if (string.IsNullOrEmpty(request.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required");
            }

            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var playlistText = string.Empty;

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                playlistText = new ManifestBuilder().GetManifestText(state, Request.RawUrl);
            }

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.mpd"), new Dictionary<string, string>());
        }

        public object Get(GetDashSegment request)
        {
            return GetDynamicSegment(request, request.SegmentId, request.SegmentType).Result;
        }

        private async Task<object> GetDynamicSegment(VideoStreamRequest request, string segmentId, string segmentType)
        {
            if ((request.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var index = int.Parse(segmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".mpd");

            var segmentExtension = GetSegmentFileExtension(state);

            var segmentPath = GetSegmentPath(playlistPath, segmentType, segmentExtension, index);
            var segmentLength = state.SegmentLength;

            TranscodingJob job = null;

            if (File.Exists(segmentPath))
            {
                job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
                return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
            }

            await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                if (File.Exists(segmentPath))
                {
                    job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
                    return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);

                    if (currentTranscodingIndex == null || index < currentTranscodingIndex.Value || (index - currentTranscodingIndex.Value) > 4)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            ApiEntryPoint.Instance.KillTranscodingJobs(j => j.Type == TranscodingJobType && string.Equals(j.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase), p => !string.Equals(p, playlistPath, StringComparison.OrdinalIgnoreCase));

                            if (currentTranscodingIndex.HasValue)
                            {
                                DeleteLastFile(playlistPath, segmentExtension, 0);
                            }

                            var startSeconds = index * state.SegmentLength;
                            request.StartTimeTicks = TimeSpan.FromSeconds(startSeconds).Ticks;

                            job = await StartFfMpeg(state, playlistPath, cancellationTokenSource, Path.GetDirectoryName(playlistPath)).ConfigureAwait(false);
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
                ApiEntryPoint.Instance.TranscodingStartLock.Release();
            }

            Logger.Info("waiting for {0}", segmentPath);
            while (!File.Exists(segmentPath))
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            Logger.Info("returning {0}", segmentPath);
            job = job ?? ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
            return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task WaitForMinimumSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            var tmpPath = playlist + ".tmp";
            Logger.Debug("Waiting for {0} segments in {1}", segmentCount, playlist);
            // Double since audio and video are split
            segmentCount = segmentCount * 2;
            // Account for the initial segments
            segmentCount += 2;

            while (true)
            {
                FileStream fileStream;
                try
                {
                    fileStream = FileSystem.GetFileStream(tmpPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true);
                }
                catch (IOException)
                {
                    fileStream = FileSystem.GetFileStream(playlist, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true);
                }
                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (fileStream)
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var count = 0;

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf(".m4s", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                count++;
                                if (count >= segmentCount)
                                {
                                    Logger.Debug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                    return;
                                }
                            }
                        }
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        private async Task<object> GetSegmentResult(string playlistPath,
            string segmentPath,
            int segmentIndex,
            int segmentLength,
            TranscodingJob transcodingJob,
            CancellationToken cancellationToken)
        {
            // If all transcoding has completed, just return immediately
            if (!IsTranscoding(playlistPath))
            {
                return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
            }

            var segmentFilename = Path.GetFileName(segmentPath);

            using (var fileStream = FileSystem.GetFileStream(playlistPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                using (var reader = new StreamReader(fileStream))
                {
                    var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                    // If it appears in the playlist, it's done
                    if (text.IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
                    }
                }
            }

            // if a different file is encoding, it's done
            //var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath);
            //if (currentTranscodingIndex > segmentIndex)
            //{
            //return GetSegmentResult(segmentPath, segmentIndex);
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

            return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
        }

        private object GetSegmentResult(string segmentPath, int index, int segmentLength, TranscodingJob transcodingJob)
        {
            var segmentEndingSeconds = (1 + index) * segmentLength;
            var segmentEndingPositionTicks = TimeSpan.FromSeconds(segmentEndingSeconds).Ticks;

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = segmentPath,
                FileShare = FileShare.ReadWrite,
                OnComplete = () =>
                {
                    if (transcodingJob != null)
                    {
                        transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                    }

                }
            });
        }

        private bool IsTranscoding(string playlistPath)
        {
            var job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);

            return job != null && !job.HasExited;
        }

        public int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
        {
            var file = GetLastTranscodingFile(playlist, segmentExtension, FileSystem);

            if (file == null)
            {
                return null;
            }

            var playlistFilename = Path.GetFileNameWithoutExtension(playlist);

            var indexString = Path.GetFileNameWithoutExtension(file.Name).Substring(playlistFilename.Length);

            return int.Parse(indexString, NumberStyles.Integer, UsCulture);
        }

        private void DeleteLastFile(string path, string segmentExtension, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            var file = GetLastTranscodingFile(path, segmentExtension, FileSystem);

            if (file != null)
            {
                try
                {
                    FileSystem.DeleteFile(file.FullName);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);

                    Thread.Sleep(100);
                    DeleteLastFile(path, segmentExtension, retryCount + 1);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);
                }
            }
        }

        private static FileInfo GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            try
            {
                return new DirectoryInfo(folder)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, segmentExtension, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        private string GetSegmentPath(string playlist, string segmentType, string segmentExtension, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var id = string.Equals(segmentType, "video", StringComparison.OrdinalIgnoreCase)
                ? "0"
                : "1";

            string filename;

            if (index == 0)
            {
                filename = "init-stream" + id + segmentExtension;
            }
            else
            {
                var number = index.ToString("00000", CultureInfo.InvariantCulture);
                filename = "chunk-stream" + id + "-" + number + segmentExtension;
            }

            return Path.Combine(folder, filename);
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

            var args = "-codec:v:0 " + codec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (codec.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                return state.VideoStream != null && IsH264(state.VideoStream) ?
                    args + " -bsf:v h264_mp4toannexb" :
                    args;
            }

            var keyFrameArg = string.Format(" -force_key_frames expr:gte(t,n_forced*{0})",
                state.SegmentLength.ToString(UsCulture));

            var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream;

            args += " " + GetVideoQualityParam(state, H264Encoder, true) + keyFrameArg;

            // Add resolution params, if specified
            if (!hasGraphicalSubs)
            {
                args += GetOutputSizeParam(state, codec, false);
            }

            // This is for internal graphical subs
            if (hasGraphicalSubs)
            {
                args += GetGraphicalSubtitleParam(state, codec);
            }

            return args;
        }

        protected override string GetCommandLineArguments(string outputPath, string transcodingJobId, StreamState state, bool isEncoding)
        {
            // test url http://192.168.1.2:8096/videos/233e8905d559a8f230db9bffd2ac9d6d/master.mpd?mediasourceid=233e8905d559a8f230db9bffd2ac9d6d&videocodec=h264&audiocodec=aac&maxwidth=1280&videobitrate=500000&audiobitrate=128000&profile=baseline&level=3
            // Good info on i-frames http://blog.streamroot.io/encode-multi-bitrate-videos-mpeg-dash-mse-based-media-players/

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            var args = string.Format("{0} {1} -map_metadata -1 -threads {2} {3} {4} -copyts {5} -f dash -use_template 0 -min_seg_duration {6} -y \"{7}\"",
                inputModifier,
                GetInputArgument(transcodingJobId, state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                (state.SegmentLength * 1000000).ToString(CultureInfo.InvariantCulture),
                outputPath
                ).Trim();

            return args;
        }

        //private string GetCurrentTranscodingIndex(string outputPath)
        //{

        //}

        //private string GetNextTranscodingIndex(string outputPath)
        //{
            
        //}

        //private string GetActualPlaylistPath(string outputPath, string transcodingIndex)
        //{
            
        //}

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return ".m4s";
        }

        protected override TranscodingJobType TranscodingJobType
        {
            get
            {
                return TranscodingJobType.Dash;
            }
        }
    }
}
