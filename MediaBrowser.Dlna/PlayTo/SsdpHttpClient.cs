using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Dlna.Common;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class SsdpHttpClient
    {
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";
        private const string FriendlyName = "Emby";

        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _config;

        public SsdpHttpClient(IHttpClient httpClient, IServerConfigurationManager config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<XDocument> SendCommandAsync(string baseUrl, 
            DeviceService service, 
            string command, 
            string postData, 
            bool logRequest = true,
            string header = null)
        {
            var response = await PostSoapDataAsync(NormalizeServiceUrl(baseUrl, service.ControlUrl), "\"" + service.ServiceType + "#" + command + "\"", postData, header, logRequest)
                .ConfigureAwait(false);

            using (var stream = response.Content)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);
                }
            }
        }

        private string NormalizeServiceUrl(string baseUrl, string serviceUrl)
        {
            // If it's already a complete url, don't stick anything onto the front of it
            if (serviceUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return serviceUrl;
            }

            if (!serviceUrl.StartsWith("/"))
                serviceUrl = "/" + serviceUrl;

            return baseUrl + serviceUrl;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        
        public async Task SubscribeAsync(string url, 
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
                LogErrorResponseBody = true
            };

            options.RequestHeaders["HOST"] = ip + ":" + port.ToString(_usCulture);
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport.ToString(_usCulture) + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second-" + timeOut.ToString(_usCulture);

            await _httpClient.SendAsync(options, "SUBSCRIBE").ConfigureAwait(false);
        }

        public async Task<XDocument> GetDataAsync(string url)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogErrorResponseBody = true
            };

            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);
                }
            }
        }

        private Task<HttpResponseInfo> PostSoapDataAsync(string url, 
            string soapAction, 
            string postData, 
            string header,
            bool logRequest)
        {
            if (!soapAction.StartsWith("\""))
                soapAction = "\"" + soapAction + "\"";

            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogRequest = logRequest || _config.GetDlnaConfiguration().EnableDebugLog,
                LogErrorResponseBody = true
            };

            options.RequestHeaders["SOAPAction"] = soapAction;
            options.RequestHeaders["Pragma"] = "no-cache";
            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;

            if (!string.IsNullOrWhiteSpace(header))
            {
                options.RequestHeaders["contentFeatures.dlna.org"] = header;
            }

            options.RequestContentType = "text/xml; charset=\"utf-8\"";
            options.RequestContent = postData;

            return _httpClient.Post(options);
        }
    }
}
