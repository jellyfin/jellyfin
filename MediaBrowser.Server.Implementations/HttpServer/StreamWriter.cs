using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class StreamWriter
    /// </summary>
    public class StreamWriter : IAsyncStreamWriter, IHasHeaders
    {
        private ILogger Logger { get; set; }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

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
        public IDictionary<string, string> Headers
        {
            get { return _options; }
        }

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }
        private readonly byte[] _bytes;

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

            Headers["Content-Type"] = contentType;

            if (source.CanSeek)
            {
                Headers["Content-Length"] = source.Length.ToString(UsCulture);
            }
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
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException("contentType");
            }

            _bytes = source;
            Logger = logger;

            Headers["Content-Type"] = contentType;

            Headers["Content-Length"] = source.Length.ToString(UsCulture);
        }

        private const int BufferSize = 81920;

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            try
            {
                if (_bytes != null)
                {
                    await responseStream.WriteAsync(_bytes, 0, _bytes.Length);
                }
                else
                {
                    using (var src = SourceStream)
                    {
                        await src.CopyToAsync(responseStream, BufferSize).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error streaming data", ex);

                if (OnError != null)
                {
                    OnError();
                }

                throw;
            }
            finally
            {
                if (OnComplete != null)
                {
                    OnComplete();
                }
            }
        }
    }
}
