using MediaBrowser.Api.Playback.Hls;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
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
        public MpegDashService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer, INetworkManager networkManager) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, dlnaManager, subtitleEncoder, deviceManager, mediaSourceManager, zipClient, jsonSerializer)
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

            var requestedIndex = string.Equals(segmentId, "init", StringComparison.OrdinalIgnoreCase) ?
                -1 :
                int.Parse(segmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".mpd");

            var segmentExtension = GetSegmentFileExtension(state);

            var segmentPath = FindSegment(playlistPath, representationId, segmentExtension, requestedIndex);
            var segmentLength = state.SegmentLength;

            TranscodingJob job = null;

            if (!string.IsNullOrWhiteSpace(segmentPath))
            {
                job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
                return await GetSegmentResult(playlistPath, segmentPath, requestedIndex, segmentLength, job, cancellationToken).ConfigureAwait(false);
            }

            await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            try
            {
                segmentPath = FindSegment(playlistPath, representationId, segmentExtension, requestedIndex);
                if (!string.IsNullOrWhiteSpace(segmentPath))
                {
                    job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
                    return await GetSegmentResult(playlistPath, segmentPath, requestedIndex, segmentLength, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    if (string.Equals(representationId, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        job = ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType);
                        var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                        var segmentGapRequiringTranscodingChange = 24 / state.SegmentLength;
                        Logger.Debug("Current transcoding index is {0}. requestedIndex={1}. segmentGapRequiringTranscodingChange={2}", currentTranscodingIndex ?? -2, requestedIndex, segmentGapRequiringTranscodingChange);
                        if (currentTranscodingIndex == null || requestedIndex < currentTranscodingIndex.Value || (requestedIndex - currentTranscodingIndex.Value) > segmentGapRequiringTranscodingChange)
                        {
                            // If the playlist doesn't already exist, startup ffmpeg
                            try
                            {
                                ApiEntryPoint.Instance.KillTranscodingJobs(request.DeviceId, request.PlaySessionId, p => false);

                                if (currentTranscodingIndex.HasValue)
                                {
                                    DeleteLastTranscodedFiles(playlistPath, 0);
                                }

                                var positionTicks = GetPositionTicks(state, requestedIndex);
                                request.StartTimeTicks = positionTicks;

                                var startNumber = GetStartNumber(state);

                                var workingDirectory = Path.Combine(Path.GetDirectoryName(playlistPath), (startNumber == -1 ? 0 : startNumber).ToString(CultureInfo.InvariantCulture));
                                state.WaitForPath = Path.Combine(workingDirectory, Path.GetFileName(playlistPath));
                                FileSystem.CreateDirectory(workingDirectory);
                                job = await StartFfMpeg(state, playlistPath, cancellationTokenSource, workingDirectory).ConfigureAwait(false);
                                await WaitForMinimumDashSegmentCount(Path.Combine(workingDirectory, Path.GetFileName(playlistPath)), 1, cancellationTokenSource.Token).ConfigureAwait(false);
                            }
                            catch
                            {
                                state.Dispose();
                                throw;
                            }
                        }
                    }
                }
            }
            finally
            {
                ApiEntryPoint.Instance.TranscodingStartLock.Release();
            }

            while (string.IsNullOrWhiteSpace(segmentPath))
            {
                segmentPath = FindSegment(playlistPath, representationId, segmentExtension, requestedIndex);
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }

            Logger.Info("returning {0}", segmentPath);
            return await GetSegmentResult(playlistPath, segmentPath, requestedIndex, segmentLength, job ?? ApiEntryPoint.Instance.GetTranscodingJob(playlistPath, TranscodingJobType), cancellationToken).ConfigureAwait(false);
        }

        private long GetPositionTicks(StreamState state, int requestedIndex)
        {
            if (requestedIndex <= 0)
            {
                return 0;
            }

            var startSeconds = requestedIndex * state.SegmentLength;
            return TimeSpan.FromSeconds(startSeconds).Ticks;
        }

        protected  Task WaitForMinimumDashSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            return WaitForSegment(playlist, "stream0-" + segmentCount.ToString("00000", CultureInfo.InvariantCulture) + ".m4s", cancellationToken);
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
            var job = ApiEntryPoint.Instance.GetTranscodingJob(playlist, TranscodingJobType);

            if (job == null || job.HasExited)
            {
                return null;
            }

            var file = GetLastTranscodingFiles(playlist, segmentExtension, FileSystem, 1).FirstOrDefault();

            if (file == null)
            {
                return null;
            }

            return GetIndex(file.FullName);
        }

        public int GetIndex(string segmentPath)
        {
            var indexString = Path.GetFileNameWithoutExtension(segmentPath).Split('-').LastOrDefault();

            if (string.Equals(indexString, "init", StringComparison.OrdinalIgnoreCase))
            {
                return -1;
            }
            var startNumber = int.Parse(Path.GetFileNameWithoutExtension(Path.GetDirectoryName(segmentPath)), NumberStyles.Integer, UsCulture);

            return startNumber + int.Parse(indexString, NumberStyles.Integer, UsCulture) - 1;
        }

        private void DeleteLastTranscodedFiles(string playlistPath, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }
        }

        private static List<FileSystemMetadata> GetLastTranscodingFiles(string playlist, string segmentExtension, IFileSystem fileSystem, int count)
        {
            var folder = Path.GetDirectoryName(playlist);

            try
            {
				return fileSystem.GetFiles(folder)
                    .Where(i => string.Equals(i.Extension, segmentExtension, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .Take(count)
                    .ToList();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<FileSystemMetadata>();
            }
        }

        private string FindSegment(string playlist, string representationId, string segmentExtension, int requestedIndex)
        {
            var folder = Path.GetDirectoryName(playlist);

            if (requestedIndex == -1)
            {
                var path = Path.Combine(folder, "0", "stream" + representationId + "-" + "init" + segmentExtension);
				return FileSystem.FileExists(path) ? path : null;
            }

            try
            {
                foreach (var subfolder in FileSystem.GetDirectoryPaths(folder).ToList())
                {
                    var subfolderName = Path.GetFileNameWithoutExtension(subfolder);
                    int startNumber;
                    if (int.TryParse(subfolderName, NumberStyles.Any, UsCulture, out startNumber))
                    {
                        var segmentIndex = requestedIndex - startNumber + 1;
                        var path = Path.Combine(folder, subfolderName, "stream" + representationId + "-" + segmentIndex.ToString("00000", CultureInfo.InvariantCulture) + segmentExtension);
						if (FileSystem.FileExists(path))
                        {
                            return path;
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
                
            }

            return null;
        }

        protected override string GetAudioArguments(StreamState state)
        {
            var codec = GetAudioEncoder(state);

            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
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
            var codec = GetVideoEncoder(state);

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

            args += " " + GetVideoQualityParam(state, GetH264Encoder(state), true) + keyFrameArg;

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

        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            // test url http://192.168.1.2:8096/videos/233e8905d559a8f230db9bffd2ac9d6d/master.mpd?mediasourceid=233e8905d559a8f230db9bffd2ac9d6d&videocodec=h264&audiocodec=aac&maxwidth=1280&videobitrate=500000&audiobitrate=128000&profile=baseline&level=3
            // Good info on i-frames http://blog.streamroot.io/encode-multi-bitrate-videos-mpeg-dash-mse-based-media-players/

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            var initSegmentName = "stream$RepresentationID$-init.m4s";
            var segmentName = "stream$RepresentationID$-$Number%05d$.m4s";

            var args = string.Format("{0} {1} -map_metadata -1 -threads {2} {3} {4} -copyts {5} -f dash -init_seg_name \"{6}\" -media_seg_name \"{7}\" -use_template 0 -use_timeline 1 -min_seg_duration {8} -y \"{9}\"",
                inputModifier,
                GetInputArgument(state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                initSegmentName,
                segmentName,
                (state.SegmentLength * 1000000).ToString(CultureInfo.InvariantCulture),
                state.WaitForPath
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

        private async Task WaitForSegment(string playlist, string segment, CancellationToken cancellationToken)
        {
            var segmentFilename = Path.GetFileName(segment);

            Logger.Debug("Waiting for {0} in {1}", segmentFilename, playlist);

            while (true)
            {
                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (var fileStream = GetPlaylistFileStream(playlist))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                Logger.Debug("Finished waiting for {0} in {1}", segmentFilename, playlist);
                                return;
                            }
                        }
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
