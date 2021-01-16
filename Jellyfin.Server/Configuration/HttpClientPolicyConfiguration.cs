using System;
using System.Net.Http;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;

namespace Jellyfin.Server.Configuration
{
    /// <summary>
    /// HttpClient policy configuration.
    /// </summary>
    public static class HttpClientPolicyConfiguration
    {
        /// <summary>
        /// Gets the HttpClient retry policy.
        /// </summary>
        /// <returns>The <see cref="IAsyncPolicy"/>.</returns>
        public static IAsyncPolicy<HttpResponseMessage> GetHttpClientRetryPolicy()
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(1), retryCount: 5);
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(delay);
        }
    }
}