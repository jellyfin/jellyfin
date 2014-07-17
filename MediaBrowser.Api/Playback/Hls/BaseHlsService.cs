using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
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
        protected BaseHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder)
            : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
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

            if (File.Exists(playlist))
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
            }
            else
            {
                await ApiEntryPoint.Instance.TranscodingStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (File.Exists(playlist))
                    {
                        ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
                        {
                            await ApiEntryPoint.Instance.KillTranscodingJobs(state.Request.DeviceId, TranscodingJobType.Hls, p => true, false).ConfigureAwait(false);
                        }

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

                        var waitCount = isLive ? 1 : GetSegmentWait();
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
                //var file = request.PlaylistId + Path.GetExtension(Request.PathInfo);

                //file = Path.Combine(ServerConfigurationManager.ApplicationPaths.TranscodingTempPath, file);

                try
                {
                    return ResultFactory.GetStaticFileResult(Request, playlist, FileShare.ReadWrite);
                }
                finally
                {
                    ApiEntryPoint.Instance.OnTranscodeEndRequest(playlist, TranscodingJobType.Hls);
                }
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

            try
            {
                return ResultFactory.GetResult(playlistText, MimeTypes.GetMimeType("playlist.m3u8"), new Dictionary<string, string>());
            }
            finally
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(playlist, TranscodingJobType.Hls);
            }
        }

        /// <summary>
        /// Gets the segment wait.
        /// </summary>
        /// <returns>System.Int32.</returns>
        protected int GetSegmentWait()
        {
            var minimumSegmentCount = 2;
            var quality = GetQualitySetting();

            if (quality == EncodingQuality.HighSpeed || quality == EncodingQuality.HighQuality)
            {
                minimumSegmentCount = 2;
            }

            return minimumSegmentCount;
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

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding)
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

            var args = string.Format("{0} {1} -i {2} -map_metadata -1 -threads {3} {4} {5} -sc_threshold 0 {6} -hls_time {7} -start_number {8} -hls_list_size {9}{10} -y \"{11}\"",
                itsOffset,
                inputModifier,
                GetInputArgument(state),
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
