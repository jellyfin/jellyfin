using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
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
        protected BaseHlsService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, ILiveTvManager liveTvManager, IDlnaManager dlnaManager, IChannelManager channelManager, ISubtitleEncoder subtitleEncoder) : base(serverConfig, userManager, libraryManager, isoManager, mediaEncoder, fileSystem, liveTvManager, dlnaManager, channelManager, subtitleEncoder)
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
        /// <returns>System.Object.</returns>
        protected object ProcessRequest(StreamRequest request)
        {
            return ProcessRequestAsync(request).Result;
        }

        private static readonly SemaphoreSlim FfmpegStartLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// Processes the request async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{System.Object}.</returns>
        /// <exception cref="ArgumentException">
        /// A video bitrate is required
        /// or
        /// An audio bitrate is required
        /// </exception>
        private async Task<object> ProcessRequestAsync(StreamRequest request)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            var state = GetState(request, cancellationTokenSource.Token).Result;

            var playlist = state.OutputFilePath;

            if (File.Exists(playlist))
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
            }
            else
            {
                await FfmpegStartLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (File.Exists(playlist))
                    {
                        ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
                    }
                    else
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

                        await WaitForMinimumSegmentCount(playlist, GetSegmentWait(), cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    FfmpegStartLock.Release();
                }
            }

            int audioBitrate;
            int videoBitrate;
            GetPlaylistBitrates(state, out audioBitrate, out videoBitrate);

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
            var minimumSegmentCount = 3;
            var quality = GetQualitySetting();

            if (quality == EncodingQuality.HighSpeed || quality == EncodingQuality.HighQuality)
            {
                minimumSegmentCount = 2;
            }

            return minimumSegmentCount;
        }

        /// <summary>
        /// Gets the playlist bitrates.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="audioBitrate">The audio bitrate.</param>
        /// <param name="videoBitrate">The video bitrate.</param>
        protected void GetPlaylistBitrates(StreamState state, out int audioBitrate, out int videoBitrate)
        {
            var audioBitrateParam = state.OutputAudioBitrate;
            var videoBitrateParam = state.OutputVideoBitrate;

            if (!audioBitrateParam.HasValue)
            {
                if (state.AudioStream != null)
                {
                    audioBitrateParam = state.AudioStream.BitRate;
                }
            }

            if (!videoBitrateParam.HasValue)
            {
                if (state.VideoStream != null)
                {
                    videoBitrateParam = state.VideoStream.BitRate;
                }
            }

            audioBitrate = audioBitrateParam ?? 0;
            videoBitrate = videoBitrateParam ?? 0;
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
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                string fileText;

                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (var fileStream = FileSystem.GetFileStream(playlist, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        fileText = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }

                if (CountStringOccurrences(fileText, "#EXTINF:") >= segmentCount)
                {
                    break;
                }

                await Task.Delay(25, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Count occurrences of strings.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="pattern">The pattern.</param>
        /// <returns>System.Int32.</returns>
        private static int CountStringOccurrences(string text, string pattern)
        {
            // Loop through all instances of the string 'text'.
            var count = 0;
            var i = 0;
            while ((i = text.IndexOf(pattern, i, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                i += pattern.Length;
                count++;
            }
            return count;
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
                                       : ((GetHlsVideoStream)state.VideoRequest).TimeStampOffsetMs;

            var itsOffset = itsOffsetMs == 0 ? string.Empty : string.Format("-itsoffset {0} ", TimeSpan.FromMilliseconds(itsOffsetMs).TotalSeconds.ToString(UsCulture));

            var threads = GetNumberOfThreads(state, false);

            var inputModifier = GetInputModifier(state);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? GetStartNumber(state).ToString(UsCulture) : "0";

            var args = string.Format("{0} {1} -i {2} -map_metadata -1 -threads {3} {4} {5} -sc_threshold 0 {6} -hls_time {7} -start_number {8} -hls_list_size {9} -y \"{10}\"",
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
