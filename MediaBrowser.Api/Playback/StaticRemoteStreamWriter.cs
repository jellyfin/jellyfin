using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class StaticRemoteStreamWriter.
    /// </summary>
    public class StaticRemoteStreamWriter : IAsyncStreamWriter, IHasHeaders
    {
        /// <summary>
        /// The _input stream.
        /// </summary>
        private readonly HttpResponseInfo _response;

        /// <summary>
        /// The _options.
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
        public IDictionary<string, string> Headers => _options;

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            using (_response)
            {
                await _response.Content.CopyToAsync(responseStream, 81920, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
