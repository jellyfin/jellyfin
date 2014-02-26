using MediaBrowser.Common.Net;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MediaBrowser.Dlna.PlayTo
{
    public class SsdpHttpClient
    {
        private const string USERAGENT = "Microsoft-Windows/6.2 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50";
        private const string FriendlyName = "MediaBrowser";

        private static readonly CookieContainer Container = new CookieContainer();

        private readonly IHttpClient _httpClient;

        public SsdpHttpClient(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<XDocument> SendCommandAsync(string baseUrl, uService service, string command, string postData, string header = null)
        {
            var serviceUrl = service.ControlURL;
            if (!serviceUrl.StartsWith("/"))
                serviceUrl = "/" + serviceUrl;

            var response = await PostSoapDataAsync(new Uri(baseUrl + serviceUrl), "\"" + service.ServiceType + "#" + command + "\"", postData, header)
                .ConfigureAwait(false);

            using (var stream = response.Content)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);
                }
            }
        }

        public async Task SubscribeAsync(Uri url, string ip, int port, string localIp, int eventport, int timeOut = 3600)
        {
            var options = new HttpRequestOptions
            {
                Url = url.ToString()
            };

            options.RequestHeaders["UserAgent"] = USERAGENT;
            options.RequestHeaders["HOST"] = ip + ":" + port;
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second - " + timeOut;
            //request.CookieContainer = Container;

            using (await _httpClient.Get(options).ConfigureAwait(false))
            {
            }
        }

        public async Task RespondAsync(Uri url, string ip, int port, string localIp, int eventport, int timeOut = 20000)
        {
            var options = new HttpRequestOptions
            {
                Url = url.ToString()
            };

            options.RequestHeaders["UserAgent"] = USERAGENT;
            options.RequestHeaders["HOST"] = ip + ":" + port;
            options.RequestHeaders["CALLBACK"] = "<" + localIp + ":" + eventport + ">";
            options.RequestHeaders["NT"] = "upnp:event";
            options.RequestHeaders["TIMEOUT"] = "Second - 3600";
            //request.CookieContainer = Container;

            using (await _httpClient.Get(options).ConfigureAwait(false))
            {
            }
        }

        public async Task<XDocument> GetDataAsync(Uri url)
        {
            var options = new HttpRequestOptions
            {
                Url = url.ToString()
            };

            options.RequestHeaders["UserAgent"] = USERAGENT;
            options.RequestHeaders["FriendlyName.DLNA.ORG"] = FriendlyName;
            //request.CookieContainer = Container;

            using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);
                }
            }
        }

        public Task<HttpResponseInfo> PostSoapDataAsync(Uri url, string soapAction, string postData, string header = null, int timeOut = 20000)
        {
            if (!soapAction.StartsWith("\""))
                soapAction = "\"" + soapAction + "\"";

            var options = new HttpRequestOptions
            {
                Url = url.ToString()
            };

            options.RequestHeaders["SOAPAction"] = soapAction;
            options.RequestHeaders["Pragma"] = "no-cache";
            options.RequestHeaders["UserAgent"] = USERAGENT;
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
