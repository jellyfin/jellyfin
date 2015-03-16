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

    [Route("/Videos/{Id}/dash/{RepresentationId}/{SegmentId}.m4s", "GET")]
    public class GetDashSegment : VideoStreamRequest
    {
        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }

        /// <summary>
        /// Gets or sets the representation identifier.
        /// </summary>
        /// <value>The representation identifier.</value>
        public string RepresentationId { get; set; }
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
            return GetDynamicSegment(request, request.SegmentId, request.RepresentationId).Result;
        }

        private async Task<object> GetDynamicSegment(VideoStreamRequest request, string segmentId, string representationId)
        {
            if ((request.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var index = string.Equals(segmentId, "init", StringComparison.OrdinalIgnoreCase) ?
                -1 :
                int.Parse(segmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".mpd");

            var segmentExtension = GetSegmentFileExtension(state);

            var segmentPath = GetSegmentPath(playlistPath, representationId, segmentExtension, index);
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
                    if (string.Equals(representationId, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                        Logger.Debug("Current transcoding index is {0}", currentTranscodingIndex ?? -2);
                        if (currentTranscodingIndex == null || index < currentTranscodingIndex.Value || (index - currentTranscodingIndex.Value) > 4)
                        {
                            // If the playlist doesn't already exist, startup ffmpeg
                            try
                            {
                                KillTranscodingJobs(request.DeviceId, playlistPath);

                                if (currentTranscodingIndex.HasValue)
                                {
                                    DeleteTranscodedFiles(playlistPath, 0);
                                }

                                var positionTicks = GetPositionTicks(state, index);
                                request.StartTimeTicks = positionTicks;

                                job = await StartFfMpeg(state, playlistPath, cancellationTokenSource, Path.GetDirectoryName(playlistPath)).ConfigureAwait(false);
                                Task.Run(() => MonitorDashProcess(playlistPath, positionTicks == 0, job, cancellationToken));
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

            return await GetSegmentResult(playlistPath, segmentPath, index, segmentLength, job ?? ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType), cancellationToken).ConfigureAwait(false);
        }

        private void KillTranscodingJobs(string deviceId, string playlistPath)
        {
            ApiEntryPoint.Instance.KillTranscodingJobs(j => j.Type == TranscodingJobType && string.Equals(j.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase), p => !string.Equals(p, playlistPath, StringComparison.OrdinalIgnoreCase));
        }

        private long GetPositionTicks(StreamState state, int segmentIndex)
        {
            if (segmentIndex <= 1)
            {
                return 0;
            }

            var startSeconds = segmentIndex * state.SegmentLength;
            return TimeSpan.FromSeconds(startSeconds).Ticks;
        }

        protected override async Task WaitForMinimumSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            var tmpPath = playlist + ".tmp";
            Logger.Debug("Waiting for {0} segments in {1}", segmentCount, playlist);

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
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf("stream0-" + segmentCount.ToString("00000", CultureInfo.InvariantCulture) + ".m4s", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                Logger.Debug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                return;
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
            if (transcodingJob != null && transcodingJob.HasExited)
            {
                return GetSegmentResult(segmentPath, segmentIndex, segmentLength, transcodingJob);
            }

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

        public int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
        {
            var file = GetLastTranscodingFiles(playlist, segmentExtension, FileSystem, 1).FirstOrDefault();

            if (file == null)
            {
                return null;
            }

            return GetIndex(file.Name);
        }

        public int GetIndex(string segmentFile)
        {
            var indexString = Path.GetFileNameWithoutExtension(segmentFile).Split('-').LastOrDefault();

            if (string.Equals(indexString, "init", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }
            return int.Parse(indexString, NumberStyles.Integer, UsCulture) - 1;
        }

        private void DeleteTranscodedFiles(string path, int retryCount)
        {

            if (retryCount >= 5)
            {
                return;
            }
        }

        private static List<FileInfo> GetLastTranscodingFiles(string playlist, string segmentExtension, IFileSystem fileSystem, int count)
        {
            var folder = Path.GetDirectoryName(playlist);

            try
            {
                return new DirectoryInfo(folder)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i => string.Equals(i.Extension, segmentExtension, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .Take(count)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<FileInfo>();
            }
        }

        private string GetSegmentPath(string playlist, string representationId, string segmentExtension, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var number = index == -1 ?
                "init" :
                index.ToString("00000", CultureInfo.InvariantCulture);

            var filename = "stream" + representationId + "-" + number + segmentExtension;

            return Path.Combine(folder, "completed", filename);
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
            //var startNumber = GetStartNumber(state);

            var initSegmentName = "stream$RepresentationID$-init.m4s";
            var segmentName = "stream$RepresentationID$-$Number%05d$.m4s";

            var args = string.Format("{0} {1} -map_metadata -1 -threads {2} {3} {4} -copyts {5} -f dash -init_seg_name \"{6}\" -media_seg_name \"{7}\" -use_template 0 -use_timeline 1 -min_seg_duration {8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(transcodingJobId, state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                initSegmentName,
                segmentName,
                (state.SegmentLength * 1000000).ToString(CultureInfo.InvariantCulture),
                outputPath
                ).Trim();

            return args;
        }

        protected override int GetStartNumber(StreamState state)
        {
            return GetStartNumber(state.VideoRequest);
        }

        private int GetStartNumber(VideoStreamRequest request)
        {
            var segmentId = "0";

            var segmentRequest = request as GetDashSegment;
            if (segmentRequest != null)
            {
                segmentId = segmentRequest.SegmentId;
            }

            if (string.Equals(segmentId, "init", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }

            return int.Parse(segmentId, NumberStyles.Integer, UsCulture);
        }

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

        private async void MonitorDashProcess(string playlist, bool moveInitSegment, TranscodingJob transcodingJob, CancellationToken cancellationToken)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(playlist));
            var completedDirectory = Path.Combine(Path.GetDirectoryName(playlist), "completed");
            Directory.CreateDirectory(completedDirectory);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var files = directory.EnumerateFiles("*.m4s", SearchOption.TopDirectoryOnly)
                        .OrderBy(FileSystem.GetCreationTimeUtc)
                        .ToList();

                    foreach (var file in files)
                    {
                        var fileIndex = GetIndex(file.Name);

                        if (fileIndex == -1 && !moveInitSegment)
                        {
                            continue;
                        }

                        await WaitForFileToBeComplete(file.FullName, playlist, transcodingJob, cancellationToken).ConfigureAwait(false);

                        var newName = fileIndex == -1
                            ? "init.m4s"
                            : fileIndex.ToString("00000", CultureInfo.InvariantCulture) + ".m4s";

                        var representationId = file.FullName.IndexOf("stream0", StringComparison.OrdinalIgnoreCase) != -1 ?
                            "0" :
                            "1";

                        newName = "stream" + representationId + "-" + newName;

                        File.Copy(file.FullName, Path.Combine(completedDirectory, newName), true);

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (IOException)
                {

                }
            }
        }

        private async Task WaitForFileToBeComplete(string segmentPath, string playlistPath, TranscodingJob transcodingJob, CancellationToken cancellationToken)
        {
            // If all transcoding has completed, just return immediately
            if (transcodingJob != null && transcodingJob.HasExited)
            {
                return;
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
                        return;
                    }
                }
            }

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
        }
    }
}
