using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
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

    [Route("/Videos/{Id}/hls1/{PlaylistId}/{SegmentId}.{SegmentContainer}", "GET")]
    public class GetHlsVideoSegment : VideoStreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    [Route("/Audio/{Id}/hls1/{PlaylistId}/{SegmentId}.{SegmentContainer}", "GET")]
    public class GetHlsAudioSegment : StreamRequest
    {
        public string PlaylistId { get; set; }

        /// <summary>
        /// Gets or sets the segment id.
        /// </summary>
        /// <value>The segment id.</value>
        public string SegmentId { get; set; }
    }

    [Authenticated]
    public class DynamicHlsService : BaseHlsService
    {

        public DynamicHlsService(
            IServerConfigurationManager serverConfig,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            ISubtitleEncoder subtitleEncoder,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IJsonSerializer jsonSerializer,
            IAuthorizationContext authorizationContext,
            INetworkManager networkManager)
            : base(serverConfig,
                userManager,
                libraryManager,
                isoManager,
                mediaEncoder,
                fileSystem,
                dlnaManager,
                subtitleEncoder,
                deviceManager,
                mediaSourceManager,
                jsonSerializer,
                authorizationContext)
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

            var segmentExtension = GetSegmentFileExtension(state.Request);

            TranscodingJob job = null;

            if (File.Exists(segmentPath))
            {
                job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, requestedIndex, job, cancellationToken).ConfigureAwait(false);
            }

            var transcodingLock = ApiEntryPoint.Instance.GetTranscodingLock(playlistPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            var released = false;
            var startTranscoding = false;

            try
            {
                if (File.Exists(segmentPath))
                {
                    job = ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
                    transcodingLock.Release();
                    released = true;
                    return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, requestedIndex, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                    var segmentGapRequiringTranscodingChange = 24 / state.SegmentLength;

                    if (currentTranscodingIndex == null)
                    {
                        Logger.LogDebug("Starting transcoding because currentTranscodingIndex=null");
                        startTranscoding = true;
                    }
                    else if (requestedIndex < currentTranscodingIndex.Value)
                    {
                        Logger.LogDebug("Starting transcoding because requestedIndex={0} and currentTranscodingIndex={1}", requestedIndex, currentTranscodingIndex);
                        startTranscoding = true;
                    }
                    else if (requestedIndex - currentTranscodingIndex.Value > segmentGapRequiringTranscodingChange)
                    {
                        Logger.LogDebug("Starting transcoding because segmentGap is {0} and max allowed gap is {1}. requestedIndex={2}", requestedIndex - currentTranscodingIndex.Value, segmentGapRequiringTranscodingChange, requestedIndex);
                        startTranscoding = true;
                    }
                    if (startTranscoding)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            await ApiEntryPoint.Instance.KillTranscodingJobs(request.DeviceId, request.PlaySessionId, p => false);

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
                            await job.TranscodingThrottler.UnpauseTranscoding();
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

            //Logger.LogInformation("waiting for {0}", segmentPath);
            //while (!File.Exists(segmentPath))
            //{
            //    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            //}

            Logger.LogInformation("returning {0}", segmentPath);
            job = job ?? ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);
            return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, requestedIndex, job, cancellationToken).ConfigureAwait(false);
        }

        private const int BufferSize = 81920;

        private long GetStartPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            if (requestedIndex >= lengths.Length)
            {
                var msg = string.Format("Invalid segment index requested: {0} - Segment count: {1}", requestedIndex, lengths.Length);
                throw new ArgumentException(msg);
            }

            for (var i = 0; i < requestedIndex; i++)
            {
                startSeconds += lengths[i];
            }

            var position = TimeSpan.FromSeconds(startSeconds).Ticks;
            return position;
        }

        private long GetEndPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            if (requestedIndex >= lengths.Length)
            {
                var msg = string.Format("Invalid segment index requested: {0} - Segment count: {1}", requestedIndex, lengths.Length);
                throw new ArgumentException(msg);
            }

            for (var i = 0; i <= requestedIndex; i++)
            {
                startSeconds += lengths[i];
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
                DeleteFile(file.FullName, retryCount);
            }
        }

        private void DeleteFile(string path, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            Logger.LogDebug("Deleting partial HLS file {path}", path);

            try
            {
                FileSystem.DeleteFile(path);
            }
            catch (IOException ex)
            {
                Logger.LogError(ex, "Error deleting partial stream file(s) {path}", path);

                var task = Task.Delay(100);
                Task.WaitAll(task);
                DeleteFile(path, retryCount + 1);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting partial stream file(s) {path}", path);
            }
        }

        private static FileSystemMetadata GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filePrefix = Path.GetFileNameWithoutExtension(playlist) ?? string.Empty;

            try
            {
                return fileSystem.GetFiles(folder, new[] { segmentExtension }, true, false)
                    .Where(i => Path.GetFileNameWithoutExtension(i.Name).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (IOException)
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

            return Path.Combine(folder, filename + index.ToString(UsCulture) + GetSegmentFileExtension(state.Request));
        }

        private async Task<object> GetSegmentResult(StreamState state,
            string playlistPath,
            string segmentPath,
            string segmentExtension,
            int segmentIndex,
            TranscodingJob transcodingJob,
            CancellationToken cancellationToken)
        {
            var segmentFileExists = File.Exists(segmentPath);

            // If all transcoding has completed, just return immediately
            if (transcodingJob != null && transcodingJob.HasExited && segmentFileExists)
            {
                return await GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob).ConfigureAwait(false);
            }

            if (segmentFileExists)
            {
                var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);

                // If requested segment is less than transcoding position, we can't transcode backwards, so assume it's ready
                if (segmentIndex < currentTranscodingIndex)
                {
                    return await GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob).ConfigureAwait(false);
                }
            }

            var segmentFilename = Path.GetFileName(segmentPath);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var text = File.ReadAllText(playlistPath, Encoding.UTF8);

                    // If it appears in the playlist, it's done
                    if (text.IndexOf(segmentFilename, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        if (!segmentFileExists)
                        {
                            segmentFileExists = File.Exists(segmentPath);
                        }
                        if (segmentFileExists)
                        {
                            return await GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob).ConfigureAwait(false);
                        }
                        //break;
                    }
                }
                catch (IOException)
                {
                    // May get an error if the file is locked
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob).ConfigureAwait(false);
        }

        private Task<object> GetSegmentResult(StreamState state, string segmentPath, int index, TranscodingJob transcodingJob)
        {
            var segmentEndingPositionTicks = GetEndPositionTicks(state, index);

            return ResultFactory.GetStaticFileResult(Request, new StaticFileResultOptions
            {
                Path = segmentPath,
                FileShare = FileShareMode.ReadWrite,
                OnComplete = () =>
                {
                    if (transcodingJob != null)
                    {
                        transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                        ApiEntryPoint.Instance.OnTranscodeEndRequest(transcodingJob);
                    }
                }
            });
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

            var isLiveStream = state.IsSegmentedLiveStream;

            var queryStringIndex = Request.RawUrl.IndexOf('?');
            var queryString = queryStringIndex == -1 ? string.Empty : Request.RawUrl.Substring(queryStringIndex);

            // from universal audio service
            if (queryString.IndexOf("SegmentContainer", StringComparison.OrdinalIgnoreCase) == -1 && !string.IsNullOrWhiteSpace(state.Request.SegmentContainer))
            {
                queryString += "&SegmentContainer=" + state.Request.SegmentContainer;
            }
            // from universal audio service
            if (!string.IsNullOrWhiteSpace(state.Request.TranscodeReasons) && queryString.IndexOf("TranscodeReasons=", StringComparison.OrdinalIgnoreCase) == -1)
            {
                queryString += "&TranscodeReasons=" + state.Request.TranscodeReasons;
            }

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
                (state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Hls || state.VideoRequest.EnableSubtitlesInManifest) ?
                "subs" :
                null;

            // If we're burning in subtitles then don't add additional subs to the manifest
            if (state.SubtitleStream != null && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode)
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
            var selectedIndex = state.SubtitleStream == null || state.SubtitleDeliveryMethod != SubtitleDeliveryMethod.Hls ? (int?)null : state.SubtitleStream.Index;

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
                builder.AppendLine("#EXTINF:" + length.ToString("0.0000", UsCulture) + ", nodesc");

                builder.AppendLine(string.Format("hls1/{0}/{1}{2}{3}",

                    name,
                    index.ToString(UsCulture),
                    GetSegmentFileExtension(request),
                    queryString));

                index++;
            }

            builder.AppendLine("#EXT-X-ENDLIST");

            var playlistText = builder.ToString();

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        protected override string GetAudioArguments(StreamState state, EncodingOptions encodingOptions)
        {
            var audioCodec = EncodingHelper.GetAudioEncoder(state);

            if (!state.IsOutputVideo)
            {
                if (string.Equals(audioCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    return "-acodec copy";
                }

                var audioTranscodeParams = new List<string>();

                audioTranscodeParams.Add("-acodec " + audioCodec);

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

            if (string.Equals(audioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                var videoCodec = EncodingHelper.GetVideoEncoder(state, encodingOptions);

                if (string.Equals(videoCodec, "copy", StringComparison.OrdinalIgnoreCase) && state.EnableBreakOnNonKeyFrames(videoCodec))
                {
                    return "-codec:a:0 copy -copypriorss:a:0 0";
                }

                return "-codec:a:0 copy";
            }

            var args = "-codec:a:0 " + audioCodec;

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

            if (state.OutputAudioSampleRate.HasValue)
            {
                args += " -ar " + state.OutputAudioSampleRate.Value.ToString(UsCulture);
            }

            args += " " + EncodingHelper.GetAudioFilterParam(state, encodingOptions, true);

            return args;
        }

        protected override string GetVideoArguments(StreamState state, EncodingOptions encodingOptions)
        {
            if (!state.IsOutputVideo)
            {
                return string.Empty;
            }

            var codec = EncodingHelper.GetVideoEncoder(state, encodingOptions);

            var args = "-codec:v:0 " + codec;

            // if (state.EnableMpegtsM2TsMode)
            // {
            //     args += " -mpegts_m2ts_mode 1";
            // }

            // See if we can save come cpu cycles by avoiding encoding
            if (string.Equals(codec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                if (state.VideoStream != null && EncodingHelper.IsH264(state.VideoStream) && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -bsf:v h264_mp4toannexb";
                }

                //args += " -flags -global_header";
            }
            else
            {
                var keyFrameArg = string.Format(" -force_key_frames \"expr:gte(t,n_forced*{0})\"",
                    state.SegmentLength.ToString(UsCulture));

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                args += " " + EncodingHelper.GetVideoQualityParam(state, codec, encodingOptions, GetDefaultH264Preset()) + keyFrameArg;

                //args += " -mixed-refs 0 -refs 3 -x264opts b_pyramid=0:weightb=0:weightp=0";

                // Add resolution params, if specified
                if (!hasGraphicalSubs)
                {
                    args += EncodingHelper.GetOutputSizeParam(state, encodingOptions, codec, true);
                }

                // This is for internal graphical subs
                if (hasGraphicalSubs)
                {
                    args += EncodingHelper.GetGraphicalSubtitleParam(state, encodingOptions, codec);
                }

                //args += " -flags -global_header";
            }

            if (args.IndexOf("-copyts", StringComparison.OrdinalIgnoreCase) == -1)
            {
                args += " -copyts";
            }

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                args += " -vsync " + state.OutputVideoSync;
            }

            args += EncodingHelper.GetOutputFFlags(state);

            return args;
        }

        protected override string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding)
        {
            var videoCodec = EncodingHelper.GetVideoEncoder(state, encodingOptions);

            var threads = EncodingHelper.GetNumberOfThreads(state, encodingOptions, videoCodec);

            var inputModifier = EncodingHelper.GetInputModifier(state, encodingOptions);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumber = GetStartNumber(state);
            var startNumberParam = isEncoding ? startNumber.ToString(UsCulture) : "0";

            var mapArgs = state.IsOutputVideo ? EncodingHelper.GetMapArgs(state) : string.Empty;

            var outputTsArg = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + "%d" + GetSegmentFileExtension(state.Request);

            var timeDeltaParam = string.Empty;

            if (isEncoding && state.TargetFramerate > 0)
            {
                float startTime = 1 / (state.TargetFramerate.Value * 2);
                timeDeltaParam = "-segment_time_delta " + Math.Round(startTime, 3).ToString(CultureInfo.InvariantCulture);
            }

            var segmentFormat = GetSegmentFileExtension(state.Request).TrimStart('.');
            if (string.Equals(segmentFormat, "ts", StringComparison.OrdinalIgnoreCase))
            {
                segmentFormat = "mpegts";
            }

            var breakOnNonKeyFrames = state.EnableBreakOnNonKeyFrames(videoCodec);

            var breakOnNonKeyFramesArg = breakOnNonKeyFrames ? " -break_non_keyframes 1" : "";

            return string.Format("{0} {1} -map_metadata -1 -map_chapters -1 -threads {2} {3} {4} {5} -f segment -max_delay 5000000 -avoid_negative_ts disabled -start_at_zero -segment_time {6} {10} -individual_header_trailer 0{12} -segment_format {11} -segment_list_type m3u8 -segment_start_number {7} -segment_list \"{8}\" -y \"{9}\"",
                inputModifier,
                EncodingHelper.GetInputArgument(state, encodingOptions),
                threads,
                mapArgs,
                GetVideoArguments(state, encodingOptions),
                GetAudioArguments(state, encodingOptions),
                state.SegmentLength.ToString(UsCulture),
                startNumberParam,
                outputPath,
                outputTsArg,
                timeDeltaParam,
                segmentFormat,
                breakOnNonKeyFramesArg
            ).Trim();
        }
    }
}
