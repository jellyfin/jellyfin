using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class StreamWriter
    /// </summary>
    public class StreamWriter : IStreamWriter, IHasOptions
    {
        private ILogger Logger { get; set; }
        
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private Stream SourceStream { get; set; }

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="logger">The logger.</param>
        public StreamWriter(Stream source, string contentType, ILogger logger)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException("contentType");
            }

            SourceStream = source;
            Logger = logger;

            Options["Content-Type"] = contentType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="logger">The logger.</param>
        public StreamWriter(byte[] source, string contentType, ILogger logger)
            : this(new MemoryStream(source), contentType, logger)
        {
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
