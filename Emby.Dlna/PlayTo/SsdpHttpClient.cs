#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
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

        private readonly IHttpClient _httpClient;

        public SsdpHttpClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;
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
            using (var response = await PostSoapDataAsync(
                url,
                $"\"{service.ServiceType}#{command}\"",
                postData,
                header,
                cancellationToken)
                .ConfigureAwait(false))
            using (var stream = response.Content)
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return XDocument.Parse(
                    await reader.ReadToEndAsync().ConfigureAwait(false),
                    LoadOptions.PreserveWhitespace);
            }
        }

        private static string NormalizeServiceUrl(string baseUrl, string serviceUrl)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (serviceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return serviceUrl;
            }

            if (!serviceUrl.StartsWith("/", StringComparison.Ordinal))
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
            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true,
                BufferContent = false,
            };

            options.RequestHeaders["HOST"] = ip + ":" + port.ToString(_usCulture);
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport.ToString(_usCulture) + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second-" + timeOut.ToString(_usCulture);

            using (await _httpClient.SendAsync(options, new HttpMethod("SUBSCRIBE")).ConfigureAwait(false))
            {

            }
        }

        public async Task<XDocument> GetDataAsync(string url, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true,
                BufferContent = false,

                CancellationToken = cancellationToken
            };

            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            using (var response = await _httpClient.SendAsync(options, HttpMethod.Get).ConfigureAwait(false))
            using (var stream = response.Content)
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return XDocument.Parse(
                    await reader.ReadToEndAsync().ConfigureAwait(false),
                    LoadOptions.PreserveWhitespace);
            }
        }

        private Task<HttpResponseInfo> PostSoapDataAsync(
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

            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true,
                BufferContent = false,

                CancellationToken = cancellationToken
            };

            options.RequestHeaders["SOAPAction"] = soapAction;
            options.RequestHeaders["Pragma"] = "no-cache";
            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            if (!string.IsNullOrEmpty(header))
            {
                options.RequestHeaders["contentFeatures.dlna.org"] = header;
            }

            options.RequestContentType = "text/xml";
            options.RequestContent = postData;

            return _httpClient.Post(options);
        }
    }
}
