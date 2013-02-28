using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Hls
{
    public abstract class BaseHlsService : BaseStreamingService
    {
        /// <summary>
        /// The segment file prefix
        /// </summary>
        public const string SegmentFilePrefix = "segment-";

        protected BaseHlsService(IServerApplicationPaths appPaths, IUserManager userManager, ILibraryManager libraryManager)
            : base(appPaths, userManager, libraryManager)
        {
        }

        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected abstract string GetAudioArguments(StreamState state);
        /// <summary>
        /// Gets the video arguments.
        /// </summary>
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
        /// <param name="state">The state.</param>
        /// <returns>System.Object.</returns>
        protected object ProcessRequest(StreamState state)
        {
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
                await StartFFMpeg(state, playlist).ConfigureAwait(false);
            }
            else
            {
                Plugin.Instance.OnTranscodeBeginRequest(playlist, TranscodingJobType.Hls);
            }

            // Get the current playlist text and convert to bytes
            var playlistText = await GetPlaylistFileText(playlist, isPlaylistNewlyCreated).ConfigureAwait(false);

            var content = Encoding.UTF8.GetBytes(playlistText);

            var stream = new MemoryStream(content);

            try
            {
                Response.ContentType = MimeTypes.GetMimeType("playlist.m3u8");
                return new StreamWriter(stream);
            }
            finally
            {
                Plugin.Instance.OnTranscodeEndRequest(playlist, TranscodingJobType.Hls);
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

            // The segement paths within the playlist are phsyical, so strip that out to make it relative
            fileText = fileText.Replace(Path.GetDirectoryName(playlist) + Path.DirectorySeparatorChar, string.Empty);

            // Even though we specify target duration of 9, ffmpeg seems unable to keep all segments under that amount
            fileText = fileText.Replace("#EXT-X-TARGETDURATION:9", "#EXT-X-TARGETDURATION:10");

            // It's considered live while still encoding (EVENT). Once the encoding has finished, it's video on demand (VOD).
            var playlistType = fileText.IndexOf("#EXT-X-ENDLIST", StringComparison.OrdinalIgnoreCase) == -1 ? "EVENT" : "VOD";

            // Add event type at the top
            fileText = fileText.Replace("#EXT-X-ALLOWCACHE", "#EXT-X-PLAYLIST-TYPE:" + playlistType + Environment.NewLine + "#EXT-X-ALLOWCACHE");

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
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, StreamState state)
        {
            var segmentOutputPath = Path.GetDirectoryName(outputPath);
            var segmentOutputName = SegmentFilePrefix + Path.GetFileNameWithoutExtension(outputPath);

            segmentOutputPath = Path.Combine(segmentOutputPath, segmentOutputName + "%03d." + GetSegmentFileExtension(state).TrimStart('.'));

            var kernel = (Kernel)Kernel;

            var probeSize = kernel.FFMpegManager.GetProbeSizeArgument(state.Item);

            return string.Format("{0} {1} -i {2}{3} -threads 0 {4} {5} {6} -f ssegment -segment_list_flags +live -segment_time 9 -segment_list \"{7}\" \"{8}\"",
                probeSize,
                GetFastSeekCommandLineParameter(state.Request),
                GetInputArgument(state.Item, state.IsoMount),
                GetSlowSeekCommandLineParameter(state.Request),
                GetMapArgs(state),
                GetVideoArguments(state),
                GetAudioArguments(state),
                outputPath,
                segmentOutputPath
                ).Trim();
        }

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        protected override void DeletePartialStreamFiles(string outputFilePath)
        {
            var directory = Path.GetDirectoryName(outputFilePath);
            var name = Path.GetFileNameWithoutExtension(outputFilePath);

            var filesToDelete = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(f => f.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                .ToList();

            foreach (var file in filesToDelete)
            {
                try
                {
                    Logger.Info("Deleting HLS file {0}", file);
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error deleting HLS file {0}", ex, file);
                }
            }
        }
    }
}
