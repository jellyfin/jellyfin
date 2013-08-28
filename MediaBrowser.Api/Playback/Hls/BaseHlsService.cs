using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Api.Playback.Hls
{
    /// <summary>
    /// Class BaseHlsService
    /// </summary>
    public abstract class BaseHlsService : BaseStreamingService
    {
        /// <summary>
        /// The segment file prefix
        /// </summary>
        public const string SegmentFilePrefix = "hls-";

        protected override string GetOutputFilePath(StreamState state)
        {
            var folder = ApplicationPaths.EncodedMediaCachePath;

            var outputFileExtension = GetOutputFileExtension(state);

            return Path.Combine(folder, SegmentFilePrefix + GetCommandLineArguments("dummy\\dummy", state, false).GetMD5() + (outputFileExtension ?? string.Empty).ToLower());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="isoManager">The iso manager.</param>
        /// <param name="mediaEncoder">The media encoder.</param>
        protected BaseHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder)
            : base(appPaths, userManager, libraryManager, isoManager, mediaEncoder)
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

            // Get the current playlist text and convert to bytes
            var playlistText = await GetPlaylistFileText(playlist, isPlaylistNewlyCreated).ConfigureAwait(false);

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
        /// Gets the current playlist text
        /// </summary>
        /// <param name="playlist">The path to the playlist</param>
        /// <param name="waitForMinimumSegments">Whether or not we should wait until it contains three segments</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetPlaylistFileText(string playlist, bool waitForMinimumSegments)
        {
            string fileText;

            while (true)
            {
                // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                using (var fileStream = new FileStream(playlist, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    using (var reader = new StreamReader(fileStream))
                    {
                        fileText = await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }

                if (!waitForMinimumSegments || CountStringOccurrences(fileText, "#EXTINF:") >= 3)
                {
                    break;
                }

                await Task.Delay(25).ConfigureAwait(false);
            }

            fileText = fileText.Replace(SegmentFilePrefix, "hls/").Replace(".ts", "/stream.ts").Replace(".aac", "/stream.aac").Replace(".mp3", "/stream.mp3");

            return fileText;
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
        /// <param name="performSubtitleConversions">if set to <c>true</c> [perform subtitle conversions].</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state, bool performSubtitleConversions)
        {
            var probeSize = GetProbeSizeArgument(state.Item);

            var audioOnlyPlaylistParams = string.Format(" -threads 0 -vn -codec:a:0 aac -strict experimental -ac 2 -ab 64000 -hls_time 10 -start_number 0 -hls_list_size 1440 \"{0}\"",
                "");

            return string.Format("{0} {1} {2} -i {3}{4} -threads 0 {5} {6} {7} -hls_time 10 -start_number 0 -hls_list_size 1440 \"{8}\" {9}",
                probeSize,
                GetUserAgentParam(state.Item),
                GetFastSeekCommandLineParameter(state.Request),
                GetInputArgument(state.Item, state.IsoMount),
                GetSlowSeekCommandLineParameter(state.Request),
                GetMapArgs(state),
                GetVideoArguments(state, performSubtitleConversions),
                GetAudioArguments(state),
                outputPath,
                audioOnlyPlaylistParams
                ).Trim();
        }
    }
}
