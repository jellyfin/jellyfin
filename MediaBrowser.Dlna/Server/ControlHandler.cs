using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;

namespace MediaBrowser.Dlna.Server
{
    public class ControlHandler
    {
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private DeviceProfile _profile;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_SEC = "http://www.sec.co.kr/";
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private int systemID = 0;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public ControlHandler(ILogger logger, IUserManager userManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _userManager = userManager;
            _libraryManager = libraryManager;
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            try
            {
                return ProcessControlRequestInternal(request);
            }
            catch (Exception ex)
            {
                return GetErrorResponse(ex);
            }
        }

        private ControlResponse ProcessControlRequestInternal(ControlRequest request)
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

            _logger.Debug("Received control request {0}", method.Name);

            var user = _userManager.Users.First();

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
                    result = HandleBrowse(sparams, user);
                    break;
                case "X_GetFeatureList":
                    result = HandleXGetFeatureList();
                    break;
                case "X_SetBookmark":
                    result = HandleXSetBookmark(sparams, user);
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

        private ControlResponse GetErrorResponse(Exception ex)
        {
            var env = new XmlDocument();
            env.AppendChild(env.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            var envelope = env.CreateElement("SOAP-ENV", "Envelope", NS_SOAPENV);
            env.AppendChild(envelope);
            envelope.SetAttribute("encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

            var rbody = env.CreateElement("SOAP-ENV:Body", NS_SOAPENV);
            env.DocumentElement.AppendChild(rbody);

            var fault = env.CreateElement("SOAP-ENV", "Fault", NS_SOAPENV);
            var faultCode = env.CreateElement("faultcode");
            faultCode.InnerText = "500";
            fault.AppendChild(faultCode);
            var faultString = env.CreateElement("faultstring");
            faultString.InnerText = ex.ToString();
            fault.AppendChild(faultString);
            var detail = env.CreateDocumentFragment();
            detail.InnerXml = "<detail><UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>401</errorCode><errorDescription>Invalid Action</errorDescription></UPnPError></detail>";
            fault.AppendChild(detail);
            rbody.AppendChild(fault);

            return new ControlResponse
            {
                Xml = env.OuterXml
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXSetBookmark(IDictionary<string, string> sparams, User user)
        {
            var id = sparams["ObjectID"];

            var newbookmark = long.Parse(sparams["PosSecond"]);

            return new Headers();
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSearchCapabilities()
        {
            return new Headers { { "SearchCaps", string.Empty } };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSortCapabilities()
        {
            return new Headers { { "SortCaps", string.Empty } };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSystemUpdateID()
        {
            return new Headers { { "Id", systemID.ToString(_usCulture) } };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXGetFeatureList()
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

        private IEnumerable<KeyValuePair<string, string>> HandleBrowse(Headers sparams, User user)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];

            int requested = 20;
            var provided = 0;
            int start = 0;

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

            var folder = string.IsNullOrWhiteSpace(id)
                ? user.RootFolder
                : (Folder)_libraryManager.GetItemById(new Guid(id));

            var children = folder.GetChildren(user, true).ToList();

            if (string.Equals(flag, "BrowseMetadata"))
            {
                Browse_AddFolder(result, folder, children.Count);
                provided++;
            }
            else
            {
                foreach (var i in children.OfType<Folder>())
                {
                    if (start > 0)
                    {
                        start--;
                        continue;
                    }

                    var childCount = i.GetChildren(user, true).Count();

                    Browse_AddFolder(result, i, childCount);

                    if (++provided == requested)
                    {
                        break;
                    }
                }

                if (provided != requested)
                {
                    foreach (var i in children.Where(i => !i.IsFolder))
                    {
                        if (start > 0)
                        {
                            start--;
                            continue;
                        }

                        Browse_AddItem(result, i, user);

                        if (++provided == requested)
                        {
                            break;
                        }
                    }
                }
            }

            var resXML = result.OuterXml;

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Result", resXML),
                new KeyValuePair<string,string>("NumberReturned", provided.ToString(_usCulture)),
                new KeyValuePair<string,string>("TotalMatches", children.Count.ToString(_usCulture)),
                new KeyValuePair<string,string>("UpdateID", systemID.ToString(_usCulture))
            };
        }

        private void Browse_AddFolder(XmlDocument result, Folder f, int childCount)
        {
            var container = result.CreateElement(string.Empty, "container", NS_DIDL);
            container.SetAttribute("restricted", "0");
            container.SetAttribute("childCount", childCount.ToString(_usCulture));
            container.SetAttribute("id", f.Id.ToString("N"));

            var parent = f.Parent;
            if (parent == null)
            {
                container.SetAttribute("parentID", "0");
            }
            else
            {
                container.SetAttribute("parentID", parent.Id.ToString("N"));
            }

            var title = result.CreateElement("dc", "title", NS_DC);
            title.InnerText = f.Name;
            container.AppendChild(title);

            var date = result.CreateElement("dc", "date", NS_DC);
            date.InnerText = f.DateModified.ToString("o");
            container.AppendChild(date);

            var objectClass = result.CreateElement("upnp", "class", NS_UPNP);
            objectClass.InnerText = "object.container.storageFolder";
            container.AppendChild(objectClass);
            result.DocumentElement.AppendChild(container);
        }

        private void Browse_AddItem(XmlDocument result, BaseItem item, User user)
        {
            var element = result.CreateElement(string.Empty, "item", NS_DIDL);
            element.SetAttribute("restricted", "1");
            element.SetAttribute("id", item.Id.ToString("N"));

            if (item.Parent != null)
            {
                element.SetAttribute("parentID", item.Parent.Id.ToString("N"));
            }

            element.AppendChild(CreateObjectClass(result, item));

            AddBookmarkInfo(item, user, element);

            AddGeneralProperties(item, element);

            AddActors(item, element);

            var title = result.CreateElement("dc", "title", NS_DC);
            title.InnerText = item.Name;
            element.AppendChild(title);

            var res = result.CreateElement(string.Empty, "res", NS_DIDL);

            //res.InnerText = String.Format(
            //  "http://{0}:{1}{2}file/{3}",
            //  request.LocalEndPoint.Address,
            //  request.LocalEndPoint.Port,
            //  prefix,
            //  resource.Id
            //  );

            //if (props.TryGetValue("SizeRaw", out prop))
            //{
            //    res.SetAttribute("size", prop);
            //}
            //if (props.TryGetValue("Resolution", out prop))
            //{
            //    res.SetAttribute("resolution", prop);
            //}
            //if (props.TryGetValue("Duration", out prop))
            //{
            //    res.SetAttribute("duration", prop);
            //}

            //res.SetAttribute("protocolInfo", String.Format(
            //    "http-get:*:{1}:{0};DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS={2}",
            //    resource.PN, DlnaMaps.Mime[resource.Type], DlnaMaps.DefaultStreaming
            //    ));

            element.AppendChild(res);

            AddCover(item, element);

            result.DocumentElement.AppendChild(element);
        }

        private XmlElement CreateObjectClass(XmlDocument result, BaseItem item)
        {
            var objectClass = result.CreateElement("upnp", "class", NS_UPNP);

            if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                objectClass.InnerText = "object.item.audioItem.musicTrack";
            }
            else if (string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                objectClass.InnerText = "object.item.imageItem.photo";
            }
            else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                objectClass.InnerText = "object.item.videoItem.movie";
            }
            else
            {
                throw new NotSupportedException();
            }

            return objectClass;
        }

        private void AddActors(BaseItem item, XmlElement element)
        {
            foreach (var actor in item.People)
            {
                var e = element.OwnerDocument.CreateElement("upnp", "actor", NS_UPNP);
                e.InnerText = actor.Name;
                element.AppendChild(e);
            }
        }

        private void AddBookmarkInfo(BaseItem item, User user, XmlElement element)
        {
            //var bookmark = bookmarkable.Bookmark;
            //if (bookmark.HasValue)
            //{
            //    var dcmInfo = item.OwnerDocument.CreateElement("sec", "dcmInfo", NS_SEC);
            //    dcmInfo.InnerText = string.Format("BM={0}", bookmark.Value);
            //    item.AppendChild(dcmInfo);
            //}
        }

        private  void AddGeneralProperties(BaseItem item, XmlElement element)
        {
            //var prop = string.Empty;
            //if (props.TryGetValue("DateO", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("dc", "date", NS_DC);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
            //if (props.TryGetValue("Genre", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "genre", NS_UPNP);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}

            if (!string.IsNullOrWhiteSpace(item.Overview))
            {
                var e = element.OwnerDocument.CreateElement("dc", "description", NS_DC);
                e.InnerText = item.Overview;
                element.AppendChild(e);
            }

            //if (props.TryGetValue("Artist", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "artist", NS_UPNP);
            //    e.SetAttribute("role", "AlbumArtist");
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
            //if (props.TryGetValue("Performer", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "artist", NS_UPNP);
            //    e.SetAttribute("role", "Performer");
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //    e = item.OwnerDocument.CreateElement("dc", "creator", NS_DC);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
            //if (props.TryGetValue("Album", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "album", NS_UPNP);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
            //if (props.TryGetValue("Track", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "originalTrackNumber", NS_UPNP);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
            //if (props.TryGetValue("Creator", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("dc", "creator", NS_DC);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}

            //if (props.TryGetValue("Director", out prop))
            //{
            //    var e = item.OwnerDocument.CreateElement("upnp", "director", NS_UPNP);
            //    e.InnerText = prop;
            //    item.AppendChild(e);
            //}
        }

        private void AddCover(BaseItem item, XmlElement element)
        {
            //var result = item.OwnerDocument;
            //var cover = resource as IMediaCover;
            //if (cover == null)
            //{
            //    return;
            //}
            //try
            //{
            //    var c = cover.Cover;
            //    var curl = String.Format(
            //      "http://{0}:{1}{2}cover/{3}",
            //      request.LocalEndPoint.Address,
            //      request.LocalEndPoint.Port,
            //      prefix,
            //      resource.Id
            //      );
            //    var icon = result.CreateElement("upnp", "albumArtURI", NS_UPNP);
            //    var profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            //    profile.InnerText = "JPEG_TN";
            //    icon.SetAttributeNode(profile);
            //    icon.InnerText = curl;
            //    item.AppendChild(icon);
            //    icon = result.CreateElement("upnp", "icon", NS_UPNP);
            //    profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            //    profile.InnerText = "JPEG_TN";
            //    icon.SetAttributeNode(profile);
            //    icon.InnerText = curl;
            //    item.AppendChild(icon);

            //    var res = result.CreateElement(string.Empty, "res", NS_DIDL);
            //    res.InnerText = curl;

            //    res.SetAttribute("protocolInfo", string.Format(
            //        "http-get:*:{1}:{0};DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS={2}",
            //        c.PN, DlnaMaps.Mime[c.Type], DlnaMaps.DefaultStreaming
            //        ));
            //    var width = c.MetaWidth;
            //    var height = c.MetaHeight;
            //    if (width.HasValue && height.HasValue)
            //    {
            //        res.SetAttribute("resolution", string.Format("{0}x{1}", width.Value, height.Value));
            //    }
            //    else
            //    {
            //        res.SetAttribute("resolution", "200x200");
            //    }
            //    res.SetAttribute("protocolInfo", string.Format(
            //      "http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=01;DLNA.ORG_CI=1;DLNA.ORG_FLAGS={0}",
            //      DlnaMaps.DefaultInteractive
            //      ));
            //    item.AppendChild(res);
            //}
            //catch (Exception)
            //{
            //    return;
            //}
        }
    }
}
