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
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly DeviceProfile _profile;
        private readonly IDtoService _dtoService;
        private readonly IImageProcessor _imageProcessor;
        private readonly IUserDataManager _userDataManager;

        private readonly string _serverAddress;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_SEC = "http://www.sec.co.kr/";
        private const string NS_SOAPENV = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private int systemID = 0;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public ControlHandler(ILogger logger, IUserManager userManager, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, IDtoService dtoService, IImageProcessor imageProcessor, IUserDataManager userDataManager)
        {
            _logger = logger;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _profile = profile;
            _serverAddress = serverAddress;
            _dtoService = dtoService;
            _imageProcessor = imageProcessor;
            _userDataManager = userDataManager;
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

            var item = _libraryManager.GetItemById(new Guid(id));

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

            var provided = 0;
            int requested = 0;
            int start = 0;

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
            didl.SetAttribute("xmlns:sec", NS_SEC);
            result.AppendChild(didl);

            var folder = string.IsNullOrWhiteSpace(id) || string.Equals(id, "0", StringComparison.OrdinalIgnoreCase)
                ? user.RootFolder
                : (Folder)_libraryManager.GetItemById(new Guid(id));

            var children = GetChildrenSorted(folder, user).ToList();

            if (string.Equals(flag, "BrowseMetadata"))
            {
                Browse_AddFolder(result, folder, children.Count);
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
                        var childCount = GetChildrenSorted(f, user).Count();

                        Browse_AddFolder(result, f, childCount);
                    }
                    else
                    {
                        Browse_AddItem(result, i, user);
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

        private IEnumerable<BaseItem> GetChildrenSorted(Folder folder, User user)
        {
            var children = folder.GetChildren(user, true).Where(i => i.LocationType != LocationType.Virtual);

            if (folder is Series || folder is Season || folder is BoxSet)
            {
                return children;
            }

            return _libraryManager.Sort(children, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
        }

        private void Browse_AddFolder(XmlDocument result, Folder f, int childCount)
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

            AddCommonFields(f, container);

            AddCover(f, container);

            container.AppendChild(CreateObjectClass(result, f));
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

            // refID?
            // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

            var audio = item as Audio;
            if (audio != null)
            {
                AddAudioResource(element, audio);
            }

            var video = item as Video;
            if (video != null)
            {
                AddVideoResource(element, video);
            }

            AddCover(item, element);

            result.DocumentElement.AppendChild(element);
        }

        private string GetDeviceId()
        {
            return "erer";
        }

        private void AddVideoResource(XmlElement container, Video video)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            var sources = _dtoService.GetMediaSources(video);

            int? maxBitrateSetting = null;

            var streamInfo = new StreamBuilder().BuildVideoItem(new VideoOptions
            {
                ItemId = video.Id.ToString("N"),
                MediaSources = sources,
                Profile = _profile,
                DeviceId = GetDeviceId(),
                MaxBitrate = maxBitrateSetting
            });

            var url = streamInfo.ToDlnaUrl(_serverAddress);
            res.InnerText = url;

            var mediaSource = sources.First(i => string.Equals(i.Id, streamInfo.MediaSourceId));

            if (mediaSource.RunTimeTicks.HasValue)
            {
                res.SetAttribute("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (streamInfo.IsDirectStream && mediaSource.Size.HasValue)
            {
                res.SetAttribute("size", mediaSource.Size.Value.ToString(_usCulture));
            }

            var videoStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Video && !string.Equals(i.Codec, "mjpeg", StringComparison.OrdinalIgnoreCase));
            var audioStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            var targetAudioBitrate = streamInfo.AudioBitrate ?? (audioStream == null ? null : audioStream.BitRate);
            var targetSampleRate = audioStream == null ? null : audioStream.SampleRate;
            var targetChannels = streamInfo.MaxAudioChannels ?? (audioStream == null ? null : audioStream.Channels);

            var targetWidth = streamInfo.MaxWidth ?? (videoStream == null ? null : videoStream.Width);
            var targetHeight = streamInfo.MaxHeight ?? (videoStream == null ? null : videoStream.Height);

            var targetVideoCodec = streamInfo.IsDirectStream
                ? (videoStream == null ? null : videoStream.Codec)
                : streamInfo.VideoCodec;

            var targetAudioCodec = streamInfo.IsDirectStream
             ? (audioStream == null ? null : audioStream.Codec)
             : streamInfo.AudioCodec;

            var targetBitrate = maxBitrateSetting ?? mediaSource.Bitrate;

            if (targetChannels.HasValue)
            {
                res.SetAttribute("nrAudioChannels", targetChannels.Value.ToString(_usCulture));
            }

            if (targetWidth.HasValue && targetHeight.HasValue)
            {
                res.SetAttribute("resolution", string.Format("{0}x{1}", targetWidth.Value, targetHeight.Value));
            }
            
            if (targetSampleRate.HasValue)
            {
                res.SetAttribute("sampleFrequency", targetSampleRate.Value.ToString(_usCulture));
            }

            if (targetAudioBitrate.HasValue)
            {
                res.SetAttribute("bitrate", targetAudioBitrate.Value.ToString(_usCulture));
            }

            var formatProfile = new MediaFormatProfileResolver().ResolveVideoFormat(streamInfo.Container,
                targetVideoCodec,
                targetAudioCodec,
                targetWidth,
                targetHeight,
                targetBitrate,
                TransportStreamTimestamp.NONE);

            var filename = url.Substring(0, url.IndexOf('?'));

            var orgOpValue = DlnaMaps.GetOrgOpValue(mediaSource.RunTimeTicks.HasValue, streamInfo.IsDirectStream, streamInfo.TranscodeSeekInfo);

            var orgCi = streamInfo.IsDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:DLNA.ORG_PN={1};DLNA.ORG_OP={2};DLNA.ORG_CI={3};DLNA.ORG_FLAGS={4}",
                MimeTypes.GetMimeType(filename),
                formatProfile,
                orgOpValue,
                orgCi,
                DlnaMaps.DefaultStreaming
                ));

            container.AppendChild(res);
        }

        private void AddAudioResource(XmlElement container, Audio audio)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            var sources = _dtoService.GetMediaSources(audio);

            var streamInfo = new StreamBuilder().BuildAudioItem(new AudioOptions
            {
                ItemId = audio.Id.ToString("N"),
                MediaSources = sources,
                Profile = _profile,
                DeviceId = GetDeviceId()
            });

            var url = streamInfo.ToDlnaUrl(_serverAddress);
            res.InnerText = url;

            var mediaSource = sources.First(i => string.Equals(i.Id, streamInfo.MediaSourceId));

            if (mediaSource.RunTimeTicks.HasValue)
            {
                res.SetAttribute("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (streamInfo.IsDirectStream && mediaSource.Size.HasValue)
            {
                res.SetAttribute("size", mediaSource.Size.Value.ToString(_usCulture));
            }

            var audioStream = mediaSource.MediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);

            var targetAudioBitrate = streamInfo.AudioBitrate ?? (audioStream == null ? null : audioStream.BitRate);
            var targetSampleRate = audioStream == null ? null : audioStream.SampleRate;
            var targetChannels = streamInfo.MaxAudioChannels ?? (audioStream == null ? null : audioStream.Channels);

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

            var formatProfile = new MediaFormatProfileResolver().ResolveAudioFormat(streamInfo.Container, targetAudioBitrate, targetSampleRate, targetChannels);

            var filename = url.Substring(0, url.IndexOf('?'));

            var orgOpValue = DlnaMaps.GetOrgOpValue(mediaSource.RunTimeTicks.HasValue, streamInfo.IsDirectStream, streamInfo.TranscodeSeekInfo);

            var orgCi = streamInfo.IsDirectStream ? ";DLNA.ORG_CI=0" : ";DLNA.ORG_CI=1";

            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:DLNA.ORG_PN={1};DLNA.ORG_OP={2};DLNA.ORG_CI={3};DLNA.ORG_FLAGS={4}",
                MimeTypes.GetMimeType(filename),
                formatProfile,
                orgOpValue,
                orgCi,
                DlnaMaps.DefaultStreaming
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
                        classType = "object.container.musicAlbum";
                    }
                    if (item is MusicArtist)
                    {
                        classType = "object.container.musicArtist";
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
        /// <param name="item"></param>
        /// <param name="element"></param>
        private void AddCommonFields(BaseItem item, XmlElement element)
        {
            if (item.PremiereDate.HasValue)
            {
                AddValue(element, "dc", "date", item.PremiereDate.Value.ToString("o"), NS_DC);
            }

            if (item.Genres.Count > 0)
            {
                AddValue(element, "upnp", "genre", item.Genres[0], NS_UPNP);
            }

            if (item.Studios.Count > 0)
            {
                AddValue(element, "upnp", "publisher", item.Studios[0], NS_UPNP);
            }

            AddValue(element, "dc", "title", item.Name, NS_DC);

            if (!string.IsNullOrWhiteSpace(item.Overview))
            {
                AddValue(element, "dc", "description", item.Overview, NS_DC);
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                AddValue(element, "dc", "rating", item.OfficialRating, NS_DC);
            }

            AddPeople(item, element);
        }

        private void AddGeneralProperties(BaseItem item, XmlElement element)
        {
            AddCommonFields(item, element);

            var audio = item as Audio;

            if (audio != null)
            {
                if (audio.Artists.Count > 0)
                {
                    AddValue(element, "upnp", "artist", audio.Artists[0], NS_UPNP);
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

            var curl = GetImageUrl(imageInfo);

            var icon = result.CreateElement("upnp", "albumArtURI", NS_UPNP);
            var profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = "JPEG_TN";
            icon.SetAttributeNode(profile);
            icon.InnerText = curl;
            element.AppendChild(icon);

            icon = result.CreateElement("upnp", "icon", NS_UPNP);
            profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = "JPEG_TN";
            icon.SetAttributeNode(profile);
            icon.InnerText = curl;
            element.AppendChild(icon);

            if (!_profile.EnableAlbumArtInDidl)
            {
                return;
            }

            var res = result.CreateElement(string.Empty, "res", NS_DIDL);
            res.InnerText = curl;

            int? width = imageInfo.Width;
            int? height = imageInfo.Height;

            var mediaProfile = new MediaFormatProfileResolver().ResolveImageFormat("jpg", width, height);

            res.SetAttribute("protocolInfo", string.Format(
                "http-get:*:{1}DLNA.ORG_PN=:{0};DLNA.ORG_OP=01;DLNA.ORG_CI=0;DLNA.ORG_FLAGS={2}",
                mediaProfile, "image/jpeg", DlnaMaps.DefaultStreaming
                ));

            if (width.HasValue && height.HasValue)
            {
                res.SetAttribute("resolution", string.Format("{0}x{1}", width.Value, height.Value));
            }
            else
            {
                // TODO: Devices need to see something here?
                res.SetAttribute("resolution", "200x200");
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

        private string GetImageUrl(ImageDownloadInfo info)
        {
            return string.Format("{0}/Items/{1}/Images/{2}?tag={3}&format=jpg",
                _serverAddress,
                info.ItemId,
                info.Type,
                info.ImageTag);
        }
    }
}
