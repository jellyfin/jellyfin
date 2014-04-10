using MediaBrowser.Controller.Dlna;
using ServiceStack;
using ServiceStack.Web;
using System.Collections.Generic;
using System.IO;
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

    [Route("/Dlna/{UuId}/contentdirectory.xml", "GET", Summary = "Gets dlna content directory xml")]
    [Route("/Dlna/{UuId}/contentdirectory", "GET", Summary = "Gets the content directory xml")]
    public class GetContentDirectory
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }
    }

    [Route("/Dlna/{UuId}/control", "POST", Summary = "Processes a control request")]
    public class ProcessControlRequest : IRequiresRequestStream
    {
        [ApiMember(Name = "UuId", Description = "Server UuId", IsRequired = false, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string UuId { get; set; }

        public Stream RequestStream { get; set; }
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
            using (var reader = new StreamReader(request.RequestStream))
            {
                return _dlnaManager.ProcessControlRequest(new ControlRequest
                {
                    Headers = GetRequestHeaders(),
                    InputXml = await reader.ReadToEndAsync().ConfigureAwait(false)
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
    }
}
