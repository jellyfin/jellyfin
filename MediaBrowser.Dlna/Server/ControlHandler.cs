using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MediaBrowser.Dlna.Server
{
    public class ControlHandler
    {
        private readonly ILogger _logger;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_SEC = "http://www.sec.co.kr/";
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private const int systemID = 0;

        public ControlHandler(ILogger logger)
        {
            _logger = logger;
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            var soap = new XmlDocument();
            soap.LoadXml(request.InputXml);
            var sparams = new Headers();
            var body = soap.GetElementsByTagName("Body", NS_SOAPENV).Item(0);

            var method = body.FirstChild;

            foreach (var p in method.ChildNodes)
            {
                var e = p as XmlElement;
                if (e == null)
                {
                    continue;
                }
                sparams.Add(e.LocalName, e.InnerText.Trim());
            }

            var env = new XmlDocument();
            env.AppendChild(env.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            var envelope = env.CreateElement("SOAP-ENV", "Envelope", NS_SOAPENV);
            env.AppendChild(envelope);
            envelope.SetAttribute("encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

            var rbody = env.CreateElement("SOAP-ENV:Body", NS_SOAPENV);
            env.DocumentElement.AppendChild(rbody);

            IEnumerable<KeyValuePair<string, string>> result;
            switch (method.LocalName)
            {
                case "GetSearchCapabilities":
                    result = HandleGetSearchCapabilities();
                    break;
                case "GetSortCapabilities":
                    result = HandleGetSortCapabilities();
                    break;
                case "GetSystemUpdateID":
                    result = HandleGetSystemUpdateID();
                    break;
                case "Browse":
                    result = HandleBrowse(sparams);
                    break;
                case "X_GetFeatureList":
                    result = HandleXGetFeatureList();
                    break;
                case "X_SetBookmark":
                    result = HandleXSetBookmark(sparams);
                    break;
                default:
                    throw new ResourceNotFoundException();
            }

            var response = env.CreateElement(String.Format("u:{0}Response", method.LocalName), method.NamespaceURI);
            rbody.AppendChild(response);

            foreach (var i in result)
            {
                var ri = env.CreateElement(i.Key);
                ri.InnerText = i.Value;
                response.AppendChild(ri);
            }

            var controlResponse = new ControlResponse
            {
                Xml = env.OuterXml
            };

            controlResponse.Headers.Add("EXT", string.Empty);

            return controlResponse;
        }

        private Headers HandleXSetBookmark(Headers sparams)
        {
            var id = sparams["ObjectID"];
            //var item = GetItem(id) as IBookmarkable;
            //if (item != null)
            //{
            //    var newbookmark = long.Parse(sparams["PosSecond"]);
            //    if (newbookmark > 30)
            //    {
            //        newbookmark -= 5;
            //    }
            //    if (newbookmark > 30 || !item.Bookmark.HasValue || item.Bookmark.Value < 60)
            //    {
            //        item.Bookmark = newbookmark;
            //    }
            //}
            return new Headers();
        }

        private Headers HandleGetSearchCapabilities()
        {
            return new Headers { { "SearchCaps", string.Empty } };
        }

        private Headers HandleGetSortCapabilities()
        {
            return new Headers { { "SortCaps", string.Empty } };
        }

        private Headers HandleGetSystemUpdateID()
        {
            return new Headers { { "Id", systemID.ToString() } };
        }

        private Headers HandleXGetFeatureList()
        {
            return new Headers { { "FeatureList", GetFeatureListXml() } };
        }

        private string GetFeatureListXml()
        {
            var builder = new StringBuilder();

            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            builder.Append("<Features xmlns=\"urn:schemas-upnp-org:av:avs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd\">");

            builder.Append("<Feature name=\"samsung.com_BASICVIEW\" version=\"1\">");
            builder.Append("<container id=\"I\" type=\"object.item.imageItem\"/>");
            builder.Append("<container id=\"A\" type=\"object.item.audioItem\"/>");
            builder.Append("<container id=\"V\" type=\"object.item.videoItem\"/>");
            builder.Append("</Feature>");

            builder.Append("</Features>");

            return builder.ToString();
        }

        private IEnumerable<KeyValuePair<string, string>> HandleBrowse(Headers sparams)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];

            int requested;
            var provided = 0;
            int start;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requested) && requested <= 0)
            {
                requested = 20;
            }
            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out start) && start <= 0)
            {
                start = 0;
            }

            //var root = GetItem(id) as IMediaFolder;
            var result = new XmlDocument();

            var didl = result.CreateElement(string.Empty, "DIDL-Lite", NS_DIDL);
            didl.SetAttribute("xmlns:dc", NS_DC);
            didl.SetAttribute("xmlns:dlna", NS_DLNA);
            didl.SetAttribute("xmlns:upnp", NS_UPNP);
            didl.SetAttribute("xmlns:sec", NS_SEC);
            result.AppendChild(didl);

            return null;
        }
    }
}
