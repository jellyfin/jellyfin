using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.Web;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class AsyncStreamWriterFunc : IStreamWriter, IAsyncStreamWriter, IHasOptions
    {
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private Func<Stream, Task> Writer { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options { get; private set; }

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        public AsyncStreamWriterFunc(Func<Stream, Task> writer, IDictionary<string, string> headers)
        {
            Writer = writer;

            if (headers == null)
            {
                headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            Options = headers;
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            var task = Writer(responseStream);
            Task.WaitAll(task);
        }

        public async Task WriteToAsync(Stream responseStream)
        {
            await Writer(responseStream).ConfigureAwait(false);
        }
    }
}
