using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace MediaBrowser.Dlna.Server
{
    public class ControlHandler
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly User _user;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private readonly int _systemUpdateId;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly DidlBuilder _didlBuilder;

        public ControlHandler(ILogger logger, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, IDtoService dtoService, IImageProcessor imageProcessor, IUserDataManager userDataManager, User user, int systemUpdateId)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;

            _didlBuilder = new DidlBuilder(profile, imageProcessor, serverAddress, dtoService);
        }

        public ControlResponse ProcessControlRequest(ControlRequest request)
        {
            try
            {
                return ProcessControlRequestInternal(request);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error processing control request", ex);

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

            var deviceId = "test";

            IEnumerable<KeyValuePair<string, string>> result;

            _logger.Debug("Received control request {0}", method.Name);

            var user = _user;

            if (string.Equals(method.LocalName, "GetSearchCapabilities", StringComparison.OrdinalIgnoreCase))
                result = HandleGetSearchCapabilities();
            else if (string.Equals(method.LocalName, "GetSortCapabilities", StringComparison.OrdinalIgnoreCase))
                result = HandleGetSortCapabilities();
            else if (string.Equals(method.LocalName, "GetSystemUpdateID", StringComparison.OrdinalIgnoreCase))
                result = HandleGetSystemUpdateID();
            else if (string.Equals(method.LocalName, "Browse", StringComparison.OrdinalIgnoreCase))
                result = HandleBrowse(sparams, user, deviceId);
            else if (string.Equals(method.LocalName, "X_GetFeatureList", StringComparison.OrdinalIgnoreCase))
                result = HandleXGetFeatureList();
            else if (string.Equals(method.LocalName, "X_SetBookmark", StringComparison.OrdinalIgnoreCase))
                result = HandleXSetBookmark(sparams, user);
            else if (string.Equals(method.LocalName, "Search", StringComparison.OrdinalIgnoreCase))
                result = HandleSearch(sparams, user, deviceId);
            else
                throw new ResourceNotFoundException("Unexpected control request name: " + method.LocalName);

            var env = new XmlDocument();
            env.AppendChild(env.CreateXmlDeclaration("1.0", "utf-8", "yes"));
            var envelope = env.CreateElement("SOAP-ENV", "Envelope", NS_SOAPENV);
            env.AppendChild(envelope);
            envelope.SetAttribute("encodingStyle", NS_SOAPENV, "http://schemas.xmlsoap.org/soap/encoding/");

            var rbody = env.CreateElement("SOAP-ENV:Body", NS_SOAPENV);
            env.DocumentElement.AppendChild(rbody);

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
                Xml = env.OuterXml,
                IsSuccessful = true
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
                Xml = env.OuterXml,
                IsSuccessful = false
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXSetBookmark(IDictionary<string, string> sparams, User user)
        {
            var id = sparams["ObjectID"];

            var item = GetItemFromObjectId(id, user);

            var newbookmark = int.Parse(sparams["PosSecond"], _usCulture);

            var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

            userdata.PlaybackPositionTicks = TimeSpan.FromSeconds(newbookmark).Ticks;

            _userDataManager.SaveUserData(user.Id, item, userdata, UserDataSaveReason.TogglePlayed,
                CancellationToken.None);

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
            return new Headers { { "Id", _systemUpdateId.ToString(_usCulture) } };
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

        private IEnumerable<KeyValuePair<string, string>> HandleBrowse(Headers sparams, User user, string deviceId)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", ""));

            var provided = 0;
            var requested = 0;
            var start = 0;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requested) && requested <= 0)
            {
                requested = 0;
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
            //didl.SetAttribute("xmlns:sec", NS_SEC);
            result.AppendChild(didl);

            var folder = (Folder)GetItemFromObjectId(id, user);

            var children = GetChildrenSorted(folder, user, sortCriteria).ToList();

            var totalCount = children.Count;

            if (string.Equals(flag, "BrowseMetadata"))
            {
                result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, folder, children.Count, filter));
                provided++;
            }
            else
            {
                if (start > 0)
                {
                    children = children.Skip(start).ToList();
                }
                if (requested > 0)
                {
                    children = children.Take(requested).ToList();
                }

                provided = children.Count;

                foreach (var i in children)
                {
                    if (i.IsFolder)
                    {
                        var f = (Folder)i;
                        var childCount = GetChildrenSorted(f, user, sortCriteria).Count();

                        result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, f, childCount, filter));
                    }
                    else
                    {
                        result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(result, i, deviceId, filter));
                    }
                }
            }

            var resXML = result.OuterXml;

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Result", resXML),
                new KeyValuePair<string,string>("NumberReturned", provided.ToString(_usCulture)),
                new KeyValuePair<string,string>("TotalMatches", totalCount.ToString(_usCulture)),
                new KeyValuePair<string,string>("UpdateID", _systemUpdateId.ToString(_usCulture))
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleSearch(Headers sparams, User user, string deviceId)
        {
            var searchCriteria = new SearchCriteria(sparams.GetValueOrDefault("SearchCriteria", ""));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", ""));
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));

            // sort example: dc:title, dc:date

            var provided = 0;
            var requested = 0;
            var start = 0;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requested) && requested <= 0)
            {
                requested = 0;
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
            //didl.SetAttribute("xmlns:sec", NS_SEC);
            result.AppendChild(didl);

            var folder = (Folder)GetItemFromObjectId(sparams["ContainerID"], user);

            var children = GetChildrenSorted(folder, user, searchCriteria, sortCriteria).ToList();

            var totalCount = children.Count;

            if (start > 0)
            {
                children = children.Skip(start).ToList();
            }
            if (requested > 0)
            {
                children = children.Take(requested).ToList();
            }

            provided = children.Count;

            foreach (var i in children)
            {
                if (i.IsFolder)
                {
                    var f = (Folder)i;
                    var childCount = GetChildrenSorted(f, user, searchCriteria, sortCriteria).Count();

                    result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, f, childCount, filter));
                }
                else
                {
                    result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(result, i, deviceId, filter));
                }
            }

            var resXML = result.OuterXml;

            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("Result", resXML),
                new KeyValuePair<string,string>("NumberReturned", provided.ToString(_usCulture)),
                new KeyValuePair<string,string>("TotalMatches", totalCount.ToString(_usCulture)),
                new KeyValuePair<string,string>("UpdateID", _systemUpdateId.ToString(_usCulture))
            };
        }

        private IEnumerable<BaseItem> GetChildrenSorted(Folder folder, User user, SearchCriteria search, SortCriteria sort)
        {
            if (search.SearchType == SearchType.Unknown)
            {
                return GetChildrenSorted(folder, user, sort);
            }

            var items = folder.GetRecursiveChildren(user);
            items = FilterUnsupportedContent(items);

            if (search.SearchType == SearchType.Audio)
            {
                items = items.OfType<Audio>();
            }
            else if (search.SearchType == SearchType.Video)
            {
                items = items.OfType<Video>();
            }
            else if (search.SearchType == SearchType.Image)
            {
                items = items.OfType<Photo>();
            }
            else if (search.SearchType == SearchType.Playlist)
            {
            }

            return SortItems(items, user, sort);
        }

        private IEnumerable<BaseItem> GetChildrenSorted(Folder folder, User user, SortCriteria sort)
        {
            var items = folder.GetChildren(user, true);

            items = FilterUnsupportedContent(items);

            if (folder is Series || folder is Season || folder is BoxSet)
            {
                return items;
            }

            return SortItems(items, user, sort);
        }

        private IEnumerable<BaseItem> SortItems(IEnumerable<BaseItem> items, User user, SortCriteria sort)
        {
            return _libraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
        }

        private IEnumerable<BaseItem> FilterUnsupportedContent(IEnumerable<BaseItem> items)
        {
            return items.Where(i =>
            {
                // Unplayable
                // TODO: Display and prevent playback with restricted flag?
                if (i.LocationType == LocationType.Virtual)
                {
                    return false;
                }

                // Unplayable
                // TODO: Display and prevent playback with restricted flag?
                var supportsPlaceHolder = i as ISupportsPlaceHolders;
                if (supportsPlaceHolder != null && supportsPlaceHolder.IsPlaceHolder)
                {
                    return false;
                }

                // Upnp renderers won't understand these
                // TODO: Display and prevent playback with restricted flag?
                if (i is Game || i is Book)
                {
                    return false;
                }

                return true;
            });
        }

        private BaseItem GetItemFromObjectId(string id, User user)
        {
            return string.IsNullOrWhiteSpace(id) || string.Equals(id, "0", StringComparison.OrdinalIgnoreCase)

                 // Samsung sometimes uses 1 as root
                 || string.Equals(id, "1", StringComparison.OrdinalIgnoreCase)

                 ? user.RootFolder
                 : _libraryManager.GetItemById(new Guid(id));
        }
    }
}
