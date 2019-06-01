using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class StreamWriter
    /// </summary>
    public class StreamWriter : IAsyncStreamWriter, IHasHeaders
    {
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private Stream SourceStream { get; set; }

        private byte[] SourceBytes { get; set; }

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Headers => _options;

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="logger">The logger.</param>
        public StreamWriter(Stream source, string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            SourceStream = source;

            Headers["Content-Type"] = contentType;

            if (source.CanSeek)
            {
                Headers[HeaderNames.ContentLength] = source.Length.ToString(CultureInfo.InvariantCulture);
            }

            Headers[HeaderNames.ContentType] = contentType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="contentType">Type of the content.</param>
        public StreamWriter(byte[] source, string contentType, int contentLength)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            SourceBytes = source;

            Headers[HeaderNames.ContentLength] = contentLength.ToString(CultureInfo.InvariantCulture);
            Headers[HeaderNames.ContentType] = contentType;
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            try
            {
                var bytes = SourceBytes;

                if (bytes != null)
                {
                    await responseStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
                else
                {
                    using (var src = SourceStream)
                    {
                        await src.CopyToAsync(responseStream).ConfigureAwait(false);
                    }
                }
            }
            catch
            {
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
