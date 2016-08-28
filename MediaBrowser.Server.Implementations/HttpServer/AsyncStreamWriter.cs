using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Web;
using MediaBrowser.Controller.Net;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class AsyncStreamWriter : IStreamWriter, IAsyncStreamWriter, IHasOptions
    {
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private IAsyncStreamSource _source;

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncStreamWriter" /> class.
        /// </summary>
        public AsyncStreamWriter(IAsyncStreamSource source)
        {
            _source = source;
        }

        public IDictionary<string, string> Options
        {
            get
            {
                var hasOptions = _source as IHasOptions;
                if (hasOptions != null)
                {
                    return hasOptions.Options;
                }

                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            var task = _source.WriteToAsync(responseStream);
            Task.WaitAll(task);
        }

        public async Task WriteToAsync(Stream responseStream)
        {
            await _source.WriteToAsync(responseStream).ConfigureAwait(false);
        }
    }
}
