using MediaBrowser.Common.Net;
using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
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
        private readonly HttpResponseInfo _response;

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();

        public StaticRemoteStreamWriter(HttpResponseInfo response)
        {
            _response = response;
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
            using (_response)
            {
                await _response.Content.CopyToAsync(responseStream, 819200).ConfigureAwait(false);
            }
        }
    }
}
