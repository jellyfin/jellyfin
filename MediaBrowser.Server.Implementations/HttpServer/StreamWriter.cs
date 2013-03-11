using MediaBrowser.Model.Logging;
using ServiceStack.Service;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class StreamWriter
    /// </summary>
    public class StreamWriter : IStreamWriter
    {
        private ILogger Logger { get; set; }
        
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        public Stream SourceStream { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="logger">The logger.</param>
        public StreamWriter(Stream source, ILogger logger)
        {
            SourceStream = source;
            Logger = logger;
        }

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
        private async Task WriteToAsync(Stream responseStream)
        {
            try
            {
                using (var src = SourceStream)
                {
                    await src.CopyToAsync(responseStream).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error streaming media", ex);

                throw;
            }
        }
    }
}
