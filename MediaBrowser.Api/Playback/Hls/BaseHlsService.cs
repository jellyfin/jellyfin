using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class BaseHlsService
    /// </summary>
    public abstract class BaseHlsService : BaseStreamingService
    {
        protected override string GetOutputFilePath(StreamState state)
        {
            var folder = ApplicationPaths.EncodedMediaCachePath;

            var outputFileExtension = GetOutputFileExtension(state);

            return Path.Combine(folder, GetCommandLineArguments("dummy\\dummy", state, false).GetMD5() + (outputFileExtension ?? string.Empty).ToLower());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        protected BaseHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IDtoService dtoService)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder, dtoService)
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
        /// <param name="performSubtitleConversion">if set to <c>true</c> [perform subtitle conversion].</param>
        /// <returns>System.String.</returns>
        protected abstract string GetVideoArguments(StreamState state, bool performSubtitleConversion);

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
            var state = GetState(request);

            return ProcessRequestAsync(state).Result;
        }

        /// <summary>
        /// Processes the request async.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>Task{System.Object}.</returns>
        public async Task<object> ProcessRequestAsync(StreamState state)
        {
            if (!state.VideoRequest.VideoBitRate.HasValue)
            {
                throw new ArgumentException("A video bitrate is required");
            }
            if (!state.Request.AudioBitRate.HasValue)
            {
                throw new ArgumentException("An audio bitrate is required");
            }

            var playlist = GetOutputFilePath(state);
            var isPlaylistNewlyCreated = false;

            // If the playlist doesn't already exist, startup ffmpeg
            if (!File.Exists(playlist))
            {
                isPlaylistNewlyCreated = true;
                await StartFfMpeg(state, playlist).ConfigureAwait(false);
            }
            else
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
            }

            if (isPlaylistNewlyCreated)
            {
                await WaitForMinimumSegmentCount(playlist, 3).ConfigureAwait(false);
            }

            var audioBitrate = GetAudioBitrateParam(state) ?? 0;
            var videoBitrate = GetVideoBitrateParam(state) ?? 0;

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

        private string GetMasterPlaylistFileText(string firstPlaylist, int bitrate, bool includeBaselineStream, int baselineStreamBitrate)
        {
            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");

            // Pad a little to satisfy the apple hls validator
            var paddedBitrate = Convert.ToInt32(bitrate * 1.05);

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

        private async Task WaitForMinimumSegmentCount(string playlist, int segmentCount)
        {
            while (true)
            {
                string fileText;

                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (var fileStream = new FileStream(playlist, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
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

                await Task.Delay(25).ConfigureAwait(false);
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

        protected void ExtendHlsTimer(string itemId, string playlistId)
        {
            var normalizedPlaylistId = playlistId.Replace("-low", string.Empty);

            foreach (var playlist in Directory.EnumerateFiles(ApplicationPaths.EncodedMediaCachePath, "*.m3u8")
                .Where(i => i.IndexOf(normalizedPlaylistId, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList())
            {
                ApiEntryPoint.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);

                // Avoid implicitly captured closure
                var playlist1 = playlist;

                Task.Run(async () =>
                {
                    // This is an arbitrary time period corresponding to when the request completes.
                    await Task.Delay(30000).ConfigureAwait(false);

                    ApiEntryPoint.Instance.OnTranscodeEndRequest(playlist1, TranscodingJobType.Hls);
                });
            }
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="performSubtitleConversions">if set to <c>true</c> [perform subtitle conversions].</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool performSubtitleConversions)
        {
            var probeSize = GetProbeSizeArgument(state.Item);

            var hlsVideoRequest = state.VideoRequest as GetHlsVideoStream;

            var itsOffsetMs = hlsVideoRequest == null
                                       ? 0
                                       : ((GetHlsVideoStream) state.VideoRequest).TimeStampOffsetMs;

            var itsOffset = itsOffsetMs == 0 ? string.Empty : string.Format("-itsoffset {0} ", TimeSpan.FromMilliseconds(itsOffsetMs).TotalSeconds);

            var args = string.Format("{0}{1} {2} {3} -i {4}{5} -threads 0 {6} {7} -sc_threshold 0 {8} -hls_time 10 -start_number 0 -hls_list_size 1440 \"{9}\"",
                itsOffset,
                probeSize,
                GetUserAgentParam(state.Item),
                GetFastSeekCommandLineParameter(state.Request),
                GetInputArgument(state.Item, state.IsoMount),
                GetSlowSeekCommandLineParameter(state.Request),
                GetMapArgs(state),
                GetVideoArguments(state, performSubtitleConversions),
                GetAudioArguments(state),
                outputPath
                ).Trim();

            if (hlsVideoRequest != null)
            {
                if (hlsVideoRequest.AppendBaselineStream && state.Item is Video)
                {
                    var lowBitratePath = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + "-low.m3u8");

                    var bitrate = hlsVideoRequest.BaselineStreamAudioBitRate ?? 64000;

                    var lowBitrateParams = string.Format(" -threads 0 -vn -codec:a:0 libmp3lame -ac 2 -ab {1} -hls_time 10 -start_number 0 -hls_list_size 1440 \"{0}\"",
                        lowBitratePath,
                        bitrate / 2);

                    args += " " + lowBitrateParams;
                }
            }

            return args;
        }
    }
}
