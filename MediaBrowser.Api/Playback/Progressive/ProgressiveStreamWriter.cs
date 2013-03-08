using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using ServiceStack.Service;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback.Progressive
{
    public class ProgressiveStreamWriter : IStreamWriter
    {
        public string Path { get; set; }
        public StreamState State { get; set; }
        public ILogger Logger { get; set; }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            var task = WriteToAsync(responseStream);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Writes to async.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Task.</returns>
        public async Task WriteToAsync(Stream responseStream)
        {
            try
            {
                await StreamFile(Path, responseStream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error streaming media", ex);
            }
            finally
            {
                ApiEntryPoint.Instance.OnTranscodeEndRequest(Path, TranscodingJobType.Progressive);
            }
        }

        /// <summary>
        /// Streams the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="outputStream">The output stream.</param>
        /// <returns>Task{System.Boolean}.</returns>
        private async Task StreamFile(string path, Stream outputStream)
        {
            var eofCount = 0;
            long position = 0;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
            {
                while (eofCount < 15)
                {
                    await fs.CopyToAsync(outputStream).ConfigureAwait(false);

                    var fsPosition = fs.Position;

                    var bytesRead = fsPosition - position;

                    //Logger.LogInfo("Streamed {0} bytes from file {1}", bytesRead, path);

                    if (bytesRead == 0)
                    {
                        eofCount++;
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    else
                    {
                        eofCount = 0;
                    }

                    position = fsPosition;
                }
            }
        }
    }
}
