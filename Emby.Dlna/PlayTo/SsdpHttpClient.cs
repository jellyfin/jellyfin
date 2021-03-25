#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Emby.Dlna.Common;
using MediaBrowser.Common.Net;

namespace Emby.Dlna.PlayTo
{
    public class SsdpHttpClient
    {
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";
        private const string FriendlyName = "Jellyfin";

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IHttpClientFactory _httpClientFactory;

        public SsdpHttpClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<XDocument> SendCommandAsync(
            string baseUrl,
            DeviceService service,
            string command,
            string postData,
            string header = null,
            CancellationToken cancellationToken = default)
        {
            var url = NormalizeServiceUrl(baseUrl, service.ControlUrl);
            using var response = await PostSoapDataAsync(
                    url,
                    $"\"{service.ServiceType}#{command}\"",
                    postData,
                    header,
                    cancellationToken)
                .ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await XDocument.LoadAsync(
                stream,
                LoadOptions.PreserveWhitespace,
                cancellationToken).ConfigureAwait(false);
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

        public async Task SubscribeAsync(
            string url,
            string ip,
            int port,
            string localIp,
            int eventport,
            int timeOut = 3600)
        {
            using var options = new HttpRequestMessage(new HttpMethod("SUBSCRIBE"), url);
            options.Headers.UserAgent.ParseAdd(USERAGENT);
            options.Headers.TryAddWithoutValidation("HOST", ip + ":" + port.ToString(_usCulture));
            options.Headers.TryAddWithoutValidation("CALLBACK", "<" + localIp + ":" + eventport.ToString(_usCulture) + ">");
            options.Headers.TryAddWithoutValidation("NT", "upnp:event");
            options.Headers.TryAddWithoutValidation("TIMEOUT", "Second-" + timeOut.ToString(_usCulture));

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .SendAsync(options, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
        }

        public async Task<XDocument> GetDataAsync(string url, CancellationToken cancellationToken)
        {
            using var options = new HttpRequestMessage(HttpMethod.Get, url);
            options.Headers.UserAgent.ParseAdd(USERAGENT);
            options.Headers.TryAddWithoutValidation("FriendlyName.DLNA.ORG", FriendlyName);
            using var response = await _httpClientFactory.CreateClient(NamedClient.Default).SendAsync(options, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await XDocument.LoadAsync(
                    stream,
                    LoadOptions.PreserveWhitespace,
                    cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                return null;
            }
        }

        private async Task<HttpResponseMessage> PostSoapDataAsync(
            string url,
            string soapAction,
            string postData,
            string header,
            CancellationToken cancellationToken)
        {
            if (soapAction[0] != '\"')
            {
                soapAction = $"\"{soapAction}\"";
            }

            using var options = new HttpRequestMessage(HttpMethod.Post, url);
            options.Headers.UserAgent.ParseAdd(USERAGENT);
            options.Headers.TryAddWithoutValidation("SOAPACTION", soapAction);
            options.Headers.TryAddWithoutValidation("Pragma", "no-cache");
            options.Headers.TryAddWithoutValidation("FriendlyName.DLNA.ORG", FriendlyName);

            if (!string.IsNullOrEmpty(header))
            {
                options.Headers.TryAddWithoutValidation("contentFeatures.dlna.org", header);
            }

            options.Content = new StringContent(postData, Encoding.UTF8, MediaTypeNames.Text.Xml);

            return await _httpClientFactory.CreateClient(NamedClient.Default).SendAsync(options, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
    }
}
