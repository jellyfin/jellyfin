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
        private const string FriendlyName = "MediaBrowser";

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
            string header = null)
        {
            var serviceUrl = service.ControlUrl;
            if (!serviceUrl.StartsWith("/"))
                serviceUrl = "/" + serviceUrl;

            var response = await PostSoapDataAsync(baseUrl + serviceUrl, "\"" + service.ServiceType + "#" + command + "\"", postData, header)
                .ConfigureAwait(false);

            using (var stream = response.Content)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);
                }
            }
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
                LogRequest = _config.Configuration.DlnaOptions.EnableDebugLogging,
                LogErrorResponseBody = true
            };

            options.RequestHeaders["HOST"] = ip + ":" + port.ToString(_usCulture);
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport.ToString(_usCulture) + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second-" + timeOut.ToString(_usCulture);

            // TODO: Method should be SUBSCRIBE
            // https://github.com/stormboy/node-upnp-controlpoint/blob/master/lib/upnp-service.js#L106
            using (await _httpClient.Get(options).ConfigureAwait(false))
            {
            }
        }

        public async Task RespondAsync(Uri url, 
            string ip, 
            int port, 
            string localIp,
            int eventport,
            int timeOut = 3600)
        {
            var options = new HttpRequestOptions
            {
                Url = url.ToString(),
                UserAgent = USERAGENT
            };

            options.RequestHeaders["HOST"] = ip + ":" + port.ToString(_usCulture);
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport.ToString(_usCulture) + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second-" + timeOut.ToString(_usCulture);

            using (await _httpClient.Get(options).ConfigureAwait(false))
            {
            }
        }

        public async Task<XDocument> GetDataAsync(string url)
        {
            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogRequest = _config.Configuration.DlnaOptions.EnableDebugLogging,
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
            string header = null)
        {
            if (!soapAction.StartsWith("\""))
                soapAction = "\"" + soapAction + "\"";

            var options = new HttpRequestOptions
            {
                Url = url,
                UserAgent = USERAGENT,
                LogRequest = _config.Configuration.DlnaOptions.EnableDebugLogging,
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
