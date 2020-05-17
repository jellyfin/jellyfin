#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Emby.Dlna.Configuration;
using Emby.Dlna.ContentDirectory;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;

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
        private readonly IMediaEncoder _mediaEncoder;
        private readonly ILibraryManager _libraryManager;

        public DidlBuilder(
            DeviceProfile profile,
            User user,
            IImageProcessor imageProcessor,
            string serverAddress,
            string accessToken,
            IUserDataManager userDataManager,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            ILogger logger,
            IMediaEncoder mediaEncoder,
            ILibraryManager libraryManager)
        {
            _profile = profile;
            _user = user;
            _imageProcessor = imageProcessor;
            _serverAddress = serverAddress;
            _accessToken = accessToken;
            _userDataManager = userDataManager;
            _localization = localization;
            _mediaSourceManager = mediaSourceManager;
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _libraryManager = libraryManager;
        }

        public static string NormalizeDlnaMediaUrl(string url)
        {
            return url + "&dlnaheaders=true";
        }

        public string GetItemDidl(BaseItem item, User user, BaseItem context, string deviceId, Filter filter, StreamInfo streamInfo)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            {
                using (var writer = XmlWriter.Create(builder, settings))
                {
                    //writer.WriteStartDocument();

                    writer.WriteStartElement(string.Empty, "DIDL-Lite", NS_DIDL);

                    writer.WriteAttributeString("xmlns", "dc", null, NS_DC);
                    writer.WriteAttributeString("xmlns", "dlna", null, NS_DLNA);
                    writer.WriteAttributeString("xmlns", "upnp", null, NS_UPNP);
                    //didl.SetAttribute("xmlns:sec", NS_SEC);

                    WriteXmlRootAttributes(_profile, writer);

                    WriteItemElement(writer, item, user, context, null, deviceId, filter, streamInfo);

                    writer.WriteFullEndElement();
                    //writer.WriteEndDocument();
                }

                return builder.ToString();
            }
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

        public void WriteItemElement(
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
                if (!parent.Equals(Guid.Empty))
                {
                    writer.WriteAttributeString("parentID", GetClientId(parent, null));
                }
            }

            AddGeneralProperties(item, null, context, writer, filter);

            AddSamsungBookmarkInfo(item, user, writer, streamInfo);

            // refID?
            // storeAttribute(itemNode, object, ClassProperties.REF_ID, false);

            if (item is IHasMediaSources)
            {
                if (string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
                {
                    AddAudioResource(writer, item, deviceId, filter, streamInfo);
                }
                else if (string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
                {
                    AddVideoResource(writer, item, deviceId, filter, streamInfo);
                }
            }

            AddCover(item, null, writer);
            writer.WriteFullEndElement();
        }

        private void AddVideoResource(XmlWriter writer, BaseItem video, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            if (streamInfo == null)
            {
                var sources = _mediaSourceManager.GetStaticMediaSources(video, true, _user);

                streamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildVideoItem(new VideoOptions
                {
                    ItemId = video.Id,
                    MediaSources = sources.ToArray(),
                    Profile = _profile,
                    DeviceId = deviceId,
                    MaxBitrate = _profile.MaxStreamingBitrate
                });
            }

            var targetWidth = streamInfo.TargetWidth;
            var targetHeight = streamInfo.TargetHeight;

            var contentFeatureList = new ContentFeatureBuilder(_profile).BuildVideoHeader(streamInfo.Container,
                streamInfo.TargetVideoCodec.FirstOrDefault(),
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                targetWidth,
                targetHeight,
                streamInfo.TargetVideoBitDepth,
                streamInfo.TargetVideoBitrate,
                streamInfo.TargetTimestamp,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks ?? 0,
                streamInfo.TargetVideoProfile,
                streamInfo.TargetVideoLevel,
                streamInfo.TargetFramerate ?? 0,
                streamInfo.TargetPacketLength,
                streamInfo.TranscodeSeekInfo,
                streamInfo.IsTargetAnamorphic,
                streamInfo.IsTargetInterlaced,
                streamInfo.TargetRefFrames,
                streamInfo.TargetVideoStreamCount,
                streamInfo.TargetAudioStreamCount,
                streamInfo.TargetVideoCodecTag,
                streamInfo.IsTargetAVC);

            foreach (var contentFeature in contentFeatureList)
            {
                AddVideoResource(writer, filter, contentFeature, streamInfo);
            }

            var subtitleProfiles = streamInfo.GetSubtitleProfiles(_mediaEncoder, false, _serverAddress, _accessToken);

            foreach (var subtitle in subtitleProfiles)
            {
                if (subtitle.DeliveryMethod != SubtitleDeliveryMethod.External)
                {
                    continue;
                }

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
                .FirstOrDefault(i => string.Equals(info.Format, i.Format, StringComparison.OrdinalIgnoreCase)
                                    && i.Method == SubtitleDeliveryMethod.External);

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
                writer.WriteAttributeString("sec", "type", null, info.Format.ToLowerInvariant());

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
                var protocolInfo = string.Format(
                    CultureInfo.InvariantCulture,
                    "http-get:*:text/{0}:*",
                    info.Format.ToLowerInvariant());
                writer.WriteAttributeString("protocolInfo", protocolInfo);

                writer.WriteString(info.Url);
                writer.WriteFullEndElement();
            }

            return true;
        }

        private void AddVideoResource(XmlWriter writer, Filter filter, string contentFeatures, StreamInfo streamInfo)
        {
            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

            var url = NormalizeDlnaMediaUrl(streamInfo.ToUrl(_serverAddress, _accessToken));

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
                    writer.WriteAttributeString(
                        "resolution",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}x{1}",
                            targetWidth.Value,
                            targetHeight.Value));
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
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                streamInfo.TargetVideoCodec.FirstOrDefault(),
                streamInfo.TargetAudioBitrate,
                targetWidth,
                targetHeight,
                streamInfo.TargetVideoBitDepth,
                streamInfo.TargetVideoProfile,
                streamInfo.TargetVideoLevel,
                streamInfo.TargetFramerate ?? 0,
                streamInfo.TargetPacketLength,
                streamInfo.TargetTimestamp,
                streamInfo.IsTargetAnamorphic,
                streamInfo.IsTargetInterlaced,
                streamInfo.TargetRefFrames,
                streamInfo.TargetVideoStreamCount,
                streamInfo.TargetAudioStreamCount,
                streamInfo.TargetVideoCodecTag,
                streamInfo.IsTargetAVC);

            var filename = url.Substring(0, url.IndexOf('?', StringComparison.Ordinal));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
               ? MimeTypes.GetMimeType(filename)
               : mediaProfile.MimeType;

            writer.WriteAttributeString(
                "protocolInfo",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "http-get:*:{0}:{1}",
                    mimeType,
                    contentFeatures));

            writer.WriteString(url);

            writer.WriteFullEndElement();
        }

        private string GetDisplayName(BaseItem item, StubType? itemStubType, BaseItem context)
        {
            if (itemStubType.HasValue)
            {
                switch (itemStubType.Value)
                {
                    case StubType.Latest: return _localization.GetLocalizedString("Latest");
                    case StubType.Playlists: return _localization.GetLocalizedString("Playlists");
                    case StubType.AlbumArtists: return _localization.GetLocalizedString("HeaderAlbumArtists");
                    case StubType.Albums: return _localization.GetLocalizedString("Albums");
                    case StubType.Artists: return _localization.GetLocalizedString("Artists");
                    case StubType.Songs: return _localization.GetLocalizedString("Songs");
                    case StubType.Genres: return _localization.GetLocalizedString("Genres");
                    case StubType.FavoriteAlbums: return _localization.GetLocalizedString("HeaderFavoriteAlbums");
                    case StubType.FavoriteArtists: return _localization.GetLocalizedString("HeaderFavoriteArtists");
                    case StubType.FavoriteSongs: return _localization.GetLocalizedString("HeaderFavoriteSongs");
                    case StubType.ContinueWatching: return _localization.GetLocalizedString("HeaderContinueWatching");
                    case StubType.Movies: return _localization.GetLocalizedString("Movies");
                    case StubType.Collections: return _localization.GetLocalizedString("Collections");
                    case StubType.Favorites: return _localization.GetLocalizedString("Favorites");
                    case StubType.NextUp: return _localization.GetLocalizedString("HeaderNextUp");
                    case StubType.FavoriteSeries: return _localization.GetLocalizedString("HeaderFavoriteShows");
                    case StubType.FavoriteEpisodes: return _localization.GetLocalizedString("HeaderFavoriteEpisodes");
                    case StubType.Series: return _localization.GetLocalizedString("Shows");
                    default: break;
                }
            }

            return item is Episode episode
                ? GetEpisodeDisplayName(episode, context)
                : item.Name;
        }

        /// <summary>
        /// Gets episode display name appropriate for the given context.
        /// </summary>
        /// <remarks>
        /// If context is a season, this will return a string containing just episode number and name.
        /// Otherwise the result will include series nams and season number.
        /// </remarks>
        /// <param name="episode">The episode.</param>
        /// <param name="context">Current context.</param>
        /// <returns>Formatted name of the episode.</returns>
        private string GetEpisodeDisplayName(Episode episode, BaseItem context)
        {
            string[] components;

            if (context is Season season)
            {
                // This is a special embedded within a season
                if (episode.ParentIndexNumber.HasValue && episode.ParentIndexNumber.Value == 0
                    && season.IndexNumber.HasValue && season.IndexNumber.Value != 0)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("ValueSpecialEpisodeName"),
                        episode.Name);
                }

                // inside a season use simple format (ex. '12 - Episode Name')
                var epNumberName = GetEpisodeIndexFullName(episode);
                components = new[] { epNumberName, episode.Name };
            }
            else
            {
                // outside a season include series and season details (ex. 'TV Show - S05E11 - Episode Name')
                var epNumberName = GetEpisodeNumberDisplayName(episode);
                components = new[] { episode.SeriesName, epNumberName, episode.Name };
            }

            return string.Join(" - ", components.Where(NotNullOrWhiteSpace));
        }

        /// <summary>
        /// Gets complete episode number.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns>For single episodes returns just the number. For double episodes - current and ending numbers.</returns>
        private string GetEpisodeIndexFullName(Episode episode)
        {
            var name = string.Empty;
            if (episode.IndexNumber.HasValue)
            {
                name += episode.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

                if (episode.IndexNumberEnd.HasValue)
                {
                    name += "-" + episode.IndexNumberEnd.Value.ToString("00", CultureInfo.InvariantCulture);
                }
            }

            return name;
        }

        /// <summary>
        /// Gets episode number formatted as 'S##E##'.
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <returns>Formatted episode number.</returns>
        private string GetEpisodeNumberDisplayName(Episode episode)
        {
            var name = string.Empty;
            var seasonNumber = episode.Season?.IndexNumber;

            if (seasonNumber.HasValue)
            {
                name = "S" + seasonNumber.Value.ToString("00", CultureInfo.InvariantCulture);
            }

            var indexName = GetEpisodeIndexFullName(episode);

            if (!string.IsNullOrWhiteSpace(indexName))
            {
                name += "E" + indexName;
            }

            return name;
        }

        private bool NotNullOrWhiteSpace(string s) => !string.IsNullOrWhiteSpace(s);

        private void AddAudioResource(XmlWriter writer, BaseItem audio, string deviceId, Filter filter, StreamInfo streamInfo = null)
        {
            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

            if (streamInfo == null)
            {
                var sources = _mediaSourceManager.GetStaticMediaSources(audio, true, _user);

                streamInfo = new StreamBuilder(_mediaEncoder, _logger).BuildAudioItem(new AudioOptions
                {
                    ItemId = audio.Id,
                    MediaSources = sources.ToArray(),
                    Profile = _profile,
                    DeviceId = deviceId
                });
            }

            var url = NormalizeDlnaMediaUrl(streamInfo.ToUrl(_serverAddress, _accessToken));

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
            var targetAudioBitDepth = streamInfo.TargetAudioBitDepth;

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
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                targetChannels,
                targetAudioBitrate,
                targetSampleRate,
                targetAudioBitDepth);

            var filename = url.Substring(0, url.IndexOf('?', StringComparison.Ordinal));

            var mimeType = mediaProfile == null || string.IsNullOrEmpty(mediaProfile.MimeType)
                ? MimeTypes.GetMimeType(filename)
                : mediaProfile.MimeType;

            var contentFeatures = new ContentFeatureBuilder(_profile).BuildAudioHeader(streamInfo.Container,
                streamInfo.TargetAudioCodec.FirstOrDefault(),
                targetAudioBitrate,
                targetSampleRate,
                targetChannels,
                targetAudioBitDepth,
                streamInfo.IsDirectStream,
                streamInfo.RunTimeTicks ?? 0,
                streamInfo.TranscodeSeekInfo);

            writer.WriteAttributeString(
                "protocolInfo",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "http-get:*:{0}:{1}",
                    mimeType,
                    contentFeatures));

            writer.WriteString(url);

            writer.WriteFullEndElement();
        }

        public static bool IsIdRoot(string id)
            => string.IsNullOrWhiteSpace(id)
                || string.Equals(id, "0", StringComparison.OrdinalIgnoreCase)
                // Samsung sometimes uses 1 as root
                || string.Equals(id, "1", StringComparison.OrdinalIgnoreCase);

        public void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem context, int childCount, Filter filter, string requestedId = null)
        {
            writer.WriteStartElement(string.Empty, "container", NS_DIDL);

            writer.WriteAttributeString("restricted", "1");
            writer.WriteAttributeString("searchable", "1");
            writer.WriteAttributeString("childCount", childCount.ToString(_usCulture));

            var clientId = GetClientId(folder, stubType);

            if (string.Equals(requestedId, "0", StringComparison.Ordinal))
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
                    if (parent.Equals(Guid.Empty))
                    {
                        writer.WriteAttributeString("parentID", "0");
                    }
                    else
                    {
                        writer.WriteAttributeString("parentID", GetClientId(parent, null));
                    }
                }
            }

            AddGeneralProperties(folder, stubType, context, writer, filter);

            AddCover(folder, stubType, writer);

            writer.WriteFullEndElement();
        }

        private void AddSamsungBookmarkInfo(BaseItem item, User user, XmlWriter writer, StreamInfo streamInfo)
        {
            if (!item.SupportsPositionTicksResume || item is Folder)
            {
                return;
            }

            MediaBrowser.Model.Dlna.XmlAttribute secAttribute = null;
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

            var userdata = _userDataManager.GetUserData(user, item);
            var playbackPositionTicks = (streamInfo != null && streamInfo.StartPositionTicks > 0) ? streamInfo.StartPositionTicks : userdata.PlaybackPositionTicks;

            if (playbackPositionTicks > 0)
            {
                var elementValue = string.Format(
                    CultureInfo.InvariantCulture,
                    "BM={0}",
                    Convert.ToInt32(TimeSpan.FromTicks(playbackPositionTicks).TotalSeconds));
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
                    AddValue(writer, "dc", "date", item.PremiereDate.Value.ToString("o", CultureInfo.InvariantCulture), NS_DC);
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

            if (!(item is Folder))
            {
                if (filter.Contains("dc:description"))
                {
                    var desc = item.Overview;

                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        AddValue(writer, "dc", "description", desc, NS_DC);
                    }
                }
                //if (filter.Contains("upnp:longDescription"))
                //{
                //    if (!string.IsNullOrWhiteSpace(item.Overview))
                //    {
                //        AddValue(writer, "upnp", "longDescription", item.Overview, NS_UPNP);
                //    }
                //}
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
            else if (item is Genre)
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
            if (!item.SupportsPeople)
            {
                return;
            }

            var types = new[]
            {
                PersonType.Director,
                PersonType.Writer,
                PersonType.Producer,
                PersonType.Composer,
                "creator"
            };

            // Seeing some LG models locking up due content with large lists of people
            // The actual issue might just be due to processing a more metadata than it can handle
            var people = _libraryManager.GetPeople(
                new InternalPeopleQuery
                {
                    ItemId = item.Id,
                    Limit = 6
                });

            foreach (var actor in people)
            {
                var type = types.FirstOrDefault(i => string.Equals(i, actor.Type, StringComparison.OrdinalIgnoreCase) || string.Equals(i, actor.Role, StringComparison.OrdinalIgnoreCase))
                    ?? PersonType.Actor;

                AddValue(writer, "upnp", type.ToLowerInvariant(), actor.Name, NS_UPNP);
            }
        }

        private void AddGeneralProperties(BaseItem item, StubType? itemStubType, BaseItem context, XmlWriter writer, Filter filter)
        {
            AddCommonFields(item, itemStubType, context, writer, filter);

            var hasAlbumArtists = item as IHasAlbumArtist;

            if (item is IHasArtist hasArtists)
            {
                foreach (var artist in hasArtists.Artists)
                {
                    AddValue(writer, "upnp", "artist", artist, NS_UPNP);
                    AddValue(writer, "dc", "creator", artist, NS_DC);

                    // If it doesn't support album artists (musicvideo), then tag as both
                    if (hasAlbumArtists == null)
                    {
                        AddAlbumArtist(writer, artist);
                    }
                }
            }

            if (hasAlbumArtists != null)
            {
                foreach (var albumArtist in hasAlbumArtists.AlbumArtists)
                {
                    AddAlbumArtist(writer, albumArtist);
                }
            }

            if (!string.IsNullOrWhiteSpace(item.Album))
            {
                AddValue(writer, "upnp", "album", item.Album, NS_UPNP);
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
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Error adding xml value: {value}", name);
            }
        }

        private void AddValue(XmlWriter writer, string prefix, string name, string value, string namespaceUri)
        {
            try
            {
                writer.WriteElementString(prefix, name, namespaceUri, value);
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Error adding xml value: {value}", value);
            }
        }

        private void AddCover(BaseItem item, StubType? stubType, XmlWriter writer)
        {
            ImageDownloadInfo imageInfo = GetImageInfo(item);

            if (imageInfo == null)
            {
                return;
            }

            var albumartUrlInfo = GetImageUrl(imageInfo, _profile.MaxAlbumArtWidth, _profile.MaxAlbumArtHeight, "jpg");

            writer.WriteStartElement("upnp", "albumArtURI", NS_UPNP);
            writer.WriteAttributeString("dlna", "profileID", NS_DLNA, _profile.AlbumArtPn);
            writer.WriteString(albumartUrlInfo.Url);
            writer.WriteFullEndElement();

            // TOOD: Remove these default values
            var iconUrlInfo = GetImageUrl(imageInfo, _profile.MaxIconWidth ?? 48, _profile.MaxIconHeight ?? 48, "jpg");
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

            if (!_profile.EnableSingleAlbumArtLimit || string.Equals(item.MediaType, MediaType.Photo, StringComparison.OrdinalIgnoreCase))
            {
                AddImageResElement(item, writer, 4096, 4096, "jpg", "JPEG_LRG");
                AddImageResElement(item, writer, 1024, 768, "jpg", "JPEG_MED");
                AddImageResElement(item, writer, 640, 480, "jpg", "JPEG_SM");
                AddImageResElement(item, writer, 4096, 4096, "png", "PNG_LRG");
                AddImageResElement(item, writer, 160, 160, "png", "PNG_TN");
            }

            AddImageResElement(item, writer, 160, 160, "jpg", "JPEG_TN");

        }

        private void AddImageResElement(
            BaseItem item,
            XmlWriter writer,
            int maxWidth,
            int maxHeight,
            string format,
            string org_Pn)
        {
            var imageInfo = GetImageInfo(item);

            if (imageInfo == null)
            {
                return;
            }

            var albumartUrlInfo = GetImageUrl(imageInfo, maxWidth, maxHeight, format);

            writer.WriteStartElement(string.Empty, "res", NS_DIDL);

            // Images must have a reported size or many clients (Bubble upnp), will only use the first thumbnail
            // rather than using a larger one when available
            var width = albumartUrlInfo.Width ?? maxWidth;
            var height = albumartUrlInfo.Height ?? maxHeight;

            var contentFeatures = new ContentFeatureBuilder(_profile)
                .BuildImageHeader(format, width, height, imageInfo.IsDirectStream, org_Pn);

            writer.WriteAttributeString(
                "protocolInfo",
                string.Format(
                    CultureInfo.InvariantCulture,
                    "http-get:*:{0}:{1}",
                    MimeTypes.GetMimeType("file." + format),
                    contentFeatures));

            writer.WriteAttributeString(
                "resolution",
                string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height));

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

            // For audio tracks without art use album art if available.
            if (item is Audio audioItem)
            {
                var album = audioItem.AlbumEntity;
                return album != null && album.HasImage(ImageType.Primary)
                    ? GetImageInfo(album, ImageType.Primary)
                    : null;
            }

            // Don't look beyond album/playlist level. Metadata service may assign an image from a different album/show to the parent folder.
            if (item is MusicAlbum || item is Playlist)
            {
                return null;
            }

            // For other item types check parents, but be aware that image retrieved from a parent may be not suitable for this media item.
            var parentWithImage = GetFirstParentWithImageBelowUserRoot(item);
            if (parentWithImage != null)
            {
                return GetImageInfo(parentWithImage, ImageType.Primary);
            }

            return null;
        }

        private BaseItem GetFirstParentWithImageBelowUserRoot(BaseItem item)
        {
            if (item == null)
            {
                return null;
            }

            if (item.HasImage(ImageType.Primary))
            {
                return item;
            }

            var parent = item.GetParent();
            if (parent is UserRootFolder)
            {
                return null;
            }

            // terminate in case we went past user root folder (unlikely?)
            if (parent is Folder folder && folder.IsRoot)
            {
                return null;
            }

            return GetFirstParentWithImageBelowUserRoot(parent);
        }

        private ImageDownloadInfo GetImageInfo(BaseItem item, ImageType type)
        {
            var imageInfo = item.GetImageInfo(type, 0);
            string tag = null;

            try
            {
                tag = _imageProcessor.GetImageCacheTag(item, type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image cache tag");
            }

            int? width = imageInfo.Width;
            int? height = imageInfo.Height;

            if (width == 0 || height == 0)
            {
                //_imageProcessor.GetImageSize(item, imageInfo);
                width = null;
                height = null;
            }

            else if (width == -1 || height == -1)
            {
                width = null;
                height = null;
            }

            //try
            //{
            //    var size = _imageProcessor.GetImageSize(imageInfo);

            //    width = size.Width;
            //    height = size.Height;
            //}
            //catch
            //{

            //}

            var inputFormat = (Path.GetExtension(imageInfo.Path) ?? string.Empty)
                .TrimStart('.')
                .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

            return new ImageDownloadInfo
            {
                ItemId = item.Id,
                Type = type,
                ImageTag = tag,
                Width = width,
                Height = height,
                Format = inputFormat,
                ItemImageInfo = imageInfo
            };
        }

        private class ImageDownloadInfo
        {
            internal Guid ItemId;
            internal string ImageTag;
            internal ImageType Type;

            internal int? Width;
            internal int? Height;

            internal bool IsDirectStream;

            internal string Format;

            internal ItemImageInfo ItemImageInfo;
        }

        private class ImageUrlInfo
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
            var id = idValue.ToString("N", CultureInfo.InvariantCulture);

            if (stubType.HasValue)
            {
                id = stubType.Value.ToString().ToLowerInvariant() + "_" + id;
            }

            return id;
        }

        private ImageUrlInfo GetImageUrl(ImageDownloadInfo info, int maxWidth, int maxHeight, string format)
        {
            var url = string.Format(
                CultureInfo.InvariantCulture,
                "{0}/Items/{1}/Images/{2}/0/{3}/{4}/{5}/{6}/0/0",
                _serverAddress,
                info.ItemId.ToString("N", CultureInfo.InvariantCulture),
                info.Type,
                info.ImageTag,
                format,
                maxWidth.ToString(CultureInfo.InvariantCulture),
                maxHeight.ToString(CultureInfo.InvariantCulture));

            var width = info.Width;
            var height = info.Height;

            info.IsDirectStream = false;

            if (width.HasValue && height.HasValue)
            {
                var newSize = DrawingUtils.Resize(
                        new ImageDimensions(width.Value, height.Value), 0, 0, maxWidth, maxHeight);

                width = newSize.Width;
                height = newSize.Height;

                var normalizedFormat = format
                    .Replace("jpeg", "jpg", StringComparison.OrdinalIgnoreCase);

                if (string.Equals(info.Format, normalizedFormat, StringComparison.OrdinalIgnoreCase))
                {
                    info.IsDirectStream = maxWidth >= width.Value && maxHeight >= height.Value;
                }
            }

            // just lie
            info.IsDirectStream = true;

            return new ImageUrlInfo
            {
                Url = url,
                Width = width,
                Height = height
            };
        }
    }
}
