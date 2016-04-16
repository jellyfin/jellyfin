using System.Linq;
using System.Xml.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Dlna.ContentDirectory
{
    public class ContentDirectoryBrowser
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ContentDirectoryBrowser(IHttpClient httpClient, ILogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        private static XNamespace UNamespace = "u";
        
        public async Task<QueryResult<ChannelItemInfo>> Browse(ContentDirectoryBrowseRequest request, CancellationToken cancellationToken)
        {
            var options = new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                UserAgent = "Emby",
                RequestContentType = "text/xml; charset=\"utf-8\"",
                LogErrorResponseBody = true,
                Url = request.ContentDirectoryUrl
            };

            options.RequestHeaders["SOAPACTION"] = "urn:schemas-upnp-org:service:ContentDirectory:1#Browse";

            options.RequestContent = GetRequestBody(request);

            var response = await _httpClient.SendAsync(options, "POST");

            using (var reader = new StreamReader(response.Content))
            {
                var doc = XDocument.Parse(reader.ReadToEnd(), LoadOptions.PreserveWhitespace);

                var queryResult = new QueryResult<ChannelItemInfo>();

                if (doc.Document == null)
                    return queryResult;

                var responseElement = doc.Document.Descendants(UNamespace + "BrowseResponse").ToList();

                var countElement = responseElement.Select(i => i.Element("TotalMatches")).FirstOrDefault(i => i != null);
                var countValue = countElement == null ? null : countElement.Value;

                int count;
                if (!string.IsNullOrWhiteSpace(countValue) && int.TryParse(countValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                {
                    queryResult.TotalRecordCount = count;

                    var resultElement = responseElement.Select(i => i.Element("Result")).FirstOrDefault(i => i != null);
                    var resultString = (string)resultElement;

                    if (resultElement != null)
                    {
                        var xElement = XElement.Parse(resultString);
                    }
                }

                return queryResult;
            }
        }

        private string GetRequestBody(ContentDirectoryBrowseRequest request)
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            builder.Append("<s:Envelope s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\" xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\"><s:Body>");
            builder.Append("<u:Browse xmlns:u=\"urn:schemas-upnp-org:service:ContentDirectory:1\">");

            if (string.IsNullOrWhiteSpace(request.ParentId))
            {
                request.ParentId = "1";
            }

            builder.AppendFormat("<ObjectID>{0}</ObjectID>", SecurityElement.Escape(request.ParentId));
            builder.Append("<BrowseFlag>BrowseDirectChildren</BrowseFlag>");

            //builder.Append("<BrowseFlag>BrowseMetadata</BrowseFlag>");

            builder.Append("<Filter>*</Filter>");

            request.StartIndex = request.StartIndex ?? 0;
            builder.AppendFormat("<StartingIndex>{0}</StartingIndex>", SecurityElement.Escape(request.StartIndex.Value.ToString(CultureInfo.InvariantCulture)));

            request.Limit = request.Limit ?? 20;
            if (request.Limit.HasValue)
            {
                builder.AppendFormat("<RequestedCount>{0}</RequestedCount>", SecurityElement.Escape(request.Limit.Value.ToString(CultureInfo.InvariantCulture)));
            }

            builder.Append("<SortCriteria></SortCriteria>");

            builder.Append("</u:Browse>");
            builder.Append("</s:Body></s:Envelope>");

            return builder.ToString();
        }
    }

    public class ContentDirectoryBrowseRequest
    {
        public int? StartIndex { get; set; }
        public int? Limit { get; set; }
        public string ParentId { get; set; }
        public string ContentDirectoryUrl { get; set; }
    }
}
