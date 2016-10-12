using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.IO;
using ServiceStack;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class StreamWriter
    /// </summary>
    public class StreamWriter : IStreamWriter, IAsyncStreamWriter, IHasOptions
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
        public IDictionary<string, string> Options
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

            Options["Content-Type"] = contentType;

            if (source.CanSeek)
            {
                Options["Content-Length"] = source.Length.ToString(UsCulture);
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

            Options["Content-Type"] = contentType;

            Options["Content-Length"] = source.Length.ToString(UsCulture);
        }

        private const int BufferSize = 81920;

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            try
            {
                if (_bytes != null)
                {
                    responseStream.Write(_bytes, 0, _bytes.Length);
                }
                else
                {
                    using (var src = SourceStream)
                    {
                        src.CopyTo(responseStream, BufferSize);
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

        public async Task WriteToAsync(Stream responseStream)
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
