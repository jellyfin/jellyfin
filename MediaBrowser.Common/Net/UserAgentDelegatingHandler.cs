using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// User agent delegating handler.
    /// Adds User-Agent header to all requests.
    /// </summary>
    public class UserAgentDelegatingHandler : DelegatingHandler
    {
        private readonly ProductInfoHeaderValue[] _userAgentValues;

        /// <inheritdoc />
        public UserAgentDelegatingHandler(IApplicationHost applicationHost)
        {
            _userAgentValues = new []
            {
                new ProductInfoHeaderValue(applicationHost.Name.Replace(' ', '-'),  applicationHost.ApplicationVersionString),
                new ProductInfoHeaderValue($"({Environment.OSVersion}; {applicationHost.ApplicationUserAgentAddress})")
            };
        }

        /// <summary>
        /// Send request message.
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="HttpResponseMessage"/>.</returns>
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.UserAgent.Count == 0)
            {
                for (var i = 0; i < _userAgentValues.Length; i++)
                {
                    request.Headers.UserAgent.Add(_userAgentValues[i]);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
