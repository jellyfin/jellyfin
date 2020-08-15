using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// The hls helpers.
    /// </summary>
    public static class HlsHelpers
    {
        /// <summary>
        /// Waits for a minimum number of segments to be available.
        /// </summary>
        /// <param name="playlist">The playlist string.</param>
        /// <param name="segmentCount">The segment count.</param>
        /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="Task"/> indicating the waiting process.</returns>
        public static async Task WaitForMinimumSegmentCount(string playlist, int? segmentCount, ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogDebug("Waiting for {0} segments in {1}", segmentCount, playlist);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Need to use FileShare.ReadWrite because we're reading the file at the same time it's being written
                    var fileStream = new FileStream(
                        playlist,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite,
                        IODefaults.FileStreamBufferSize,
                        FileOptions.SequentialScan);
                    await using (fileStream.ConfigureAwait(false))
                    {
                        using var reader = new StreamReader(fileStream);
                        var count = 0;

                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync().ConfigureAwait(false);

                            if (line.IndexOf("#EXTINF:", StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                count++;
                                if (count >= segmentCount)
                                {
                                    logger.LogDebug("Finished waiting for {0} segments in {1}", segmentCount, playlist);
                                    return;
                                }
                            }
                        }
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }
                catch (IOException)
                {
                    // May get an error if the file is locked
                }

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the hls playlist text.
        /// </summary>
        /// <param name="path">The path to the playlist file.</param>
        /// <param name="segmentLength">The segment length.</param>
        /// <returns>The playlist text as a string.</returns>
        public static string GetLivePlaylistText(string path, int segmentLength)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            var text = reader.ReadToEnd();

            text = text.Replace("#EXTM3U", "#EXTM3U\n#EXT-X-PLAYLIST-TYPE:EVENT", StringComparison.InvariantCulture);

            var newDuration = "#EXT-X-TARGETDURATION:" + segmentLength.ToString(CultureInfo.InvariantCulture);

            text = text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength - 1).ToString(CultureInfo.InvariantCulture), newDuration, StringComparison.OrdinalIgnoreCase);
            // text = text.Replace("#EXT-X-TARGETDURATION:" + (segmentLength + 1).ToString(CultureInfo.InvariantCulture), newDuration, StringComparison.OrdinalIgnoreCase);

            return text;
        }
    }
}
