using System;
using System.Collections.Generic;
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
        /// <inheritdoc />
        public UserAgentDelegatingHandler(IApplicationHost applicationHost)
        {
            UserAgentValues = new List<ProductInfoHeaderValue>
            {
                new ProductInfoHeaderValue(applicationHost.Name.Replace(' ', '-'),  applicationHost.ApplicationVersionString),
                new ProductInfoHeaderValue($"({Environment.OSVersion}; {applicationHost.ApplicationUserAgentAddress})")
            };
        }

        /// <summary>
        /// Gets or sets the user agent values.
        /// </summary>
        public List<ProductInfoHeaderValue> UserAgentValues { get; set; }

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
                foreach (var userAgentValue in UserAgentValues)
                {
                    request.Headers.UserAgent.Add(userAgentValue);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
