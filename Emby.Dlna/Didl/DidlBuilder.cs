using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Emby.Dlna.ContentDirectory;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Globalization;

namespace Emby.Dlna.Didl
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

        public string GetItemDidl(DlnaOptions options, BaseItem item, User user, BaseItem context, string deviceId, Filter filter, StreamInfo streamInfo)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8);

            using (XmlWriter writer = XmlWriter.Create(builder, settings))
            {
                //writer.WriteStartDocument();

                writer.WriteStartElement(string.Empty, "DIDL-Lite", NS_DIDL);

                writer.WriteAttributeString("xmlns", "dc", null, NS_DC);
                writer.WriteAttributeString("xmlns", "dlna", null, NS_DLNA);
                writer.WriteAttributeString("xmlns", "upnp", null, NS_UPNP);
                //didl.SetAttribute("xmlns:sec", NS_SEC);

                WriteXmlRootAttributes(_profile, writer);

                WriteItemElement(options, writer, item, user, context, null, deviceId, filter, streamInfo);

                writer.WriteFullEndElement();
                //writer.WriteEndDocument();
            }

            return builder.ToString();
        }

        public static void WriteXmlRootAttributes(DeviceProfile profile, XmlWriter writer)
        {
            foreach (var att in profile.XmlRootAttributes)
            {
                var parts = att.Name.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    writer.WriteAttributeString(parts[0], parts[1], null, att.Value);
                }
                else
                {
                    writer.WriteAttributeString(att.Name, att.Value);
                }
            }
        }

        public void WriteItemElement(DlnaOptions options,
            XmlWriter writer,
            BaseItem item,
            User user,
            BaseItem context,
            StubType? contextStubType,
            string deviceId,
            Filter filter,
            StreamInfo streamInfo = null)
        {
            var clientId = GetClientId(item, null);

            writer.WriteStartElement(string.Empty, "item", NS_DIDL);

            writer.WriteAttributeString("restricted", "1");
            writer.WriteAttributeString("id", clientId);

            if (context != null)
            {
                writer.WriteAttributeString("parentID", GetClientId(context, contextStubType));
            }
            else
            {
                var parent = item.DisplayParentId;
                if (parent.HasValue)
                {
                    writer.WriteAttributeString("parentID", GetClientId(parent.Value, null));
                }
            }

            AddGeneralProperties(item, null, context, writer, filter);

            AddSamsungBookmarkInfo(item, user, writer);

            // refID?
            // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

            var hasMediaSources = item as IHasMediaSources;

            if (hasMediaSources != null)
            {
                if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
                {
                    AddAudioResource(options, writer, hasMediaSources, deviceId, filter, streamInfo);
                }
                else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    AddVideoResource(options, writer, hasMediaSources, deviceId, filter, streamInfo);
                }
            }

            AddCover(item, context, null, writer);
            writer.WriteFullEndElement();
        }

        private ILogger GetStreamBuilderLogger(DlnaOptions options)
        {
            if (options.EnableDebugLog)
            {
                return _logger;
            }

            return new NullLogger();
        }

        private string GetMimeType(string input)
        {
            var mime = MimeTypes.GetMimeType(input);

            if (string.Equals(mime, "video/mp2t", StringComparison.OrdinalIgnoreCase))
            {
                mime = "video/mpeg";
            }

            return mime;
        }

        private void AddVideoResource(DlnaOptions options, XmlWriter writer, IHasMediaSources video, string deviceId, Filter filter, StreamInfo streamInfo = null)
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
                streamInfo.TargetVideoCodec,
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
                streamInfo.TargetVideoCodecTag,
                streamInfo.IsTargetAVC);

            foreach (var contentFeature in contentFeatureList)
            {
                AddVideoResource(writer, video, deviceId, filter, contentFeature, streamInfo);
            }

            var subtitleProfiles = streamInfo.GetSubtitleProfiles(false, _serverAddress, _accessToken)
                .Where(subtitle => subtitle.DeliveryMethod == SubtitleDeliveryMethod.External)
                .ToList();

            foreach (var subtitle in subtitleProfiles)
            {
                var subtitleAdded = AddSubtitleElement(writer, subtitle);

                if (subtitleAdded && _profile.EnableSingleSubtitleLimit)
                {
                    break;
                }
            }
        }

        private bool AddSubtitleElement(XmlWriter writer, SubtitleStreamInfo info)
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

                writer.WriteStartElement("sec", "CaptionInfoEx", null);
                writer.WriteAttributeString("sec", "type", null, info.Format.ToLower());

                writer.WriteString(info.Url);
                writer.WriteFullEndElement();
            }
            else if (string.Equals(subtitleMode, "smi", StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteStartElement(string.Empty, "res", NS_DIDL);

                writer.WriteAttributeString("protocolInfo", "http-get:*:smi/caption:*");

                writer.WriteString(info.Url);
                writer.WriteFullEndElement();
            }
            else
            {
                writer.WriteStartElement(string.Empty, "res", NS_DIDL);
                var protocolInfo = string.Format("http-get:*:text/{0}:*", info.Format.ToLower());
                writer.WriteAttributeString("protocolInfo", protocolInfo);

                writer.WriteString(info.Url);
                writer.WriteFullEndElement();
            }

            return true;
        }

        private void AddVideoResource(XmlWriter writer, IHasMediaSources video, string deviceId, Filter filter, string contentFeatures, StreamInfo streamInfo)
        {
            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

            var url = streamInfo.ToDlnaUrl(_serverAddress, _accessToken);

            var mediaSource = streamInfo.MediaSource;

            if (mediaSource.RunTimeTicks.HasValue)
            {
                writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (filter.Contains("res@size"))
            {
                if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
                {
                    var size = streamInfo.TargetSize;

                    if (size.HasValue)
                    {
                        writer.WriteAttributeString("size", size.Value.ToString(_usCulture));
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
                writer.WriteAttributeString("nrAudioChannels", targetChannels.Value.ToString(_usCulture));
            }

            if (filter.Contains("res@resolution"))
            {
                if (targetWidth.HasValue && targetHeight.HasValue)
                {
                    writer.WriteAttributeString("resolution", string.Format("{0}x{1}", targetWidth.Value, targetHeight.Value));
                }
            }

            if (targetSampleRate.HasValue)
            {
                writer.WriteAttributeString("sampleFrequency", targetSampleRate.Value.ToString(_usCulture));
            }

            if (totalBitrate.HasValue)
            {
                writer.WriteAttributeString("bitrate", totalBitrate.Value.ToString(_usCulture));
            }

            var mediaProfile = _profile.GetVideoMediaProfile(streamInfo.Container,
                streamInfo.TargetAudioCodec,
                streamInfo.TargetVideoCodec,
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
                streamInfo.TargetVideoCodecTag,
                streamInfo.IsTargetAVC);

            var filename = url.Substring(0, url.IndexOf('?'));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
               ? GetMimeType(filename)
               : mediaProfile.MimeType;

            writer.WriteAttributeString("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures
                ));

            writer.WriteString(url);

            writer.WriteFullEndElement();
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
                    var number = item.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

                    if (episode.IndexNumberEnd.HasValue)
                    {
                        number += "-" + episode.IndexNumberEnd.Value.ToString("00", CultureInfo.InvariantCulture);
                    }

                    return number + " - " + item.Name;
                }
            }

            return item.Name;
        }

        private void AddAudioResource(DlnaOptions options, XmlWriter writer, IHasMediaSources audio, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

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

            var mediaSource = streamInfo.MediaSource;

            if (mediaSource.RunTimeTicks.HasValue)
            {
                writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", _usCulture));
            }

            if (filter.Contains("res@size"))
            {
                if (streamInfo.IsDirectStream || streamInfo.EstimateContentLength)
                {
                    var size = streamInfo.TargetSize;

                    if (size.HasValue)
                    {
                        writer.WriteAttributeString("size", size.Value.ToString(_usCulture));
                    }
                }
            }

            var targetAudioBitrate = streamInfo.TargetAudioBitrate;
            var targetSampleRate = streamInfo.TargetAudioSampleRate;
            var targetChannels = streamInfo.TargetAudioChannels;

            if (targetChannels.HasValue)
            {
                writer.WriteAttributeString("nrAudioChannels", targetChannels.Value.ToString(_usCulture));
            }

            if (targetSampleRate.HasValue)
            {
                writer.WriteAttributeString("sampleFrequency", targetSampleRate.Value.ToString(_usCulture));
            }

            if (targetAudioBitrate.HasValue)
            {
                writer.WriteAttributeString("bitrate", targetAudioBitrate.Value.ToString(_usCulture));
            }

            var mediaProfile = _profile.GetAudioMediaProfile(streamInfo.Container,
                streamInfo.TargetAudioCodec,
                targetChannels,
                targetAudioBitrate,
                targetSampleRate);

            var filename = url.Substring(0, url.IndexOf('?'));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
                ? GetMimeType(filename)
                : mediaProfile.MimeType;

            var contentFeatures = new ContentFeatureBuilder(_profile).BuildAudioHeader(streamInfo.Container,
                streamInfo.TargetAudioCodec,
                targetAudioBitrate,
                targetSampleRate,
                targetChannels,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks,
                streamInfo.TranscodeSeekInfo);

            writer.WriteAttributeString("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                mimeType,
                contentFeatures
                ));

            writer.WriteString(url);

            writer.WriteFullEndElement();
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

        public void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem context, int childCount, Filter filter, string requestedId = null)
        {
            writer.WriteStartElement(string.Empty, "container", NS_DIDL);

            writer.WriteAttributeString("restricted", "0");
            writer.WriteAttributeString("searchable", "1");
            writer.WriteAttributeString("childCount", childCount.ToString(_usCulture));

            var clientId = GetClientId(folder, stubType);

            if (string.Equals(requestedId, "0"))
            {
                writer.WriteAttributeString("id", "0");
                writer.WriteAttributeString("parentID", "-1");
            }
            else
            {
                writer.WriteAttributeString("id", clientId);

                if (context != null)
                {
                    writer.WriteAttributeString("parentID", GetClientId(context, null));
                }
                else
                {
                    var parent = folder.DisplayParentId;
                    if (!parent.HasValue)
                    {
                        writer.WriteAttributeString("parentID", "0");
                    }
                    else
                    {
                        writer.WriteAttributeString("parentID", GetClientId(parent.Value, null));
                    }
                }
            }

            AddGeneralProperties(folder, stubType, context, writer, filter);

            AddCover(folder, context, stubType, writer);

            writer.WriteFullEndElement();
        }

        private void AddSamsungBookmarkInfo(BaseItem item, User user, XmlWriter writer)
        {
            if (!item.SupportsPositionTicksResume || item is Folder)
            {
                return;
            }

            XmlAttribute secAttribute = null;
            foreach (var attribute in _profile.XmlRootAttributes)
            {
                if (string.Equals(attribute.Name, "xmlns:sec", StringComparison.OrdinalIgnoreCase))
                {
                    secAttribute = attribute;
                    break;
                }
            }

            // Not a samsung device
            if (secAttribute == null)
            {
                return;
            }

            var userdata = _userDataManager.GetUserData(user.Id, item);

            if (userdata.PlaybackPositionTicks > 0)
            {
                var elementValue = string.Format("BM={0}", Convert.ToInt32(TimeSpan.FromTicks(userdata.PlaybackPositionTicks).TotalSeconds).ToString(_usCulture));
                AddValue(writer, "sec", "dcmInfo", elementValue, secAttribute.Value);
            }
        }

        /// <summary>
        /// Adds fields used by both items and folders
        /// </summary>
        private void AddCommonFields(BaseItem item, StubType? itemStubType, BaseItem context, XmlWriter writer, Filter filter)
        {
            // Don't filter on dc:title because not all devices will include it in the filter
            // MediaMonkey for example won't display content without a title
            //if (filter.Contains("dc:title"))
            {
                AddValue(writer, "dc", "title", GetDisplayName(item, itemStubType, context), NS_DC);
            }

            WriteObjectClass(writer, item, itemStubType);

            if (filter.Contains("dc:date"))
            {
                if (item.PremiereDate.HasValue)
                {
                    AddValue(writer, "dc", "date", item.PremiereDate.Value.ToString("o"), NS_DC);
                }
            }

            if (filter.Contains("upnp:genre"))
            {
                foreach (var genre in item.Genres)
                {
                    AddValue(writer, "upnp", "genre", genre, NS_UPNP);
                }
            }

            foreach (var studio in item.Studios)
            {
                AddValue(writer, "upnp", "publisher", studio, NS_UPNP);
            }

            if (filter.Contains("dc:description"))
            {
                var desc = item.Overview;

                if (!string.IsNullOrWhiteSpace(desc))
                {
                    AddValue(writer, "dc", "description", desc, NS_DC);
                }
            }
            if (filter.Contains("upnp:longDescription"))
            {
                if (!string.IsNullOrWhiteSpace(item.Overview))
                {
                    AddValue(writer, "upnp", "longDescription", item.Overview, NS_UPNP);
                }
            }

            if (!string.IsNullOrEmpty(item.OfficialRating))
            {
                if (filter.Contains("dc:rating"))
                {
                    AddValue(writer, "dc", "rating", item.OfficialRating, NS_DC);
                }
                if (filter.Contains("upnp:rating"))
                {
                    AddValue(writer, "upnp", "rating", item.OfficialRating, NS_UPNP);
                }
            }

            AddPeople(item, writer);
        }

        private void WriteObjectClass(XmlWriter writer, BaseItem item, StubType? stubType)
        {
            // More types here
            // http://oss.linn.co.uk/repos/Public/LibUpnpCil/DidlLite/UpnpAv/Test/TestDidlLite.cs

            writer.WriteStartElement("upnp", "class", NS_UPNP);

            if (item.IsDisplayedAsFolder || stubType.HasValue)
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

                writer.WriteString(classType ?? "object.container.storageFolder");
            }
            else if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteString("object.item.audioItem.musicTrack");
            }
            else if (string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                writer.WriteString("object.item.imageItem.photo");
            }
            else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                if (!_profile.RequiresPlainVideoItems && item is Movie)
                {
                    writer.WriteString("object.item.videoItem.movie");
                }
                else if (!_profile.RequiresPlainVideoItems && item is MusicVideo)
                {
                    writer.WriteString("object.item.videoItem.musicVideoClip");
                }
                else
                {
                    writer.WriteString("object.item.videoItem");
                }
            }
            else if (item is MusicGenre)
            {
                writer.WriteString(_profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre.musicGenre");
            }
            else if (item is Genre || item is GameGenre)
            {
                writer.WriteString(_profile.RequiresPlainFolders ? "object.container.storageFolder" : "object.container.genre");
            }
            else
            {
                writer.WriteString("object.item");
            }

            writer.WriteFullEndElement();
        }

        private void AddPeople(BaseItem item, XmlWriter writer)
        {
            var types = new[]
            {
                PersonType.Director,
                PersonType.Writer,
                PersonType.Producer,
                PersonType.Composer,
                "Creator"
            };

            var people = _libraryManager.GetPeople(item);

            var index = 0;

            // Seeing some LG models locking up due content with large lists of people
            // The actual issue might just be due to processing a more metadata than it can handle
            var limit = 6;

            foreach (var actor in people)
            {
                var type = types.FirstOrDefault(i => string.Equals(i, actor.Type, StringComparison.OrdinalIgnoreCase) || string.Equals(i, actor.Role, StringComparison.OrdinalIgnoreCase))
                    ?? PersonType.Actor;

                AddValue(writer, "upnp", type.ToLower(), actor.Name, NS_UPNP);

                index++;

                if (index >= limit)
                {
                    break;
                }
            }
        }

        private void AddGeneralProperties(BaseItem item, StubType? itemStubType, BaseItem context, XmlWriter writer, Filter filter)
        {
            AddCommonFields(item, itemStubType, context, writer, filter);

            var audio = item as Audio;

            if (audio != null)
            {
                foreach (var artist in audio.Artists)
                {
                    AddValue(writer, "upnp", "artist", artist, NS_UPNP);
                }

                if (!string.IsNullOrEmpty(audio.Album))
                {
                    AddValue(writer, "upnp", "album", audio.Album, NS_UPNP);
                }

                foreach (var artist in audio.AlbumArtists)
                {
                    AddAlbumArtist(writer, artist);
                }
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                foreach (var artist in album.AlbumArtists)
                {
                    AddAlbumArtist(writer, artist);
                    AddValue(writer, "upnp", "artist", artist, NS_UPNP);
                }
                foreach (var artist in album.Artists)
                {
                    AddValue(writer, "upnp", "artist", artist, NS_UPNP);
                }
            }

            var musicVideo = item as MusicVideo;

            if (musicVideo != null)
            {
                foreach (var artist in musicVideo.Artists)
                {
                    AddValue(writer, "upnp", "artist", artist, NS_UPNP);
                    AddAlbumArtist(writer, artist);
                }

                if (!string.IsNullOrEmpty(musicVideo.Album))
                {
                    AddValue(writer, "upnp", "album", musicVideo.Album, NS_UPNP);
                }
            }

            if (item.IndexNumber.HasValue)
            {
                AddValue(writer, "upnp", "originalTrackNumber", item.IndexNumber.Value.ToString(_usCulture), NS_UPNP);

                if (item is Episode)
                {
                    AddValue(writer, "upnp", "episodeNumber", item.IndexNumber.Value.ToString(_usCulture), NS_UPNP);
                }
            }
        }

        private void AddAlbumArtist(XmlWriter writer, string name)
        {
            try
            {
                writer.WriteStartElement("upnp", "artist", NS_UPNP);
                writer.WriteAttributeString("role", "AlbumArtist");

                writer.WriteString(name);

                writer.WriteFullEndElement();
            }
            catch (XmlException)
            {
                //_logger.Error("Error adding xml value: " + value);
            }
        }

        private void AddValue(XmlWriter writer, string prefix, string name, string value, string namespaceUri)
        {
            try
            {
                writer.WriteElementString(prefix, name, namespaceUri, value);
            }
            catch (XmlException)
            {
                //_logger.Error("Error adding xml value: " + value);
            }
        }

        private void AddCover(BaseItem item, BaseItem context, StubType? stubType, XmlWriter writer)
        {
            if (stubType.HasValue && stubType.Value == StubType.People)
            {
                AddEmbeddedImageAsCover("people", writer);
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

            var playbackPercentage = 0;
            var unplayedCount = 0;

            if (item is Video)
            {
                var userData = _userDataManager.GetUserDataDto(item, _user);

                playbackPercentage = Convert.ToInt32(userData.PlayedPercentage ?? 0);
                if (playbackPercentage >= 100 || userData.Played)
                {
                    playbackPercentage = 100;
                }
            }
            else if (item is Series || item is Season || item is BoxSet)
            {
                var userData = _userDataManager.GetUserDataDto(item, _user);

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

            writer.WriteStartElement("upnp", "albumArtURI", NS_UPNP);
            writer.WriteAttributeString("dlna", "profileID", NS_DLNA, _profile.AlbumArtPn);
            writer.WriteString(albumartUrlInfo.Url);
            writer.WriteFullEndElement();

            // TOOD: Remove these default values
            var iconUrlInfo = GetImageUrl(imageInfo, _profile.MaxIconWidth ?? 48, _profile.MaxIconHeight ?? 48, playbackPercentage, unplayedCount, "jpg");
            writer.WriteElementString("upnp", "icon", NS_UPNP, iconUrlInfo.Url);

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

            AddImageResElement(item, writer, 160, 160, playbackPercentage, unplayedCount, "jpg", "JPEG_TN");

            if (!_profile.EnableSingleAlbumArtLimit)
            {
                AddImageResElement(item, writer, 4096, 4096, playbackPercentage, unplayedCount, "jpg", "JPEG_LRG");
                AddImageResElement(item, writer, 1024, 768, playbackPercentage, unplayedCount, "jpg", "JPEG_MED");
                AddImageResElement(item, writer, 640, 480, playbackPercentage, unplayedCount, "jpg", "JPEG_SM");
                AddImageResElement(item, writer, 4096, 4096, playbackPercentage, unplayedCount, "png", "PNG_LRG");
                AddImageResElement(item, writer, 160, 160, playbackPercentage, unplayedCount, "png", "PNG_TN");
            }
        }

        private void AddEmbeddedImageAsCover(string name, XmlWriter writer)
        {
            writer.WriteStartElement("upnp", "albumArtURI", NS_UPNP);
            writer.WriteAttributeString("dlna", "profileID", NS_DLNA, _profile.AlbumArtPn);
            writer.WriteString(_serverAddress + "/Dlna/icons/people480.jpg");
            writer.WriteFullEndElement();

            writer.WriteElementString("upnp", "icon", NS_UPNP, _serverAddress + "/Dlna/icons/people48.jpg");
        }

        private void AddImageResElement(BaseItem item,
            XmlWriter writer,
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

            var albumartUrlInfo = GetImageUrl(imageInfo, maxWidth, maxHeight, playbackPercentage, unplayedCount, format);

            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

            var width = albumartUrlInfo.Width;
            var height = albumartUrlInfo.Height;

            var contentFeatures = new ContentFeatureBuilder(_profile)
                .BuildImageHeader(format, width, height, imageInfo.IsDirectStream, org_Pn);

            writer.WriteAttributeString("protocolInfo", String.Format(
                "http-get:*:{0}:{1}",
                GetMimeType("file." + format),
                contentFeatures
                ));

            if (width.HasValue && height.HasValue)
            {
                writer.WriteAttributeString("resolution", string.Format("{0}x{1}", width.Value, height.Value));
            }

            writer.WriteString(albumartUrlInfo.Url);

            writer.WriteFullEndElement();
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
