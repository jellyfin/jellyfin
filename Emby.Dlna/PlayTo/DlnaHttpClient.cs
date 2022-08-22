#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Emby.Dlna.Common;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    public class DlnaHttpClient
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public DlnaHttpClient(ILogger logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private static string NormalizeServiceUrl(string baseUrl, string serviceUrl)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (serviceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return serviceUrl;
            }

            if (!serviceUrl.StartsWith('/'))
            {
                serviceUrl = "/" + serviceUrl;
            }

            return baseUrl + serviceUrl;
        }

        private async Task<XDocument?> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using var response = await _httpClientFactory.CreateClient(NamedClient.Dlna).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await XDocument.LoadAsync(
                    stream,
                    LoadOptions.None,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Failed to parse response");
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Malformed response: {Content}\n", await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
                }

                return null;
            }
        }

        public async Task<XDocument?> GetDataAsync(string url, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Have to await here instead of returning the Task directly, otherwise request would be disposed too soon
            return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        public async Task<XDocument?> SendCommandAsync(
            string baseUrl,
            DeviceService service,
            string command,
            string postData,
            string? header = null,
            CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, NormalizeServiceUrl(baseUrl, service.ControlUrl))
            {
                Content = new StringContent(postData, Encoding.UTF8, MediaTypeNames.Text.Xml)
            };

            request.Headers.TryAddWithoutValidation(
                "SOAPACTION",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "\"{0}#{1}\"",
                    service.ServiceType,
                    command));
            request.Headers.Pragma.ParseAdd("no-cache");

            if (!string.IsNullOrEmpty(header))
            {
                request.Headers.TryAddWithoutValidation("contentFeatures.dlna.org", header);
            }

            // Have to await here instead of returning the Task directly, otherwise request would be disposed too soon
            return await SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
