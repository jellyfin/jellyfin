using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Streaming
{
    /// <summary>
    /// Class BaseMediaHandler
    /// </summary>
    /// <typeparam name="TBaseItemType">The type of the T base item type.</typeparam>
    public abstract class BaseProgressiveStreamingHandler<TBaseItemType> : BaseStreamingHandler<TBaseItemType>
        where TBaseItemType : BaseItem, IHasMediaStreams, new()
    {
        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected override TranscodingJobType TranscodingJobType
        {
            get { return TranscodingJobType.Progressive; }
        }

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        public override Task ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            if (!RequiresConversion())
            {
                return new StaticFileHandler(Kernel)
                {
                    Path = LibraryItem.Path

                }.ProcessRequest(ctx);
            }

            var outputPath = OutputFilePath;

            if (File.Exists(outputPath) && !Plugin.Instance.HasActiveTranscodingJob(outputPath, TranscodingJobType.Progressive))
            {
                return new StaticFileHandler(Kernel)
                {
                    Path = outputPath

                }.ProcessRequest(ctx);
            }

            return base.ProcessRequest(ctx);
        }

        /// <summary>
        /// Requireses the conversion.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected bool RequiresConversion()
        {
            return !GetBoolQueryStringParam("static");
        }

        /// <summary>
        /// Writes the response to output stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="contentLength">Length of the content.</param>
        /// <returns>Task.</returns>
        protected override async Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? contentLength)
        {
            // Use the command line args with a dummy playlist path
            var outputPath = OutputFilePath;

            if (!File.Exists(outputPath))
            {
                await StartFFMpeg(outputPath).ConfigureAwait(false);
            }
            else
            {
                Plugin.Instance.OnTranscodeBeginRequest(outputPath, TranscodingJobType.Progressive);
            }

            try
            {
                await StreamFile(outputPath, stream).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error streaming media", ex);
            }
            finally
            {
                Plugin.Instance.OnTranscodeEndRequest(outputPath, TranscodingJobType.Progressive);
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

        /// <summary>
        /// Deletes the partial stream files.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        protected override void DeletePartialStreamFiles(string outputFilePath)
        {
            File.Delete(outputFilePath);
        }
    }
}
