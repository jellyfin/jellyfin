using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Dlna.ContentDirectory;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Dlna.Didl
{
    public class DidlBuilder
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";

        private readonly DeviceProfile _profile;
        private readonly IImageProcessor _imageProcessor;
        private readonly string _serverAddress;
        private readonly string _accessToken;
        private readonly User _user;
        private readonly IUserDataManager _userDataManager;
        private readonly ILocalizationManager _localization;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaEncoder _mediaEncoder;

        public DidlBuilder(DeviceProfile profile, User user, IImageProcessor imageProcessor, string serverAddress, string accessToken, IUserDataManager userDataManager, ILocalizationManager localization, IMediaSourceManager mediaSourceManager, ILogger logger, ILibraryManager libraryManager, IMediaEncoder mediaEncoder)
        {
            _profile = profile;
            _imageProcessor = imageProcessor;
            _serverAddress = serverAddress;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _logger = logger;
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _accessToken = accessToken;
            _user = user;
        }

        public string GetItemDidl(DlnaOptions options, BaseItem item, BaseItem context, string deviceId, Filter filter, StreamInfo streamInfo)
        {
            var result = new XmlDocument();

            var didl = result.CreateElement(string.Empty, "DIDL-Lite", NS_DIDL);
            didl.SetAttribute("xmlns:dc", NS_DC);
            didl.SetAttribute("xmlns:dlna", NS_DLNA);
            didl.SetAttribute("xmlns:upnp", NS_UPNP);
            //didl.SetAttribute("xmlns:sec", NS_SEC);

            foreach (var att in _profile.XmlRootAttributes)
            {
                didl.SetAttribute(att.Name, att.Value);
            }

            result.AppendChild(didl);

            result.DocumentElement.AppendChild(GetItemElement(options, result, item, context, null, deviceId, filter, streamInfo));

            return result.DocumentElement.OuterXml;
        }

        public XmlElement GetItemElement(DlnaOptions options, XmlDocument doc, BaseItem item, BaseItem context, StubType? contextStubType, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            var clientId = GetClientId(item, null);

            var element = doc.CreateElement(string.Empty, "item", NS_DIDL);
            element.SetAttribute("restricted", "1");
            element.SetAttribute("id", clientId);

            if (context != null)
            {
                element.SetAttribute("parentID", GetClientId(context, contextStubType));
            }
            else
            {
                var parent = item.DisplayParentId;
                if (parent.HasValue)
                {
                    element.SetAttribute("parentID", GetClientId(parent.Value, null));
                }
            }

            //AddBookmarkInfo(item, user, element);

            AddGeneralProperties(item, null, context, element, filter);

            // refID?
            // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

            var hasMediaSources = item as IHasMediaSources;

            if (hasMediaSources != null)
            {
                if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
                {
                    AddAudioResource(options, element, hasMediaSources, deviceId, filter, streamInfo);
                }
                else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    AddVideoResource(options, element, hasMediaSources, deviceId, filter, streamInfo);
                }
            }

            AddCover(item, context, null, element);

            return element;
        }

        private ILogger GetStreamBuilderLogger(DlnaOptions options)
        {
            if (options.EnableDebugLog)
            {
                return _logger;
            }

            return new NullLogger();
        }

        private void AddVideoResource(DlnaOptions options, XmlElement container, IHasMediaSources video, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            if (streamInfo == null)
            {
                var sources = _mediaSourceManager.GetStaticMediaSources(video, true, _user).ToList();

                streamInfo = new StreamBuilder(_mediaEncoder, GetStreamBuilderLogger(options)).BuildVideoItem(new VideoOptions
                {
                    ItemId = GetClientId(video),
                    MediaSources = sources,
                    Profile = _profile,
                    DeviceId = deviceId,
                    MaxBitrate = _profile.MaxStreamingBitrate
                });
            }

            var targetWidth = streamInfo.TargetWidth;
            var targetHeight = streamInfo.TargetHeight;

            var contentFeatureList = new ContentFeatureBuilder(_profile).BuildVideoHeader(streamInfo.Container,
                streamInfo.VideoCodec,
                streamInfo.TargetAudioCodec,
                targetWidth,
                targetHeight,
                streamInfo.TargetVideoBitDepth,
                streamInfo.TargetVideoBitrate,
                streamInfo.TargetTimestamp,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks,
                streamInfo.TargetVideoProfile,
                streamInfo.TargetVideoLevel,
                streamInfo.TargetFramerate,
                streamInfo.TargetPacketLength,
                streamInfo.TranscodeSeekInfo,
                streamInfo.IsTargetAnamorphic,
                streamInfo.TargetRefFrames,
                streamInfo.TargetVideoStreamCount,
                streamInfo.TargetAudioStreamCount,
                streamInfo.TargetVideoCodecTag);

            foreach (var contentFeature in contentFeatureList)
            {
                AddVideoResource(container, video, deviceId, filter, contentFeature, streamInfo);
            }

            foreach (var subtitle in streamInfo.GetSubtitleProfiles(false, _serverAddress, _accessToken))
            {
                if (subtitle.DeliveryMethod == SubtitleDeliveryMethod.External)
                {
                    var subtitleAdded = AddSubtitleElement(container, subtitle);

                    if (subtitleAdded && _profile.EnableSingleSubtitleLimit)
                    {
                        break;
                    }
                }
            }
        }

        private bool AddSubtitleElement(XmlElement container, SubtitleStreamInfo info)
        {
            var subtitleProfile = _profile.SubtitleProfiles
                .FirstOrDefault(i => string.Equals(info.Format, i.Format, StringComparison.OrdinalIgnoreCase) && i.Method == SubtitleDeliveryMethod.External);

            if (subtitleProfile == null)
            {
                return false;
            }

            var subtitleMode = subtitleProfile.DidlMode;

            if (string.Equals(subtitleMode, "CaptionInfoEx", StringComparison.OrdinalIgnoreCase))
            {
                // <sec:CaptionInfoEx sec:type="srt">http://192.168.1.3:9999/video.srt</sec:CaptionInfoEx>
                // <sec:CaptionInfo sec:type="srt">http://192.168.1.3:9999/video.srt</sec:CaptionInfo>

                var res = container.OwnerDocument.CreateElement("CaptionInfoEx", "sec");

                res.InnerText = info.Url;

                //// TODO: attribute needs SEC:
                res.SetAttribute("type", "sec", info.Format.ToLower());
                container.AppendChild(res);
            }
            else if (string.Equals(subtitleMode, "smi", StringComparison.OrdinalIgnoreCase))
            {
                var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

                res.InnerText = info.Url;

                res.SetAttribute("protocolInfo", "http-get:*:smi/caption:*");

                container.AppendChild(res);
            }
            else
            {
                var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

                res.InnerText = info.Url;

                var protocolInfo = string.Format("http-get:*:text/{0}:*", info.Format.ToLower());
                res.SetAttribute("protocolInfo", protocolInfo);

                container.AppendChild(res);
            }

            return true;
        }

        private void AddVideoResource(XmlElement container, IHasMediaSources video, string deviceId, Filter filter, string contentFeatures, StreamInfo streamInfo)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            var url = streamInfo.ToDlnaUrl(_serverAddress, _accessToken);

            res.InnerText = url;

            var mediaSource = streamInfo.MediaSource;

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

            var totalBitrate = streamInfo.TargetTotalBitrate;
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
                streamInfo.TargetAudioCodec,
                streamInfo.VideoCodec,
                streamInfo.TargetAudioBitrate,
                targetWidth,
                targetHeight,
                streamInfo.TargetVideoBitDepth,
                streamInfo.TargetVideoProfile,
                streamInfo.TargetVideoLevel,
                streamInfo.TargetFramerate,
                streamInfo.TargetPacketLength,
                streamInfo.TargetTimestamp,
                streamInfo.IsTargetAnamorphic,
                streamInfo.TargetRefFrames,
                streamInfo.TargetVideoStreamCount,
                streamInfo.TargetAudioStreamCount,
                streamInfo.TargetVideoCodecTag);

            var filename = url.Substring(0, url.IndexOf('?'));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
               ? MimeTypes.GetMimeType(filename)
               : mediaProfile.MimeType;

            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures
                ));

            container.AppendChild(res);
        }

        private string GetDisplayName(BaseItem item, StubType? itemStubType, BaseItem context)
        {
            if (itemStubType.HasValue && itemStubType.Value == StubType.People)
            {
                if (item is Video)
                {
                    return _localization.GetLocalizedString("HeaderCastCrew");
                }
                return _localization.GetLocalizedString("HeaderPeople");
            }

            var episode = item as Episode;
            var season = context as Season;

            if (episode != null && season != null)
            {
                // This is a special embedded within a season
                if (item.ParentIndexNumber.HasValue && item.ParentIndexNumber.Value == 0)
                {
                    if (season.IndexNumber.HasValue && season.IndexNumber.Value != 0)
                    {
                        return string.Format(_localization.GetLocalizedString("ValueSpecialEpisodeName"), item.Name);
                    }
                }

                if (item.IndexNumber.HasValue)
                {
                    var number = item.IndexNumber.Value.ToString("00").ToString(CultureInfo.InvariantCulture);

                    if (episode.IndexNumberEnd.HasValue)
                    {
                        number += "-" + episode.IndexNumberEnd.Value.ToString("00").ToString(CultureInfo.InvariantCulture);
                    }

                    return number + " - " + item.Name;
                }
            }

            return item.Name;
        }

        private void AddAudioResource(DlnaOptions options, XmlElement container, IHasMediaSources audio, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            var res = container.OwnerDocument.CreateElement(string.Empty, "res", NS_DIDL);

            if (streamInfo == null)
            {
                var sources = _mediaSourceManager.GetStaticMediaSources(audio, true, _user).ToList();

                streamInfo = new StreamBuilder(_mediaEncoder, GetStreamBuilderLogger(options)).BuildAudioItem(new AudioOptions
               {
                   ItemId = GetClientId(audio),
                   MediaSources = sources,
                   Profile = _profile,
                   DeviceId = deviceId
               });
            }

            var url = streamInfo.ToDlnaUrl(_serverAddress, _accessToken);

            res.InnerText = url;

            var mediaSource = streamInfo.MediaSource;

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
                streamInfo.TargetAudioCodec,
                targetChannels,
                targetAudioBitrate);

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

        public static bool IsIdRoot(string id)
        {
            if (string.IsNullOrWhiteSpace(id) ||

                string.Equals(id, "0", StringComparison.OrdinalIgnoreCase)

                // Samsung sometimes uses 1 as root
                || string.Equals(id, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public XmlElement GetFolderElement(XmlDocument doc, BaseItem folder, StubType? stubType, BaseItem context, int childCount, Filter filter, string requestedId = null)
        {
            var container = doc.CreateElement(string.Empty, "container", NS_DIDL);
            container.SetAttribute("restricted", "0");
            container.SetAttribute("searchable", "1");
            container.SetAttribute("childCount", childCount.ToString(_usCulture));

            var clientId = GetClientId(folder, stubType);

            if (string.Equals(requestedId, "0"))
            {
                container.SetAttribute("id", "0");
                container.SetAttribute("parentID", "-1");
            }
            else
            {
                container.SetAttribute("id", clientId);

                if (context != null)
                {
                    container.SetAttribute("parentID", GetClientId(context, null));
                }
                else
                {
                    var parent = folder.DisplayParentId;
                    if (!parent.HasValue)
                    {
                        container.SetAttribute("parentID", "0");
                    }
                    else
                    {
                        container.SetAttribute("parentID", GetClientId(parent.Value, null));
                    }
                }
            }

            AddCommonFields(folder, stubType, null, container, filter);

            AddCover(folder, context, stubType, container);

            return container;
        }

        //private void AddBookmarkInfo(BaseItem item, User user, XmlElement element)
        //{
        //    var userdata = _userDataManager.GetUserData(user.Id, item.GetUserDataKey());

        //    if (userdata.PlaybackPositionTicks > 0)
        //    {
        //        var dcmInfo = element.OwnerDocument.CreateElement("sec", "dcmInfo", NS_SEC);
        //        dcmInfo.InnerText = string.Format("BM={0}", Convert.ToInt32(TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds).ToString(_usCulture));
        //        element.AppendChild(dcmInfo);
        //    }
        //}

        /// <summary>
        /// Adds fields used by both items and folders
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="itemStubType">Type of the item stub.</param>
        /// <param name="context">The context.</param>
        /// <param name="element">The element.</param>
        /// <param name="filter">The filter.</param>
        private void AddCommonFields(BaseItem item, StubType? itemStubType, BaseItem context, XmlElement element, Filter filter)
        {
            // Don't filter on dc:title because not all devices will include it in the filter
            // MediaMonkey for example won't display content without a title
            //if (filter.Contains("dc:title"))
            {
                AddValue(element, "dc", "title", GetDisplayName(item, itemStubType, context), NS_DC);
            }

            element.AppendChild(CreateObjectClass(element.OwnerDocument, item, itemStubType));

            if (filter.Contains("dc:date"))
            {
                if (item.PremiereDate.HasValue)
                {
                    AddValue(element, "dc", "date", item.PremiereDate.Value.ToString("o"), NS_DC);
                }
            }

            if (filter.Contains("upnp:genre"))
            {
                foreach (var genre in item.Genres)
                {
                    AddValue(element, "upnp", "genre", genre, NS_UPNP);
                }
            }

            foreach (var studio in item.Studios)
            {
                AddValue(element, "upnp", "publisher", studio, NS_UPNP);
            }

            if (filter.Contains("dc:description"))
            {
                var desc = item.Overview;

                var hasShortOverview = item as IHasShortOverview;
                if (hasShortOverview != null && !string.IsNullOrEmpty(hasShortOverview.ShortOverview))
                {
                    desc = hasShortOverview.ShortOverview;
                }

                if (!string.IsNullOrWhiteSpace(desc))
                {
                    AddValue(element, "dc", "description", desc, NS_DC);
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

        private XmlElement CreateObjectClass(XmlDocument result, BaseItem item, StubType? stubType)
        {
            // More types here
            // http://oss.linn.co.uk/repos/Public/LibUpnpCil/DidlLite/UpnpAv/Test/TestDidlLite.cs

            var objectClass = result.CreateElement("upnp", "class", NS_UPNP);

            if (item.IsFolder || stubType.HasValue)
            {
                string classType = null;

                if (!_profile.RequiresPlainFolders)
                {
                    if (item is MusicAlbum)
                    {
                        classType = "object.container.album.musicAlbum";
                    }
                    else if (item is MusicArtist)
                    {
                        classType = "object.container.person.musicArtist";
                    }
                    else if (item is Series || item is Season || item is BoxSet || item is Video)
                    {
                        classType = "object.container.album.videoAlbum";
                    }
                    else if (item is Playlist)
                    {
                        classType = "object.container.playlistContainer";
                    }
                    else if (item is PhotoAlbum)
                    {
                        classType = "object.container.album.photoAlbum";
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
                else if (!_profile.RequiresPlainVideoItems && item is MusicVideo)
                {
                    objectClass.InnerText = "object.item.videoItem.musicVideoClip";
                }
                else
                {
                    objectClass.InnerText = "object.item.videoItem";
                }
            }
            else if (item is MusicGenre)
            {
                objectClass.InnerText = _profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre.musicGenre";
            }
            else if (item is Genre || item is GameGenre)
            {
                objectClass.InnerText = _profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre";
            }
            else
            {
                objectClass.InnerText = "object.item";
            }

            return objectClass;
        }

        private void AddPeople(BaseItem item, XmlElement element)
        {
            var types = new[] { PersonType.Director, PersonType.Writer, PersonType.Producer, PersonType.Composer, "Creator" };

            var people = _libraryManager.GetPeople(item);

            foreach (var actor in people)
            {
                var type = types.FirstOrDefault(i => string.Equals(i, actor.Type, StringComparison.OrdinalIgnoreCase) || string.Equals(i, actor.Role, StringComparison.OrdinalIgnoreCase))
                    ?? PersonType.Actor;

                AddValue(element, "upnp", type.ToLower(), actor.Name, NS_UPNP);
            }
        }

        private void AddGeneralProperties(BaseItem item, StubType? itemStubType, BaseItem context, XmlElement element, Filter filter)
        {
            AddCommonFields(item, itemStubType, context, element, filter);

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

                foreach (var artist in audio.AlbumArtists)
                {
                    AddAlbumArtist(element, artist);
                }
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                foreach (var artist in album.AlbumArtists)
                {
                    AddAlbumArtist(element, artist);
                    AddValue(element, "upnp", "artist", artist, NS_UPNP);
                }
                foreach (var artist in album.Artists)
                {
                    AddValue(element, "upnp", "artist", artist, NS_UPNP);
                }
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                foreach (var artist in musicVideo.Artists)
                {
                    AddValue(element, "upnp", "artist", artist, NS_UPNP);
                    AddAlbumArtist(element, artist);
                }

                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    AddValue(element, "upnp", "album", musicVideo.Album, NS_UPNP);
                }
            }

            if (item.IndexNumber.HasValue)
            {
                AddValue(element, "upnp", "originalTrackNumber", item.IndexNumber.Value.ToString(_usCulture), NS_UPNP);

                if (item is Episode)
                {
                    AddValue(element, "upnp", "episodeNumber", item.IndexNumber.Value.ToString(_usCulture), NS_UPNP);
                }
            }
        }

        private void AddAlbumArtist(XmlElement elem, string name)
        {
            try
            {
                var newNode = elem.OwnerDocument.CreateElement("upnp", "artist", NS_UPNP);
                newNode.InnerText = name;

                newNode.SetAttribute("role", "AlbumArtist");

                elem.AppendChild(newNode);
            }
            catch (XmlException)
            {
                //_logger.Error("Error adding xml value: " + value);
            }
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

        private void AddCover(BaseItem item, BaseItem context, StubType? stubType, XmlElement element)
        {
            if (stubType.HasValue && stubType.Value == StubType.People)
            {
                AddEmbeddedImageAsCover("people", element);
                return;
            }

            ImageDownloadInfo imageInfo = null;

            if (context is UserView)
            {
                var episode = item as Episode;
                if (episode != null)
                {
                    var parent = episode.Series;
                    if (parent != null)
                    {
                        imageInfo = GetImageInfo(parent);
                    }
                }
            }

            // Finally, just use the image from the item
            if (imageInfo == null)
            {
                imageInfo = GetImageInfo(item);
            }

            if (imageInfo == null)
            {
                return;
            }

            var result = element.OwnerDocument;

            var playbackPercentage = 0;
            var unplayedCount = 0;

            if (item is Video)
            {
                var userData = _userDataManager.GetUserDataDto(item, _user).Result;

                playbackPercentage = Convert.ToInt32(userData.PlayedPercentage ?? 0);
                if (playbackPercentage >= 100 || userData.Played)
                {
                    playbackPercentage = 100;
                }
            }
            else if (item is Series || item is Season || item is BoxSet)
            {
                var userData = _userDataManager.GetUserDataDto(item, _user).Result;

                if (userData.Played)
                {
                    playbackPercentage = 100;
                }
                else
                {
                    unplayedCount = userData.UnplayedItemCount ?? 0;
                }
            }

            var albumartUrlInfo = GetImageUrl(imageInfo, _profile.MaxAlbumArtWidth, _profile.MaxAlbumArtHeight, playbackPercentage, unplayedCount, "jpg");

            var icon = result.CreateElement("upnp", "albumArtURI", NS_UPNP);
            var profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = _profile.AlbumArtPn;
            icon.SetAttributeNode(profile);
            icon.InnerText = albumartUrlInfo.Url;
            element.AppendChild(icon);

            // TOOD: Remove these default values
            var iconUrlInfo = GetImageUrl(imageInfo, _profile.MaxIconWidth ?? 48, _profile.MaxIconHeight ?? 48, playbackPercentage, unplayedCount, "jpg");
            icon = result.CreateElement("upnp", "icon", NS_UPNP);
            icon.InnerText = iconUrlInfo.Url;
            element.AppendChild(icon);

            if (!_profile.EnableAlbumArtInDidl)
            {
                if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    if (!stubType.HasValue)
                    {
                        return;
                    }
                }
            }

            AddImageResElement(item, element, 160, 160, playbackPercentage, unplayedCount, "jpg", "JPEG_TN");

            if (!_profile.EnableSingleAlbumArtLimit)
            {
                AddImageResElement(item, element, 4096, 4096, playbackPercentage, unplayedCount, "jpg", "JPEG_LRG");
                AddImageResElement(item, element, 1024, 768, playbackPercentage, unplayedCount, "jpg", "JPEG_MED");
                AddImageResElement(item, element, 640, 480, playbackPercentage, unplayedCount, "jpg", "JPEG_SM");
                AddImageResElement(item, element, 4096, 4096, playbackPercentage, unplayedCount, "png", "PNG_LRG");
                AddImageResElement(item, element, 160, 160, playbackPercentage, unplayedCount, "png", "PNG_TN");
            }
        }

        private void AddEmbeddedImageAsCover(string name, XmlElement element)
        {
            var result = element.OwnerDocument;

            var icon = result.CreateElement("upnp", "albumArtURI", NS_UPNP);
            var profile = result.CreateAttribute("dlna", "profileID", NS_DLNA);
            profile.InnerText = _profile.AlbumArtPn;
            icon.SetAttributeNode(profile);
            icon.InnerText = _serverAddress + "/Dlna/icons/people480.jpg";
            element.AppendChild(icon);

            icon = result.CreateElement("upnp", "icon", NS_UPNP);
            icon.InnerText = _serverAddress + "/Dlna/icons/people48.jpg";
            element.AppendChild(icon);
        }

        private void AddImageResElement(BaseItem item,
            XmlElement element,
            int maxWidth,
            int maxHeight,
            int playbackPercentage,
            int unplayedCount,
            string format,
            string org_Pn)
        {
            var imageInfo = GetImageInfo(item);

            if (imageInfo == null)
            {
                return;
            }

            var result = element.OwnerDocument;

            var albumartUrlInfo = GetImageUrl(imageInfo, maxWidth, maxHeight, playbackPercentage, unplayedCount, format);

            var res = result.CreateElement(string.Empty, "res", NS_DIDL);

            res.InnerText = albumartUrlInfo.Url;

            var width = albumartUrlInfo.Width;
            var height = albumartUrlInfo.Height;

            var contentFeatures = new ContentFeatureBuilder(_profile)
                .BuildImageHeader(format, width, height, imageInfo.IsDirectStream, org_Pn);

            res.SetAttribute("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                MimeTypes.GetMimeType("file." + format),
                contentFeatures
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
            if (item.HasImage(ImageType.Backdrop))
            {
                if (item is Channel)
                {
                    return GetImageInfo(item, ImageType.Backdrop);
                }
            }

            item = item.GetParents().FirstOrDefault(i => i.HasImage(ImageType.Primary));

            if (item != null)
            {
                if (item.HasImage(ImageType.Primary))
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
                tag = _imageProcessor.GetImageCacheTag(item, type);
            }
            catch
            {

            }

            int? width = null;
            int? height = null;

            try
            {
                var size = _imageProcessor.GetImageSize(imageInfo);

                width = Convert.ToInt32(size.Width);
                height = Convert.ToInt32(size.Height);
            }
            catch
            {

            }

            var inputFormat = (Path.GetExtension(imageInfo.Path) ?? string.Empty)
                .TrimStart('.')
                .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

            return new ImageDownloadInfo
            {
                ItemId = item.Id.ToString("N"),
                Type = type,
                ImageTag = tag,
                Width = width,
                Height = height,
                Format = inputFormat,
                ItemImageInfo = imageInfo
            };
        }

        class ImageDownloadInfo
        {
            internal string ItemId;
            internal string ImageTag;
            internal ImageType Type;

            internal int? Width;
            internal int? Height;

            internal bool IsDirectStream;

            internal string Format;

            internal ItemImageInfo ItemImageInfo;
        }

        class ImageUrlInfo
        {
            internal string Url;

            internal int? Width;
            internal int? Height;
        }

        public static string GetClientId(BaseItem item, StubType? stubType)
        {
            return GetClientId(item.Id, stubType);
        }

        public static string GetClientId(Guid idValue, StubType? stubType)
        {
            var id = idValue.ToString("N");

            if (stubType.HasValue)
            {
                id = stubType.Value.ToString().ToLower() + "_" + id;
            }

            return id;
        }

        public static string GetClientId(IHasMediaSources item)
        {
            var id = item.Id.ToString("N");

            return id;
        }

        private ImageUrlInfo GetImageUrl(ImageDownloadInfo info, int maxWidth, int maxHeight, int playbackPercentage, int unplayedCount, string format)
        {
            var url = string.Format("{0}/Items/{1}/Images/{2}/0/{3}/{4}/{5}/{6}/{7}/{8}",
                _serverAddress,
                info.ItemId,
                info.Type,
                info.ImageTag,
                format,
                maxWidth.ToString(CultureInfo.InvariantCulture),
                maxHeight.ToString(CultureInfo.InvariantCulture),
                playbackPercentage.ToString(CultureInfo.InvariantCulture),
                unplayedCount.ToString(CultureInfo.InvariantCulture)
                );

            var width = info.Width;
            var height = info.Height;

            info.IsDirectStream = false;

            if (width.HasValue && height.HasValue)
            {
                var newSize = DrawingUtils.Resize(new ImageSize
                {
                    Height = height.Value,
                    Width = width.Value

                }, null, null, maxWidth, maxHeight);

                width = Convert.ToInt32(newSize.Width);
                height = Convert.ToInt32(newSize.Height);

                var normalizedFormat = format
                    .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

                if (string.Equals(info.Format, normalizedFormat, StringComparison.OrdinalIgnoreCase))
                {
                    info.IsDirectStream = maxWidth >= width.Value && maxHeight >= height.Value;
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
