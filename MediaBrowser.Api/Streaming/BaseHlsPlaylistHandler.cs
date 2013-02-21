using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Class BaseHlsPlaylistHandler
    /// </summary>
    /// <typeparam name="TBaseItemType">The type of the T base item type.</typeparam>
    public abstract class BaseHlsPlaylistHandler<TBaseItemType> : BaseStreamingHandler<TBaseItemType>
        where TBaseItemType : BaseItem, IHasMediaStreams, new()
    {
        /// <summary>
        /// Gets the audio arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected abstract string GetAudioArguments();
        /// <summary>
        /// Gets the video arguments.
        /// </summary>
        /// <returns>System.String.</returns>
        protected abstract string GetVideoArguments();

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType
        {
            get { return TranscodingJobType.Hls; }
        }

        /// <summary>
        /// This isn't needed because we're going to override the whole flow using ProcessRequest
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="contentLength">Length of the content.</param>
        /// <returns>Task.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected override Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? contentLength)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the segment file extension.
        /// </summary>
        /// <value>The segment file extension.</value>
        protected abstract string SegmentFileExtension { get; }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        public override async Task ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            var playlist = OutputFilePath;
            var isPlaylistNewlyCreated = false;

            // If the playlist doesn't already exist, startup ffmpeg
            if (!File.Exists(playlist))
            {
                isPlaylistNewlyCreated = true;
                await StartFFMpeg(playlist).ConfigureAwait(false);
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
                // Dump the stream off to the static file handler to serve statically
                await new StaticFileHandler(Kernel) { ContentType = MimeTypes.GetMimeType("playlist.m3u8"), SourceStream = stream }.ProcessRequest(ctx);
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
        /// Gets all command line arguments to pass to ffmpeg
        /// </summary>
        /// <param name="outputPath">The playlist output path</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <returns>System.String.</returns>
        protected override string GetCommandLineArguments(string outputPath, IIsoMount isoMount)
        {
            var segmentOutputPath = Path.GetDirectoryName(outputPath);
            var segmentOutputName = HlsSegmentHandler.SegmentFilePrefix + Path.GetFileNameWithoutExtension(outputPath);

            segmentOutputPath = Path.Combine(segmentOutputPath, segmentOutputName + "%03d." + SegmentFileExtension.TrimStart('.'));

            var probeSize = Kernel.FFMpegManager.GetProbeSizeArgument(LibraryItem);

            return string.Format("{0} {1} -i {2}{3} -threads 0 {4} {5} {6} -f ssegment -segment_list_flags +live -segment_time 9 -segment_list \"{7}\" \"{8}\"",
                probeSize,
                FastSeekCommandLineParameter,
                GetInputArgument(isoMount),
                SlowSeekCommandLineParameter,
                MapArgs,
                GetVideoArguments(),
                GetAudioArguments(),
                outputPath,
                segmentOutputPath
                ).Trim();
        }

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="playlistFilePath">The playlist file path.</param>
        protected override void DeletePartialStreamFiles(string playlistFilePath)
        {
            var directory = Path.GetDirectoryName(playlistFilePath);
            var name = Path.GetFileNameWithoutExtension(playlistFilePath);

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
