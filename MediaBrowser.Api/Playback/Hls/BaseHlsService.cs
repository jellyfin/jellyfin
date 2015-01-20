using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class BaseHlsService
    /// </summary>
    public abstract class BaseHlsService : BaseStreamingService
    {
        protected BaseHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder, deviceManager)
        {
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetAudioArguments(StreamState state);
        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetVideoArguments(StreamState state);

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected abstract string GetSegmentFileExtension(StreamState state);

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType
        {
            get { return TranscodingJobType.Hls; }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLive">if set to <c>true</c> [is live].</param>
        /// <returns>System.Object.</returns>
        protected object ProcessRequest(StreamRequest request, bool isLive)
        {
            return ProcessRequestAsync(request, isLive).Result;
        }

        /// <summary>
        /// Processes the request async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="isLive">if set to <c>true</c> [is live].</param>
        /// <returns>Task{System.Object}.</returns>
        /// <exception cref="ArgumentException">A video bitrate is required
        /// or
        /// An audio bitrate is required</exception>
        private async Task<object> ProcessRequestAsync(StreamRequest request, bool isLive)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var state = await GetState(request, cancellationTokenSource.Token).ConfigureAwait(false);

            if (isLive)
            {
                state.Request.StartTimeTicks = null;
            }

            var playlist = state.OutputFilePath;

            if (!File.Exists(playlist))
            {
                await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (!File.Exists(playlist))
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            await StartFfMpeg(state, playlist, cancellationTokenSource).ConfigureAwait(false);
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        var waitCount = isLive ? 2 : 2;
                        await WaitForMinimumSegmentCount(playlist, waitCount, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    ApiEntryPoint.Instance.TranscodingStartLock.Release();
                }
            }

            if (isLive)
            {
                return ResultFactory.GetResult(GetLivePlaylistText(playlist, state.SegmentLength), MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
            }

            var audioBitrate = state.OutputAudioBitrate ?? 0;
            var videoBitrate = state.OutputVideoBitrate ?? 0;

            var appendBaselineStream = false;
            var baselineStreamBitrate = 64000;

            var hlsVideoRequest = state.VideoRequest as GetHlsVideoStream;
            if (hlsVideoRequest != null)
            {
                appendBaselineStream = hlsVideoRequest.AppendBaselineStream;
                baselineStreamBitrate = hlsVideoRequest.BaselineStreamAudioBitRate ?? baselineStreamBitrate;
            }

            var playlistText = GetMasterPlaylistFileText(playlist, videoBitrate + audioBitrate, appendBaselineStream, baselineStreamBitrate);

            return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
        }

        private string GetLivePlaylistText(string path, int segmentLength)
        {
            using (var stream = FileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(stream))
                {
                    var text = reader.ReadToEnd();

                    var newDuration = "#EXT-X-TARGETDURATION:" + segmentLength.ToString(UsCulture) + Environment.NewLine + "#EXT-X-ALLOW-CACHE:NO";

                    // ffmpeg pads the reported length by a full second
                    return text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength + 1).ToString(UsCulture), newDuration, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private string GetMasterPlaylistFileText(string firstPlaylist, int bitrate, bool includeBaselineStream, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.15);

            // Main stream
            builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + paddedBitrate.ToString(UsCulture));
            var playlistUrl = "hls/" + Path.GetFileName(firstPlaylist).Replace(".m3u8", "/stream.m3u8");
            builder.AppendLine(playlistUrl);

            // Low bitrate stream
            if (includeBaselineStream)
            {
                builder.AppendLine("#EXT-X-STREAM-INF:PROGRAM-ID=1,BANDWIDTH=" + baselineStreamBitrate.ToString(UsCulture));
                playlistUrl = "hls/" + Path.GetFileName(firstPlaylist).Replace(".m3u8", "-low/stream.m3u8");
                builder.AppendLine(playlistUrl);
            }

            return builder.ToString();
        }

        protected async Task WaitForMinimumSegmentCount(string playlist, int segmentCount, CancellationToken cancellationToken)
        {
            Logger.Debug("Waiting for {0} segments in {1}", segmentCount, playlist);

            while (true)
            {
                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (var fileStream = FileSystem.GetFileStream(playlist, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        var count = 0;

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf("#EXTINF:", StringComparison.OrdinalIgnoreCase) != -1)
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

        protected override string GetCommandLineArguments(string outputPath, string transcodingJobId, StreamState state, bool isEncoding)
        {
            var hlsVideoRequest = state.VideoRequest as GetHlsVideoStream;

            var itsOffsetMs = hlsVideoRequest == null
                                       ? 0
                                       : hlsVideoRequest.TimeStampOffsetMs;

            var itsOffset = itsOffsetMs == 0 ? string.Empty : string.Format("-itsoffset {0} ", TimeSpan.FromMilliseconds(itsOffsetMs).TotalSeconds.ToString(UsCulture));

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var baseUrlParam = string.Empty;

            if (state.Request is GetLiveHlsStream)
            {
                baseUrlParam = string.Format(" -hls_base_url \"{0}/\"",
                    "hls/" + Path.GetFileNameWithoutExtension(outputPath));
            }

            var args = string.Format("{0} {1} {2} -map_metadata -1 -threads {3} {4} {5} -sc_threshold 0 {6} -hls_time {7} -start_number {8} -hls_list_size {9}{10} -y \"{11}\"",
                itsOffset,
                inputModifier,
                GetInputArgument(transcodingJobId, state),
                threads,
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                state.SegmentLength.ToString(UsCulture),
                startNumberParam,
                state.HlsListSize.ToString(UsCulture),
                baseUrlParam,
                outputPath
                ).Trim();

            if (hlsVideoRequest != null)
            {
                if (hlsVideoRequest.AppendBaselineStream)
                {
                    var lowBitratePath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "-low.m3u8");

                    var bitrate = hlsVideoRequest.BaselineStreamAudioBitRate ?? 64000;

                    var lowBitrateParams = string.Format(" -threads {0} -vn -codec:a:0 libmp3lame -ac 2 -ab {1} -hls_time {2} -start_number {3} -hls_list_size {4} -y \"{5}\"",
                        threads,
                        bitrate / 2,
                        state.SegmentLength.ToString(UsCulture),
                        startNumberParam,
                        state.HlsListSize.ToString(UsCulture),
                        lowBitratePath);

                    args += " " + lowBitrateParams;
                }
            }

            return args;
        }

        protected virtual int GetStartNumber(StreamState state)
        {
            return 0;
        }
    }
}
