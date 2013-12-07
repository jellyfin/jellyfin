using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class StaticRemoteStreamWriter
    /// </summary>
    public class StaticRemoteStreamWriter : IStreamWriter, IHasOptions
    {
        /// <summary>
        /// The _input stream
        /// </summary>
        private readonly HttpResponseMessage _msg;

        private readonly HttpClient _client;

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticRemoteStreamWriter"/> class.
        /// </summary>
        public StaticRemoteStreamWriter(HttpResponseMessage msg, HttpClient client)
        {
            _msg = msg;
            _client = client;
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Options
        {
            get { return _options; }
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
        public async Task WriteToAsync(Stream responseStream)
        {
            using (_client)
            {
                using (_msg)
                {
                    using (var remoteStream = await _msg.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        await remoteStream.CopyToAsync(responseStream, 819200).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
