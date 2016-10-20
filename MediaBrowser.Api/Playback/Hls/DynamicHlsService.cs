using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MimeTypes = MediaBrowser.Model.Net.MimeTypes;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Options is needed for chromecast. Threw Head in there since it's related
    /// </summary>
    [Route("/Videos/{Id}/master.m3u8", "GET", Summary = "Gets a video stream using HTTP live streaming.")]
    [Route("/Videos/{Id}/master.m3u8", "HEAD", Summary = "Gets a video stream using HTTP live streaming.")]
    public class GetMasterHlsVideoPlaylist : VideoStreamRequest, IMasterHlsRequest
    {
        public bool EnableAdaptiveBitrateStreaming { get; set; }

        public GetMasterHlsVideoPlaylist()
        {
            EnableAdaptiveBitrateStreaming = true;
        }
    }

    [Route("/Audio/{Id}/master.m3u8", "GET", Summary = "Gets an audio stream using HTTP live streaming.")]
    [Route("/Audio/{Id}/master.m3u8", "HEAD", Summary = "Gets an audio stream using HTTP live streaming.")]
    public class GetMasterHlsAudioPlaylist : StreamRequest, IMasterHlsRequest
    {
        public bool EnableAdaptiveBitrateStreaming { get; set; }

        public GetMasterHlsAudioPlaylist()
        {
            EnableAdaptiveBitrateStreaming = true;
        }
    }

    public interface IMasterHlsRequest
    {
        bool EnableAdaptiveBitrateStreaming { get; set; }
    }

    [Route("/Videos/{Id}/main.m3u8", "GET", Summary = "Gets a video stream using HTTP live streaming.")]
    public class GetVariantHlsVideoPlaylist : VideoStreamRequest
    {
    }

    [Route("/Audio/{Id}/main.m3u8", "GET", Summary = "Gets an audio stream using HTTP live streaming.")]
    public class GetVariantHlsAudioPlaylist : StreamRequest
    {
    }

    [Route("/Videos/{Id}/hls1/{PlaylistId}/{SegmentId}.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsVideoSegment : VideoStreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    [Route("/Audio/{Id}/hls1/{PlaylistId}/{SegmentId}.aac", "GET")]
    [Route("/Audio/{Id}/hls1/{PlaylistId}/{SegmentId}.ts", "GET")]
    [Api(Description = "Gets an Http live streaming segment file. Internal use only.")]
    public class GetHlsAudioSegment : StreamRequest
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
        public DynamicHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer, INetworkManager networkManager)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, dlnaManager, subtitleEncoder, deviceManager, mediaSourceManager, zipClient, jsonSerializer)
        {
            NetworkManager = networkManager;
        }

        protected INetworkManager NetworkManager { get; private set; }

        public Task<object> Get(GetMasterHlsVideoPlaylist request)
        {
            return GetMasterPlaylistInternal(request, "GET");
        }

        public Task<object> Head(GetMasterHlsVideoPlaylist request)
        {
            return GetMasterPlaylistInternal(request, "HEAD");
        }

        public Task<object> Get(GetMasterHlsAudioPlaylist request)
        {
            return GetMasterPlaylistInternal(request, "GET");
        }

        public Task<object> Head(GetMasterHlsAudioPlaylist request)
        {
            return GetMasterPlaylistInternal(request, "HEAD");
        }

        public Task<object> Get(GetVariantHlsVideoPlaylist request)
        {
            return GetVariantPlaylistInternal(request, true, "main");
        }

        public Task<object> Get(GetVariantHlsAudioPlaylist request)
        {
            return GetVariantPlaylistInternal(request, false, "main");
        }

        public Task<object> Get(GetHlsVideoSegment request)
        {
            return GetDynamicSegment(request, request.SegmentId);
        }

        public Task<object> Get(GetHlsAudioSegment request)
        {
            return GetDynamicSegment(request, request.SegmentId);
        }

        private async Task<object> GetDynamicSegment(StreamRequest request, string segmentId)
        {
            if ((request.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            var requestedIndex = int.Parse(segmentId, NumberStyles.Integer, UsCulture);

            var state = await GetState(request, cancellationToken).ConfigureAwait(false);

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

            var segmentPath = GetSegmentPath(state, playlistPath, requestedIndex);

            var segmentExtension = GetSegmentFileExtension(state);

            TranscodingJob job = null;

            if (FileSystem.FileExists(segmentPath))
            {
                job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                return await GetSegmentResult(state, playlistPath, segmentPath, requestedIndex, job, cancellationToken).ConfigureAwait(false);
            }

            var transcodingLock = ApiEntryPoint.Instance.GetTranscodingLock(playlistPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            var released = false;
            try
            {
                if (FileSystem.FileExists(segmentPath))
                {
                    job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                    transcodingLock.Release();
                    released = true;
                    return await GetSegmentResult(state, playlistPath, segmentPath, requestedIndex, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var startTranscoding = false;

                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                    var segmentGapRequiringTranscodingChange = 24 / state.SegmentLength;

                    if (currentTranscodingIndex == null)
                    {
                        Logger.Debug("Starting transcoding because currentTranscodingIndex=null");
                        startTranscoding = true;
                    }
                    else if (requestedIndex < currentTranscodingIndex.Value)
                    {
                        Logger.Debug("Starting transcoding because requestedIndex={0} and currentTranscodingIndex={1}", requestedIndex, currentTranscodingIndex);
                        startTranscoding = true;
                    }
                    else if (requestedIndex - currentTranscodingIndex.Value > segmentGapRequiringTranscodingChange)
                    {
                        Logger.Debug("Starting transcoding because segmentGap is {0} and max allowed gap is {1}. requestedIndex={2}", requestedIndex - currentTranscodingIndex.Value, segmentGapRequiringTranscodingChange, requestedIndex);
                        startTranscoding = true;
                    }
                    if (startTranscoding)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            ApiEntryPoint.Instance.KillTranscodingJobs(request.DeviceId, request.PlaySessionId, p => false);

                            if (currentTranscodingIndex.HasValue)
                            {
                                DeleteLastFile(playlistPath, segmentExtension, 0);
                            }

                            request.StartTimeTicks = GetStartPositionTicks(state, requestedIndex);

                            job = await StartFfMpeg(state, playlistPath, cancellationTokenSource).ConfigureAwait(false);
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        //await WaitForMinimumSegmentCount(playlistPath, 1, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                        if (job.TranscodingThrottler != null)
                        {
                            job.TranscodingThrottler.UnpauseTranscoding();
                        }
                    }
                }
            }
            finally
            {
                if (!released)
                {
                    transcodingLock.Release();
                }
            }

            //Logger.Info("waiting for {0}", segmentPath);
            //while (!File.Exists(segmentPath))
            //{
            //    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            //}

            Logger.Info("returning {0}", segmentPath);
            job = job ?? ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
            return await GetSegmentResult(state, playlistPath, segmentPath, requestedIndex, job, cancellationToken).ConfigureAwait(false);
        }

        private const int BufferSize = 81920;

        private long GetStartPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            for (var i = 0; i < requestedIndex; i++)
            {
                startSeconds += lengths[requestedIndex];
            }

            var position = TimeSpan.FromSeconds(startSeconds).Ticks;
            return position;
        }

        private long GetEndPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            for (var i = 0; i <= requestedIndex; i++)
            {
                startSeconds += lengths[requestedIndex];
            }

            var position = TimeSpan.FromSeconds(startSeconds).Ticks;
            return position;
        }

        private double[] GetSegmentLengths(StreamState state)
        {
            var result = new List<double>();

            var ticks = state.RunTimeTicks ?? 0;

            var segmentLengthTicks = TimeSpan.FromSeconds(state.SegmentLength).Ticks;

            while (ticks > 0)
            {
                var length = ticks >= segmentLengthTicks ? segmentLengthTicks : ticks;

                result.Add(TimeSpan.FromTicks(length).TotalSeconds);

                ticks -= length;
            }

            return result.ToArray();
        }

        public int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
        {
            var job = ApiEntryPoint.Instance.GetTranscodingJob(playlist, TranscodingJobType);

            if (job == null || job.HasExited)
            {
                return null;
            }

            var file = GetLastTranscodingFile(playlist, segmentExtension, FileSystem);

            if (file == null)
            {
                return null;
            }

            var playlistFilename = Path.GetFileNameWithoutExtension(playlist);

            var indexString = Path.GetFileNameWithoutExtension(file.Name).Substring(playlistFilename.Length);

            return int.Parse(indexString, NumberStyles.Integer, UsCulture);
        }

        private void DeleteLastFile(string playlistPath, string segmentExtension, int retryCount)
        {
            var file = GetLastTranscodingFile(playlistPath, segmentExtension, FileSystem);

            if (file != null)
            {
                DeleteFile(file, retryCount);
            }
        }

        private void DeleteFile(FileSystemMetadata file, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            try
            {
                FileSystem.DeleteFile(file.FullName);
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);

                Thread.Sleep(100);
                DeleteFile(file, retryCount + 1);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error deleting partial stream file(s) {0}", ex, file.FullName);
            }
        }

        private static FileSystemMetadata GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filePrefix = Path.GetFileNameWithoutExtension(playlist) ?? string.Empty;

            try
            {
                return fileSystem.GetFiles(folder)
                    .Where(i => string.Equals(i.Extension, segmentExtension, StringComparison.OrdinalIgnoreCase) && Path.GetFileNameWithoutExtension(i.Name).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
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
            return GetStartNumber(state.VideoRequest);
        }

        private int GetStartNumber(VideoStreamRequest request)
        {
            var segmentId = "0";

            var segmentRequest = request as GetHlsVideoSegment;
            if (segmentRequest != null)
            {
                segmentId = segmentRequest.SegmentId;
            }

            return int.Parse(segmentId, NumberStyles.Integer, UsCulture);
        }

        private string GetSegmentPath(StreamState state, string playlist, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filename = Path.GetFileNameWithoutExtension(playlist);

            return Path.Combine(folder, filename + index.ToString(UsCulture) + GetSegmentFileExtension(state));
        }

        private async Task<object> GetSegmentResult(StreamState state, string playlistPath,
            string segmentPath,
            int segmentIndex,
            TranscodingJob transcodingJob,
            CancellationToken cancellationToken)
        {
            // If all transcoding has completed, just return immediately
            if (transcodingJob != null && transcodingJob.HasExited && FileSystem.FileExists(segmentPath))
            {
                return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
            }

            var segmentFilename = Path.GetFileName(segmentPath);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var fileStream = GetPlaylistFileStream(playlistPath))
                    {
                        using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                        {
                            var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                            // If it appears in the playlist, it's done
                            if (text.IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                if (FileSystem.FileExists(segmentPath))
                                {
                                    return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
                                }
                                //break;
                            }
                        }
                    }
                }
                catch (IOException)
                {
                    // May get an error if the file is locked
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
        }

        private object GetSegmentResult(StreamState state, string segmentPath, int index, TranscodingJob transcodingJob)
        {
            var segmentEndingPositionTicks = GetEndPositionTicks(state, index);

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = segmentPath,
                FileShare = FileShare.ReadWrite,
                OnComplete = () =>
                {
                    if (transcodingJob != null)
                    {
                        transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                        ApiEntryPoint.Instance.OnTranscodeEndRequest(transcodingJob);
                    }
                }
            }).Result;
        }

        private async Task<object> GetMasterPlaylistInternal(StreamRequest request, string method)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            if (string.IsNullOrEmpty(request.MediaSourceId))
            {
                throw new ArgumentException("MediaSourceId is required");
            }

            var playlistText = string.Empty;

            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                var audioBitrate = state.OutputAudioBitrate ?? 0;
                var videoBitrate = state.OutputVideoBitrate ?? 0;

                playlistText = GetMasterPlaylistFileText(state, videoBitrate + audioBitrate);
            }

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetMasterPlaylistFileText(StreamState state, int totalBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            var isLiveStream = IsLiveStream(state);

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            // Main stream
            var playlistUrl = isLiveStream ? "live.m3u8" : "main.m3u8";

            playlistUrl += queryString;

            var request = state.Request;

            var subtitleStreams = state.MediaSource
                .MediaStreams
                .Where(i => i.IsTextSubtitleStream)
                .ToList();

            var subtitleGroup = subtitleStreams.Count > 0 &&
                request is GetMasterHlsVideoPlaylist &&
                (state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Hls || state.VideoRequest.EnableSubtitlesInManifest) ?
                "subs" :
                null;

            // If we're burning in subtitles then don't add additional subs to the manifest
            if (state.SubtitleStream != null && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode)
            {
                subtitleGroup = null;
            }

            if (!string.IsNullOrWhiteSpace(subtitleGroup))
            {
                AddSubtitles(state, subtitleStreams, builder);
            }

            AppendPlaylist(builder, state, playlistUrl, totalBitrate, subtitleGroup);

            if (EnableAdaptiveBitrateStreaming(state, isLiveStream))
            {
                var requestedVideoBitrate = state.VideoRequest == null ? 0 : state.VideoRequest.VideoBitRate ?? 0;

                // By default, vary by just 200k
                var variation = GetBitrateVariation(totalBitrate);

                var newBitrate = totalBitrate - variation;
                var variantUrl = ReplaceBitrate(playlistUrl, requestedVideoBitrate, requestedVideoBitrate - variation);
                AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);

                variation *= 2;
                newBitrate = totalBitrate - variation;
                variantUrl = ReplaceBitrate(playlistUrl, requestedVideoBitrate, requestedVideoBitrate - variation);
                AppendPlaylist(builder, state, variantUrl, newBitrate, subtitleGroup);
            }

            return builder.ToString();
        }

        private string ReplaceBitrate(string url, int oldValue, int newValue)
        {
            return url.Replace(
                "videobitrate=" + oldValue.ToString(UsCulture),
                "videobitrate=" + newValue.ToString(UsCulture),
                StringComparison.OrdinalIgnoreCase);
        }

        private void AddSubtitles(StreamState state, IEnumerable<MediaStream> subtitles, StringBuilder builder)
        {
            var selectedIndex = state.SubtitleStream == null || state.VideoRequest.SubtitleMethod != SubtitleDeliveryMethod.Hls ? (int?)null : state.SubtitleStream.Index;

            foreach (var stream in subtitles)
            {
                const string format = "#EXT-X-MEDIA:TYPE=SUBTITLES,GROUP-ID=\"subs\",NAME=\"{0}\",DEFAULT={1},FORCED={2},AUTOSELECT=YES,URI=\"{3}\",LANGUAGE=\"{4}\"";

                var name = stream.DisplayTitle;

                var isDefault = selectedIndex.HasValue && selectedIndex.Value == stream.Index;
                var isForced = stream.IsForced;

                var url = string.Format("{0}/Subtitles/{1}/subtitles.m3u8?SegmentLength={2}&api_key={3}",
                    state.Request.MediaSourceId,
                    stream.Index.ToString(UsCulture),
                    30.ToString(UsCulture),
                    AuthorizationContext.GetAuthorizationInfo(Request).Token);

                var line = string.Format(format,
                    name,
                    isDefault ? "YES" : "NO",
                    isForced ? "YES" : "NO",
                    url,
                    stream.Language ?? "Unknown");

                builder.AppendLine(line);
            }
        }

        private bool EnableAdaptiveBitrateStreaming(StreamState state, bool isLiveStream)
        {
            // Within the local network this will likely do more harm than good.
            if (Request.IsLocal || NetworkManager.IsInLocalNetwork(Request.RemoteIp))
            {
                return false;
            }

            var request = state.Request as IMasterHlsRequest;
            if (request != null && !request.EnableAdaptiveBitrateStreaming)
            {
                return false;
            }

            if (isLiveStream || string.IsNullOrWhiteSpace(state.MediaPath))
            {
                // Opening live streams is so slow it's not even worth it
                return false;
            }

            if (string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(state.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!state.IsOutputVideo)
            {
                return false;
            }

            // Having problems in android
            return false;
            //return state.VideoRequest.VideoBitRate.HasValue;
        }

        private void AppendPlaylist(StringBuilder builder, StreamState state, string url, int bitrate, string subtitleGroup)
        {
            var header = "#EXT-X-STREAM-INF:BANDWIDTH=" + bitrate.ToString(UsCulture) + ",AVERAGE-BANDWIDTH=" + bitrate.ToString(UsCulture);

            // tvos wants resolution, codecs, framerate
            //if (state.TargetFramerate.HasValue)
            //{
            //    header += string.Format(",FRAME-RATE=\"{0}\"", state.TargetFramerate.Value.ToString(CultureInfo.InvariantCulture));
            //}

            if (!string.IsNullOrWhiteSpace(subtitleGroup))
            {
                header += string.Format(",SUBTITLES=\"{0}\"", subtitleGroup);
            }

            builder.AppendLine(header);
            builder.AppendLine(url);
        }

        private int GetBitrateVariation(int bitrate)
        {
            // By default, vary by just 50k
            var variation = 50000;

            if (bitrate >= 10000000)
            {
                variation = 2000000;
            }
            else if (bitrate >= 5000000)
            {
                variation = 1500000;
            }
            else if (bitrate >= 3000000)
            {
                variation = 1000000;
            }
            else if (bitrate >= 2000000)
            {
                variation = 500000;
            }
            else if (bitrate >= 1000000)
            {
                variation = 300000;
            }
            else if (bitrate >= 600000)
            {
                variation = 200000;
            }
            else if (bitrate >= 400000)
            {
                variation = 100000;
            }

            return variation;
        }

        private async Task<object> GetVariantPlaylistInternal(StreamRequest request, bool isOutputVideo, string name)
        {
            var state = await GetState(request, CancellationToken.None).ConfigureAwait(false);

            var segmentLengths = GetSegmentLengths(state);

            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");
            builder.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
            builder.AppendLine("#EXT-X-VERSION:3");
            builder.AppendLine("#EXT-X-TARGETDURATION:" + Math.Ceiling(segmentLengths.Length > 0 ? segmentLengths.Max() : state.SegmentLength).ToString(UsCulture));
            builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            //if ((Request.UserAgent ?? string.Empty).IndexOf("roku", StringComparison.OrdinalIgnoreCase) != -1)
            //{
            //    queryString = string.Empty;
            //}

            var index = 0;

            foreach (var length in segmentLengths)
            {
                builder.AppendLine("#EXTINF:" + length.ToString("0.0000", UsCulture) + ",");

                builder.AppendLine(string.Format("hls1/{0}/{1}{2}{3}",

                    name,
                    index.ToString(UsCulture),
                    GetSegmentFileExtension(isOutputVideo),
                    queryString));

                index++;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            var playlistText = builder.ToString();

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        protected override string GetAudioArguments(StreamState state)
        {
            var codec = GetAudioEncoder(state);

            if (!state.IsOutputVideo)
            {
                if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return "-acodec copy";
                }

                var audioTranscodeParams = new List<string>();

                audioTranscodeParams.Add("-acodec " + codec);

                if (state.OutputAudioBitrate.HasValue)
                {
                    audioTranscodeParams.Add("-ab " + state.OutputAudioBitrate.Value.ToString(UsCulture));
                }

                if (state.OutputAudioChannels.HasValue)
                {
                    audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(UsCulture));
                }

                if (state.OutputAudioSampleRate.HasValue)
                {
                    audioTranscodeParams.Add("-ar " + state.OutputAudioSampleRate.Value.ToString(UsCulture));
                }

                audioTranscodeParams.Add("-vn");
                return string.Join(" ", audioTranscodeParams.ToArray());
            }

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
            if (!state.IsOutputVideo)
            {
                return string.Empty;
            }

            var codec = GetVideoEncoder(state);

            var args = "-codec:v:0 " + codec;

            if (state.EnableMpegtsM2TsMode)
            {
                args += " -mpegts_m2ts_mode 1";
            }

            // See if we can save come cpu cycles by avoiding encoding
            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                if (state.VideoStream != null && IsH264(state.VideoStream) && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -bsf:v h264_mp4toannexb";
                }

                args += " -flags -global_header";
            }
            else
            {
                var keyFrameArg = string.Format(" -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                    state.SegmentLength.ToString(UsCulture));

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode;

                args += " " + GetVideoQualityParam(state, GetH264Encoder(state)) + keyFrameArg;

                //args += " -mixed-refs 0 -refs 3 -x264opts b_pyramid=0:weightb=0:weightp=0";

                // Add resolution params, if specified
                if (!hasGraphicalSubs)
                {
                    args += GetOutputSizeParam(state, codec, EnableCopyTs(state));
                }

                // This is for internal graphical subs
                if (hasGraphicalSubs)
                {
                    args += GetGraphicalSubtitleParam(state, codec);
                }

                args += " -flags -global_header";
            }

            if (EnableCopyTs(state) && args.IndexOf("-copyts", StringComparison.OrdinalIgnoreCase) == -1)
            {
                args += " -copyts";
            }

            return args;
        }

        private bool EnableCopyTs(StreamState state)
        {
            //return state.SubtitleStream != null && state.SubtitleStream.IsTextSubtitleStream && state.VideoRequest.SubtitleMethod == SubtitleDeliveryMethod.Encode;
            return true;
        }

        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
        {
            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state, false);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var toTimeParam = string.Empty;
            var timestampOffsetParam = string.Empty;

            if (state.IsOutputVideo && !EnableCopyTs(state) && !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) && (state.Request.StartTimeTicks ?? 0) > 0)
            {
                timestampOffsetParam = " -output_ts_offset " + MediaEncoder.GetTimeParameter(state.Request.StartTimeTicks ?? 0).ToString(CultureInfo.InvariantCulture);
            }

            var mapArgs = state.IsOutputVideo ? GetMapArgs(state) : string.Empty;
            var enableSplittingOnNonKeyFrames = state.VideoRequest.EnableSplittingOnNonKeyFrames && string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase);
            enableSplittingOnNonKeyFrames = false;
            // TODO: check libavformat version for 57 50.100 and use -hls_flags split_by_time
            var hlsProtocolSupportsSplittingByTime = false;

            if (enableSplittingOnNonKeyFrames && !hlsProtocolSupportsSplittingByTime)
            {
                var outputTsArg = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + "%d" + GetSegmentFileExtension(state);

                return string.Format("{0} {10} {1} -map_metadata -1 -threads {2} {3} {4} {5} -f segment -max_delay 5000000 -avoid_negative_ts disabled -start_at_zero -segment_time {6} -break_non_keyframes  1 -segment_format mpegts -segment_list_type m3u8 -segment_start_number {7} -segment_list \"{8}\" -y \"{9}\"",
                    inputModifier,
                    GetInputArgument(state),
                    threads,
                    mapArgs,
                    GetVideoArguments(state),
                    GetAudioArguments(state),
                    state.SegmentLength.ToString(UsCulture),
                    startNumberParam,
                    outputPath,
                    outputTsArg,
                            toTimeParam
                    ).Trim();
            }

            var splitByTime = hlsProtocolSupportsSplittingByTime && enableSplittingOnNonKeyFrames;
            var splitByTimeArg = splitByTime ? " -hls_flags split_by_time" : "";

            return string.Format("{0}{12} {1} -map_metadata -1 -threads {2} {3} {4}{5} {6} -max_delay 5000000 -avoid_negative_ts disabled -start_at_zero -hls_time {7}{8} -start_number {9} -hls_list_size {10} -y \"{11}\"",
                            inputModifier,
                            GetInputArgument(state),
                            threads,
                            mapArgs,
                            GetVideoArguments(state),
                            timestampOffsetParam,
                            GetAudioArguments(state),
                            state.SegmentLength.ToString(UsCulture),
                            splitByTimeArg,
                            startNumberParam,
                            state.HlsListSize.ToString(UsCulture),
                            outputPath,
                            toTimeParam
                            ).Trim();
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected override string GetSegmentFileExtension(StreamState state)
        {
            return GetSegmentFileExtension(state.IsOutputVideo);
        }

        protected string GetSegmentFileExtension(bool isOutputVideo)
        {
            return isOutputVideo ? ".ts" : ".ts";
        }
    }
}