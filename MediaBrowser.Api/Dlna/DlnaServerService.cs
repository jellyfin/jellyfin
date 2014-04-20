using MediaBrowser.Controller.Dlna;
using ServiceStack;
using ServiceStack.Text.Controller;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Dlna
{
    [Route("/Dlna/{UuId}/description.xml", "GET", Summary = "Gets dlna server info")]
    [Route("/Dlna/{UuId}/description", "GET", Summary = "Gets dlna server info")]
    public class GetDescriptionXml
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/contentdirectory/contentdirectory.xml", "GET", Summary = "Gets dlna content directory xml")]
    [Route("/Dlna/contentdirectory/contentdirectory", "GET", Summary = "Gets dlna content directory xml")]
    public class GetContentDirectory
    {
    }

    [Route("/Dlna/contentdirectory/{UuId}/control", "POST", Summary = "Processes a control request")]
    public class ProcessControlRequest : IRequiresRequestStream
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        public Stream RequestStream { get; set; }
    }

    [Route("/Dlna/contentdirectory/{UuId}/events", Summary = "Processes an event subscription request")]
    public class ProcessEventRequest
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/icons/{Filename}", "GET", Summary = "Gets a server icon")]
    public class GetIcon
    {
        [ApiMember(Name = "Filename", Description = "The icon filename", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Filename { get; set; }
    }

    public class DlnaServerService : BaseApiService
    {
        private readonly IDlnaManager _dlnaManager;

        public DlnaServerService(IDlnaManager dlnaManager)
        {
            _dlnaManager = dlnaManager;
        }

        public object Get(GetDescriptionXml request)
        {
            var xml = _dlnaManager.GetServerDescriptionXml(GetRequestHeaders(), request.UuId);

            return ResultFactory.GetResult(xml, "text/xml");
        }

        public object Get(GetContentDirectory request)
        {
            var xml = _dlnaManager.GetContentDirectoryXml(GetRequestHeaders());

            return ResultFactory.GetResult(xml, "text/xml");
        }

        public object Post(ProcessControlRequest request)
        {
            var response = PostAsync(request).Result;

            return ResultFactory.GetResult(response.Xml, "text/xml");
        }

        private async Task<ControlResponse> PostAsync(ProcessControlRequest request)
        {
            var pathInfo = PathInfo.Parse(Request.PathInfo);
            var id = pathInfo.GetArgumentValue<string>(2);

            using (var reader = new StreamReader(request.RequestStream))
            {
                return _dlnaManager.ProcessControlRequest(new ControlRequest
                {
                    Headers = GetRequestHeaders(),
                    InputXml = await reader.ReadToEndAsync().ConfigureAwait(false),
                    TargetServerUuId = id
                });
            }
        }

        private IDictionary<string, string> GetRequestHeaders()
        {
            var headers = new Dictionary<string, string>();

            foreach (var key in Request.Headers.AllKeys)
            {
                headers[key] = Request.Headers[key];
            }

            return headers;
        }

        public object Get(GetIcon request)
        {
            using (var response = _dlnaManager.GetIcon(request.Filename))
            {
                using (var ms = new MemoryStream())
                {
                    response.Stream.CopyTo(ms);

                    ms.Position = 0;
                    var bytes = ms.ToArray();
                    return ResultFactory.GetResult(bytes, "image/" + response.Format.ToString().ToLower());
                }
            }
        }

        public object Any(ProcessEventRequest request)
        {
            var subscriptionId = GetHeader("SID");
            var notificationType = GetHeader("NT");
            var callback = GetHeader("CALLBACK");
            var timeoutString = GetHeader("TIMEOUT");

            var timeout = ParseTimeout(timeoutString) ?? 300;

            if (string.Equals(Request.Verb, "SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(notificationType))
                {
                    RenewEvent(subscriptionId, timeout);
                }
                else
                {
                    SubscribeToEvent(notificationType, timeout, callback);
                }

                return GetSubscriptionResponse(request.UuId, timeout);
            }

            UnsubscribeFromEvent(subscriptionId);
            return ResultFactory.GetResult("", "text/plain");
        }

        private void UnsubscribeFromEvent(string subscriptionId)
        {

        }

        private void SubscribeToEvent(string notificationType, int? timeout, string callback)
        {

        }

        private void RenewEvent(string subscriptionId, int? timeout)
        {

        }

        private object GetSubscriptionResponse(string uuid, int timeout)
        {
            var headers = new Dictionary<string, string>();

            headers["SID"] = "uuid:" + uuid;
            headers["TIMEOUT"] = "SECOND-" + timeout.ToString(_usCulture);

            return ResultFactory.GetResult("\r\n", "text/plain", headers);
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        private int? ParseTimeout(string header)
        {
            if (!string.IsNullOrEmpty(header))
            {
                // Starts with SECOND-
                header = header.Split('-').Last();

                int val;

                if (int.TryParse(header, NumberStyles.Any, _usCulture, out val))
                {
                    return val;
                }
            }

            return null;
        }
    }
}
