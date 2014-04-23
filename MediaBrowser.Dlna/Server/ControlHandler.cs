using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
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
        private readonly DeviceProfile _profile;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;
        private readonly User _user;

        private readonly string _serverAddress;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_SEC = "http://www.sec.co.kr/";
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private readonly int _systemUpdateId;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public ControlHandler(ILogger logger, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, IDtoService dtoService, IImageProcessor imageProcessor, IUserDataManager userDataManager, User user, int systemUpdateId)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _profile = profile;
            _serverAddress = serverAddress;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
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

            var deviceId = "fgd";

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
                Browse_AddFolder(result, folder, children.Count, filter);
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

                        Browse_AddFolder(result, f, childCount, filter);
                    }
                    else
                    {
                        Browse_AddItem(result, i, deviceId, filter);
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

                    Browse_AddFolder(result, f, childCount, filter);
                }
                else
                {
                    Browse_AddItem(result, i, deviceId, filter);
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

        private void Browse_AddFolder(XmlDocument result, Folder f, int childCount, Filter filter)
        {
            var container = result.CreateElement(string.Empty, "container", NS_DIDL);
            container.SetAttribute("restricted", "0");
            container.SetAttribute("searchable", "1");
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

            AddCommonFields(f, container, filter);

            AddCover(f, container);

            result.DocumentElement.AppendChild(container);
        }

        private void AddValue(XmlElement elem, string prefix, string name, string value, string namespaceUri)
        {
            try
            {
                var date = elem.OwnerDocument.CreateElement(prefix, name, namespaceUri);
                date.InnerText = value;
                elem.AppendChild(date);
            }
            catch (XmlException)
            {
                //_logger.Error("Error adding xml value: " + value);
            }
        }

        private void Browse_AddItem(XmlDocument result, BaseItem item, string deviceId, Filter filter)
        {
            var element = result.CreateElement(string.Empty, "item", NS_DIDL);
            element.SetAttribute("restricted", "1");
            element.SetAttribute("id", item.Id.ToString("N"));

            if (item.Parent != null)
            {
                element.SetAttribute("parentID", item.Parent.Id.ToString("N"));
            }

            //AddBookmarkInfo(item, user, element);

            AddGeneralProperties(item, element, filter);

            // refID?
            // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

            var audio = item as Audio;
            if (audio != null)
            {
                AddAudioResource(element, audio, deviceId, filter);
            }

            var video = item as Video;
            if (video != null)
            {
                AddVideoResource(element, video, deviceId, filter);
            }

            AddCover(item, element);

            result.DocumentElement.AppendChild(element);
        }

        private void AddVideoResource(XmlElement container, Video video, string deviceId, Filter filter)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            var sources = _dtoService.GetMediaSources(video);

            int? maxBitrateSetting = null;

            var streamInfo = new StreamBuilder().BuildVideoItem(new VideoOptions
            {
                ItemId = video.Id.ToString("N"),
                MediaSources = sources,
                Profile = _profile,
                DeviceId = deviceId,
                MaxBitrate = maxBitrateSetting
            });

            var url = streamInfo.ToDlnaUrl(_serverAddress);
            res.InnerText = url;

            var mediaSource = sources.First(i => string.Equals(i.Id, streamInfo.MediaSourceId));

            if (mediaSource.RunTimeTicks.HasValue)
            {
                res.SetAttribute("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (filter.Contains("res@size"))
            {
                if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
                {
                    var size = streamInfo.TargetSize;

                    if (size.HasValue)
                    {
                        res.SetAttribute("size", size.Value.ToString(_usCulture));
                    }
                }
            }

            var totalBitrate = streamInfo.TotalOutputBitrate;
            var targetSampleRate = streamInfo.TargetAudioSampleRate;
            var targetChannels = streamInfo.TargetAudioChannels;

            var targetWidth = streamInfo.TargetWidth;
            var targetHeight = streamInfo.TargetHeight;

            if (targetChannels.HasValue)
            {
                res.SetAttribute("nrAudioChannels", targetChannels.Value.ToString(_usCulture));
            }

            if (filter.Contains("res@resolution"))
            {
                if (targetWidth.HasValue && targetHeight.HasValue)
                {
                    res.SetAttribute("resolution", string.Format("{0}x{1}", targetWidth.Value, targetHeight.Value));
                }
            }

            if (targetSampleRate.HasValue)
            {
                res.SetAttribute("sampleFrequency", targetSampleRate.Value.ToString(_usCulture));
            }

            if (totalBitrate.HasValue)
            {
                res.SetAttribute("bitrate", totalBitrate.Value.ToString(_usCulture));
            }

            var mediaProfile = _profile.GetVideoMediaProfile(streamInfo.Container,
                streamInfo.AudioCodec,
                streamInfo.VideoCodec);

            var filename = url.Substring(0, url.IndexOf('?'));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
               ? MimeTypes.GetMimeType(filename)
               : mediaProfile.MimeType;

            var contentFeatures = new ContentFeatureBuilder(_profile).BuildVideoHeader(streamInfo.Container,
                streamInfo.VideoCodec,
                streamInfo.AudioCodec,
                targetWidth,
                targetHeight,
                totalBitrate,
                streamInfo.TargetTimestamp,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks,
                streamInfo.TranscodeSeekInfo);
            
            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures
                ));

            container.AppendChild(res);
        }

        private void AddAudioResource(XmlElement container, Audio audio, string deviceId, Filter filter)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            var sources = _dtoService.GetMediaSources(audio);

            var streamInfo = new StreamBuilder().BuildAudioItem(new AudioOptions
            {
                ItemId = audio.Id.ToString("N"),
                MediaSources = sources,
                Profile = _profile,
                DeviceId = deviceId
            });

            var url = streamInfo.ToDlnaUrl(_serverAddress);
            res.InnerText = url;

            var mediaSource = sources.First(i => string.Equals(i.Id, streamInfo.MediaSourceId));

            if (mediaSource.RunTimeTicks.HasValue)
            {
                res.SetAttribute("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (filter.Contains("res@size"))
            {
                if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
                {
                    var size = streamInfo.TargetSize;

                    if (size.HasValue)
                    {
                        res.SetAttribute("size", size.Value.ToString(_usCulture));
                    }
                }
            }

            var targetAudioBitrate = streamInfo.TargetAudioBitrate;
            var targetSampleRate = streamInfo.TargetAudioSampleRate;
            var targetChannels = streamInfo.TargetAudioChannels;

            if (targetChannels.HasValue)
            {
                res.SetAttribute("nrAudioChannels", targetChannels.Value.ToString(_usCulture));
            }

            if (targetSampleRate.HasValue)
            {
                res.SetAttribute("sampleFrequency", targetSampleRate.Value.ToString(_usCulture));
            }

            if (targetAudioBitrate.HasValue)
            {
                res.SetAttribute("bitrate", targetAudioBitrate.Value.ToString(_usCulture));
            }

            var mediaProfile = _profile.GetAudioMediaProfile(streamInfo.Container,
                streamInfo.AudioCodec);

            var filename = url.Substring(0, url.IndexOf('?'));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
                ? MimeTypes.GetMimeType(filename)
                : mediaProfile.MimeType;

            var contentFeatures = new ContentFeatureBuilder(_profile).BuildAudioHeader(streamInfo.Container,
                streamInfo.TargetAudioCodec,
                targetAudioBitrate,
                targetSampleRate,
                targetChannels,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks,
                streamInfo.TranscodeSeekInfo);
            
            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures
                ));

            container.AppendChild(res);
        }

        private XmlElement CreateObjectClass(XmlDocument result, BaseItem item)
        {
            var objectClass = result.CreateElement("upnp", "class", NS_UPNP);

            if (item.IsFolder)
            {
                string classType = null;

                if (!_profile.RequiresPlainFolders)
                {
                    if (item is MusicAlbum)
                    {
                        classType = "object.container.album.musicAlbum";
                    }
                    if (item is MusicArtist)
                    {
                        classType = "object.container.person.musicArtist";
                    }
                }

                objectClass.InnerText = classType ?? "object.container.storageFolder";
            }
            else if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                objectClass.InnerText = "object.item.audioItem.musicTrack";
            }
            else if (string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                objectClass.InnerText = "object.item.imageItem.photo";
            }
            else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                if (!_profile.RequiresPlainVideoItems && item is Movie)
                {
                    objectClass.InnerText = "object.item.videoItem.movie";
                }
                else
                {
                    objectClass.InnerText = "object.item.videoItem";
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            return objectClass;
        }

        private void AddPeople(BaseItem item, XmlElement element)
        {
            foreach (var actor in item.People)
            {
                AddValue(element, "upnp", (actor.Type ?? PersonType.Actor).ToLower(), actor.Name, NS_UPNP);
            }
        }

        private void AddBookmarkInfo(BaseItem item, User user, XmlElement element)
        {
            var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

            if (userdata.PlaybackPositionTicks > 0)
            {
                var dcmInfo = element.OwnerDocument.CreateElement("sec", "dcmInfo", NS_SEC);
                dcmInfo.InnerText = string.Format("BM={0}", Convert.ToInt32(TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds).ToString(_usCulture));
                element.AppendChild(dcmInfo);
            }
        }

        /// <summary>
        /// Adds fields used by both items and folders
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="element">The element.</param>
        /// <param name="filter">The filter.</param>
        private void AddCommonFields(BaseItem item, XmlElement element, Filter filter)
        {
            if (filter.Contains("dc:title"))
            {
                AddValue(element, "dc", "title", item.Name, NS_DC);
            }

            element.AppendChild(CreateObjectClass(element.OwnerDocument, item));

            if (filter.Contains("dc:date"))
            {
                if (item.PremiereDate.HasValue)
                {
                    AddValue(element, "dc", "date", item.PremiereDate.Value.ToString("o"), NS_DC);
                }
            }

            foreach (var genre in item.Genres)
            {
                AddValue(element, "upnp", "genre", genre, NS_UPNP);
            }

            foreach (var studio in item.Studios)
            {
                AddValue(element, "upnp", "publisher", studio, NS_UPNP);
            }

            if (filter.Contains("dc:description"))
            {
                if (!string.IsNullOrWhiteSpace(item.Overview))
                {
                    AddValue(element, "dc", "description", item.Overview, NS_DC);
                }
            }
            if (filter.Contains("upnp:longDescription"))
            {
                if (!string.IsNullOrWhiteSpace(item.Overview))
                {
                    AddValue(element, "upnp", "longDescription", item.Overview, NS_UPNP);
                }
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                if (filter.Contains("dc:rating"))
                {
                    AddValue(element, "dc", "rating", item.OfficialRating, NS_DC);
                }
                if (filter.Contains("upnp:rating"))
                {
                    AddValue(element, "upnp", "rating", item.OfficialRating, NS_UPNP);
                }
            }

            AddPeople(item, element);
        }

        private void AddGeneralProperties(BaseItem item, XmlElement element, Filter filter)
        {
            AddCommonFields(item, element, filter);

            var audio = item as Audio;

            if (audio != null)
            {
                foreach (var artist in audio.Artists)
                {
                    AddValue(element, "upnp", "artist", artist, NS_UPNP);
                }

                if (!string.IsNullOrEmpty(audio.Album))
                {
                    AddValue(element, "upnp", "album", audio.Album, NS_UPNP);
                }

                if (!string.IsNullOrEmpty(audio.AlbumArtist))
                {
                    AddValue(element, "upnp", "albumArtist", audio.AlbumArtist, NS_UPNP);
                }
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                if (!string.IsNullOrEmpty(album.AlbumArtist))
                {
                    AddValue(element, "upnp", "artist", album.AlbumArtist, NS_UPNP);
                    AddValue(element, "upnp", "albumArtist", album.AlbumArtist, NS_UPNP);
                }
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                if (!string.IsNullOrEmpty(musicVideo.Artist))
                {
                    AddValue(element, "upnp", "artist", musicVideo.Artist, NS_UPNP);
                }

                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    AddValue(element, "upnp", "album", musicVideo.Album, NS_UPNP);
                }
            }

            if (item.IndexNumber.HasValue)
            {
                AddValue(element, "upnp", "originalTrackNumber", item.IndexNumber.Value.ToString(_usCulture), NS_UPNP);
            }
        }

        private void AddCover(BaseItem item, XmlElement element)
        {
            var imageInfo = GetImageInfo(item);

            if (imageInfo == null)
            {
                return;
            }

            var result = element.OwnerDocument;

            var albumartUrlInfo = GetImageUrl(imageInfo, _profile.MaxAlbumArtWidth, _profile.MaxAlbumArtHeight);

            var icon = result.CreateElement("upnp", "albumArtURI", NS_UPNP);
            var profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = _profile.AlbumArtPn;
            icon.SetAttributeNode(profile);
            icon.InnerText = albumartUrlInfo.Url;
            element.AppendChild(icon);

            var iconUrlInfo = GetImageUrl(imageInfo, _profile.MaxIconWidth, _profile.MaxIconHeight);
            icon = result.CreateElement("upnp", "icon", NS_UPNP);
            profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = _profile.AlbumArtPn;
            icon.SetAttributeNode(profile);
            icon.InnerText = iconUrlInfo.Url;
            element.AppendChild(icon);

            if (!_profile.EnableAlbumArtInDidl)
            {
                return;
            }

            var res = result.CreateElement(string.Empty, "res", NS_DIDL);

            res.InnerText = albumartUrlInfo.Url;

            var width = albumartUrlInfo.Width;
            var height = albumartUrlInfo.Height;

            var mediaProfile = new MediaFormatProfileResolver().ResolveImageFormat("jpg", width, height);

            var orgPn = mediaProfile.HasValue ? "DLNA.ORG_PN=:" + mediaProfile.Value + ";" : string.Empty;

            res.SetAttribute("protocolInfo", string.Format(
                "http-get:*:{1}:{0}DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS={2}",
                orgPn,
                "image/jpeg",
                DlnaMaps.DefaultStreaming
                ));

            if (width.HasValue && height.HasValue)
            {
                res.SetAttribute("resolution", string.Format("{0}x{1}", width.Value, height.Value));
            }

            element.AppendChild(res);
        }

        private ImageDownloadInfo GetImageInfo(BaseItem item)
        {
            if (item.HasImage(ImageType.Primary))
            {
                return GetImageInfo(item, ImageType.Primary);
            }
            if (item.HasImage(ImageType.Thumb))
            {
                return GetImageInfo(item, ImageType.Thumb);
            }

            if (item is Audio || item is Episode)
            {
                item = item.Parents.FirstOrDefault(i => i.HasImage(ImageType.Primary));

                if (item != null)
                {
                    return GetImageInfo(item, ImageType.Primary);
                }
            }

            return null;
        }

        private ImageDownloadInfo GetImageInfo(BaseItem item, ImageType type)
        {
            var imageInfo = item.GetImageInfo(type, 0);
            string tag = null;

            try
            {
                var guid = _imageProcessor.GetImageCacheTag(item, ImageType.Primary);

                tag = guid.HasValue ? guid.Value.ToString("N") : null;
            }
            catch
            {

            }

            int? width = null;
            int? height = null;

            try
            {
                var size = _imageProcessor.GetImageSize(imageInfo.Path, imageInfo.DateModified);

                width = Convert.ToInt32(size.Width);
                height = Convert.ToInt32(size.Height);
            }
            catch
            {

            }

            return new ImageDownloadInfo
            {
                ItemId = item.Id.ToString("N"),
                Type = ImageType.Primary,
                ImageTag = tag,
                Width = width,
                Height = height
            };
        }

        class ImageDownloadInfo
        {
            internal string ItemId;
            internal string ImageTag;
            internal ImageType Type;

            internal int? Width;
            internal int? Height;
        }

        class ImageUrlInfo
        {
            internal string Url;

            internal int? Width;
            internal int? Height;
        }

        private ImageUrlInfo GetImageUrl(ImageDownloadInfo info, int? maxWidth, int? maxHeight)
        {
            var url = string.Format("{0}/Items/{1}/Images/{2}?tag={3}&format=jpg",
                _serverAddress,
                info.ItemId,
                info.Type,
                info.ImageTag);

            if (maxWidth.HasValue)
            {
                url += "&maxWidth=" + maxWidth.Value.ToString(_usCulture);
            }

            if (maxHeight.HasValue)
            {
                url += "&maxHeight=" + maxHeight.Value.ToString(_usCulture);
            }

            var width = info.Width;
            var height = info.Height;

            if (width.HasValue && height.HasValue)
            {
                if (maxWidth.HasValue || maxHeight.HasValue)
                {
                    var newSize = DrawingUtils.Resize(new ImageSize
                    {
                        Height = height.Value,
                        Width = width.Value

                    }, maxWidth: maxWidth, maxHeight: maxHeight);

                    width = Convert.ToInt32(newSize.Width);
                    height = Convert.ToInt32(newSize.Height);
                }
            }

            return new ImageUrlInfo
            {
                Url = url,
                Width = width,
                Height = height
            };
        }
    }
}
