#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Emby.Dlna.Didl;
using Emby.Dlna.Service;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Genre = MediaBrowser.Controller.Entities.Genre;

namespace Emby.Dlna.ContentDirectory
{
    /// <summary>
    /// Defines the <see cref="ControlHandler" />.
    /// </summary>
    public class ControlHandler : BaseControlHandler
    {
        private const string NsDc = "http://purl.org/dc/elements/1.1/";
        private const string NsDidl = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NsDlna = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NsUpnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly User _user;
        private readonly IUserViewManager _userViewManager;
        private readonly ITVSeriesManager _tvSeriesManager;

        private readonly int _systemUpdateId;

        private readonly DidlBuilder _didlBuilder;

        private readonly DeviceProfile _profile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlHandler"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="libraryManager">The <see cref="ILibraryManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="profile">The <see cref="DeviceProfile"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="serverAddress">The server address to use in this instance> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="accessToken">The <see cref="string"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="imageProcessor">The <see cref="IImageProcessor"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="userDataManager">The <see cref="IUserDataManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="user">The <see cref="User"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="systemUpdateId">The system id for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="config">The <see cref="IServerConfigurationManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="localization">The <see cref="ILocalizationManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="mediaSourceManager">The <see cref="IMediaSourceManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="userViewManager">The <see cref="IUserViewManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="mediaEncoder">The <see cref="IMediaEncoder"/> for use with the <see cref="ControlHandler"/> instance.</param>
        /// <param name="tvSeriesManager">The <see cref="ITVSeriesManager"/> for use with the <see cref="ControlHandler"/> instance.</param>
        public ControlHandler(
            ILogger logger,
            ILibraryManager libraryManager,
            DeviceProfile profile,
            string serverAddress,
            string accessToken,
            IImageProcessor imageProcessor,
            IUserDataManager userDataManager,
            User user,
            int systemUpdateId,
            IServerConfigurationManager config,
            ILocalizationManager localization,
            IMediaSourceManager mediaSourceManager,
            IUserViewManager userViewManager,
            IMediaEncoder mediaEncoder,
            ITVSeriesManager tvSeriesManager)
            : base(config, logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
            _userViewManager = userViewManager;
            _tvSeriesManager = tvSeriesManager;
            _profile = profile;

            _didlBuilder = new DidlBuilder(
                profile,
                user,
                imageProcessor,
                serverAddress,
                accessToken,
                userDataManager,
                localization,
                mediaSourceManager,
                Logger,
                mediaEncoder,
                libraryManager);
        }

        /// <inheritdoc />
        protected override void WriteResult(string methodName, IReadOnlyDictionary<string, string> methodParams, XmlWriter xmlWriter)
        {
            ArgumentNullException.ThrowIfNull(xmlWriter);

            ArgumentNullException.ThrowIfNull(methodParams);

            const string DeviceId = "test";

            if (string.Equals(methodName, "GetSearchCapabilities", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetSearchCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSortCapabilities", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetSortCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSortExtensionCapabilities", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetSortExtensionCapabilities(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetSystemUpdateID", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetSystemUpdateID(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "Browse", StringComparison.OrdinalIgnoreCase))
            {
                HandleBrowse(xmlWriter, methodParams, DeviceId);
                return;
            }

            if (string.Equals(methodName, "X_GetFeatureList", StringComparison.OrdinalIgnoreCase))
            {
                HandleXGetFeatureList(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "GetFeatureList", StringComparison.OrdinalIgnoreCase))
            {
                HandleGetFeatureList(xmlWriter);
                return;
            }

            if (string.Equals(methodName, "X_SetBookmark", StringComparison.OrdinalIgnoreCase))
            {
                HandleXSetBookmark(methodParams);
                return;
            }

            if (string.Equals(methodName, "Search", StringComparison.OrdinalIgnoreCase))
            {
                HandleSearch(xmlWriter, methodParams, DeviceId);
                return;
            }

            if (string.Equals(methodName, "X_BrowseByLetter", StringComparison.OrdinalIgnoreCase))
            {
                HandleXBrowseByLetter(xmlWriter, methodParams, DeviceId);
                return;
            }

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        /// <summary>
        /// Adds a "XSetBookmark" element to the xml document.
        /// </summary>
        /// <param name="sparams">The method parameters.</param>
        private void HandleXSetBookmark(IReadOnlyDictionary<string, string> sparams)
        {
            var id = sparams["ObjectID"];

            var serverItem = GetItemFromObjectId(id);

            var item = serverItem.Item;

            var newbookmark = int.Parse(sparams["PosSecond"], CultureInfo.InvariantCulture);

            var userdata = _userDataManager.GetUserData(_user, item);

            userdata.PlaybackPositionTicks = TimeSpan.FromSeconds(newbookmark).Ticks;

            _userDataManager.SaveUserData(
                _user,
                item,
                userdata,
                UserDataSaveReason.TogglePlayed,
                CancellationToken.None);
        }

        /// <summary>
        /// Adds the "SearchCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSearchCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SearchCaps",
                "res@resolution,res@size,res@duration,dc:title,dc:creator,upnp:actor,upnp:artist,upnp:genre,upnp:album,dc:date,upnp:class,@id,@refID,@protocolInfo,upnp:author,dc:description,pv:avKeywords");
        }

        /// <summary>
        /// Adds the "SortCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSortCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SortCaps",
                "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
        }

        /// <summary>
        /// Adds the "SortExtensionCaps" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetSortExtensionCapabilities(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString(
                "SortExtensionCaps",
                "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating");
        }

        /// <summary>
        /// Adds the "Id" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private void HandleGetSystemUpdateID(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("Id", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Adds the "FeatureList" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleGetFeatureList(XmlWriter xmlWriter)
        {
            xmlWriter.WriteElementString("FeatureList", WriteFeatureListXml());
        }

        /// <summary>
        /// Adds the "FeatureList" element to the xml document.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        private static void HandleXGetFeatureList(XmlWriter xmlWriter)
            => HandleGetFeatureList(xmlWriter);

        /// <summary>
        /// Builds a static feature list.
        /// </summary>
        /// <returns>The xml feature list.</returns>
        private static string WriteFeatureListXml()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<Features xmlns=\"urn:schemas-upnp-org:av:avs\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd\">"
                + "<Feature name=\"samsung.com_BASICVIEW\" version=\"1\">"
                + "<container id=\"0\" type=\"object.item.imageItem\"/>"
                + "<container id=\"0\" type=\"object.item.audioItem\"/>"
                + "<container id=\"0\" type=\"object.item.videoItem\"/>"
                + "</Feature>"
                + "</Features>";
        }

        /// <summary>
        /// Builds the "Browse" xml response.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The method parameters.</param>
        /// <param name="deviceId">The device Id to use.</param>
        private void HandleBrowse(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", string.Empty));

            var provided = 0;

            // Default to null instead of 0
            // Upnp inspector sends 0 as requestedCount when it wants everything
            int? requestedCount = null;
            int? start = 0;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
            }

            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal > 0)
            {
                start = startVal;
            }

            int totalCount;

            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);

                writer.WriteAttributeString("xmlns", "dc", null, NsDc);
                writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
                writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

                DidlBuilder.WriteXmlRootAttributes(_profile, writer);

                var serverItem = GetItemFromObjectId(id);
                var item = serverItem.Item;

                if (string.Equals(flag, "BrowseMetadata", StringComparison.Ordinal))
                {
                    totalCount = 1;

                    if (item.IsDisplayedAsFolder || serverItem.StubType.HasValue)
                    {
                        var childrenResult = GetUserItems(item, serverItem.StubType, _user, sortCriteria, start, requestedCount);

                        _didlBuilder.WriteFolderElement(writer, item, serverItem.StubType, null, childrenResult.TotalRecordCount, filter, id);
                    }
                    else
                    {
                        _didlBuilder.WriteItemElement(writer, item, _user, null, null, deviceId, filter);
                    }

                    provided++;
                }
                else
                {
                    var childrenResult = GetUserItems(item, serverItem.StubType, _user, sortCriteria, start, requestedCount);
                    totalCount = childrenResult.TotalRecordCount;

                    provided = childrenResult.Items.Count;

                    foreach (var i in childrenResult.Items)
                    {
                        var childItem = i.Item;
                        var displayStubType = i.StubType;

                        if (childItem.IsDisplayedAsFolder || displayStubType.HasValue)
                        {
                            var childCount = GetUserItems(childItem, displayStubType, _user, sortCriteria, null, 0)
                                .TotalRecordCount;

                            _didlBuilder.WriteFolderElement(writer, childItem, displayStubType, item, childCount, filter);
                        }
                        else
                        {
                            _didlBuilder.WriteItemElement(writer, childItem, _user, item, serverItem.StubType, deviceId, filter);
                        }
                    }
                }

                writer.WriteFullEndElement();
                writer.Flush();
                xmlWriter.WriteElementString("Result", builder.ToString());
            }

            xmlWriter.WriteElementString("NumberReturned", provided.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("TotalMatches", totalCount.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("UpdateID", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Builds the response to the "X_BrowseByLetter request.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The method parameters.</param>
        /// <param name="deviceId">The device id.</param>
        private void HandleXBrowseByLetter(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
        {
            // TODO: Implement this method
            HandleSearch(xmlWriter, sparams, deviceId);
        }

        /// <summary>
        /// Builds a response to the "Search" request.
        /// </summary>
        /// <param name="xmlWriter">The xmlWriter<see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The method parameters.</param>
        /// <param name="deviceId">The deviceId<see cref="string"/>.</param>
        private void HandleSearch(XmlWriter xmlWriter, IReadOnlyDictionary<string, string> sparams, string deviceId)
        {
            var searchCriteria = new SearchCriteria(sparams.GetValueOrDefault("SearchCriteria", string.Empty));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", string.Empty));
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));

            // sort example: dc:title, dc:date

            // Default to null instead of 0
            // Upnp inspector sends 0 as requestedCount when it wants everything
            int? requestedCount = null;
            int? start = 0;

            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out var requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
            }

            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out var startVal) && startVal > 0)
            {
                start = startVal;
            }

            QueryResult<BaseItem> childrenResult;
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                CloseOutput = false,
                OmitXmlDeclaration = true,
                ConformanceLevel = ConformanceLevel.Fragment
            };

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            using (var writer = XmlWriter.Create(builder, settings))
            {
                writer.WriteStartElement(string.Empty, "DIDL-Lite", NsDidl);
                writer.WriteAttributeString("xmlns", "dc", null, NsDc);
                writer.WriteAttributeString("xmlns", "dlna", null, NsDlna);
                writer.WriteAttributeString("xmlns", "upnp", null, NsUpnp);

                DidlBuilder.WriteXmlRootAttributes(_profile, writer);

                var serverItem = GetItemFromObjectId(sparams["ContainerID"]);

                var item = serverItem.Item;

                childrenResult = GetChildrenSorted(item, _user, searchCriteria, sortCriteria, start, requestedCount);
                foreach (var i in childrenResult.Items)
                {
                    if (i.IsDisplayedAsFolder)
                    {
                        var childCount = GetChildrenSorted(i, _user, searchCriteria, sortCriteria, null, 0)
                            .TotalRecordCount;

                        _didlBuilder.WriteFolderElement(writer, i, null, item, childCount, filter);
                    }
                    else
                    {
                        _didlBuilder.WriteItemElement(writer, i, _user, item, serverItem.StubType, deviceId, filter);
                    }
                }

                writer.WriteFullEndElement();
                writer.Flush();
                xmlWriter.WriteElementString("Result", builder.ToString());
            }

            xmlWriter.WriteElementString("NumberReturned", childrenResult.Items.Count.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("TotalMatches", childrenResult.TotalRecordCount.ToString(CultureInfo.InvariantCulture));
            xmlWriter.WriteElementString("UpdateID", _systemUpdateId.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Returns the child items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="search">The <see cref="SearchCriteria"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{BaseItem}"/>.</returns>
        private static QueryResult<BaseItem> GetChildrenSorted(BaseItem item, User user, SearchCriteria search, SortCriteria sort, int? startIndex, int? limit)
        {
            var folder = (Folder)item;

            string[] mediaTypes = Array.Empty<string>();
            bool? isFolder = null;

            switch (search.SearchType)
            {
                case SearchType.Audio:
                    mediaTypes = new[] { MediaType.Audio };
                    isFolder = false;
                    break;
                case SearchType.Video:
                    mediaTypes = new[] { MediaType.Video };
                    isFolder = false;
                    break;
                case SearchType.Image:
                    mediaTypes = new[] { MediaType.Photo };
                    isFolder = false;
                    break;
                case SearchType.Playlist:
                case SearchType.MusicAlbum:
                    isFolder = true;
                    break;
            }

            return folder.GetItems(new InternalItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                OrderBy = GetOrderBy(sort, folder.IsPreSorted),
                User = user,
                Recursive = true,
                IsMissing = false,
                ExcludeItemTypes = new[] { BaseItemKind.Book },
                IsFolder = isFolder,
                MediaTypes = mediaTypes,
                DtoOptions = GetDtoOptions()
            });
        }

        /// <summary>
        /// Returns a new DtoOptions object.
        /// </summary>
        /// <returns>The <see cref="DtoOptions"/>.</returns>
        private static DtoOptions GetDtoOptions()
        {
            return new DtoOptions(true);
        }

        /// <summary>
        /// Returns the User items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetUserItems(BaseItem item, StubType? stubType, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            switch (item)
            {
                case MusicGenre:
                    return GetMusicGenreItems(item, user, sort, startIndex, limit);
                case MusicArtist:
                    return GetMusicArtistItems(item, user, sort, startIndex, limit);
                case Genre:
                    return GetGenreItems(item, user, sort, startIndex, limit);
            }

            if (stubType != StubType.Folder && item is IHasCollectionType collectionFolder)
            {
                var collectionType = collectionFolder.CollectionType;
                if (string.Equals(CollectionType.Music, collectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMusicFolders(item, user, stubType, sort, startIndex, limit);
                }

                if (string.Equals(CollectionType.Movies, collectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMovieFolders(item, user, stubType, sort, startIndex, limit);
                }

                if (string.Equals(CollectionType.TvShows, collectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetTvFolders(item, user, stubType, sort, startIndex, limit);
                }

                if (string.Equals(CollectionType.Folders, collectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetFolders(user, startIndex, limit);
                }

                if (string.Equals(CollectionType.LiveTv, collectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetLiveTvChannels(user, sort, startIndex, limit);
                }
            }

            if (stubType.HasValue && stubType.Value != StubType.Folder)
            {
                // TODO should this be doing something?
                return new QueryResult<ServerItem>();
            }

            var folder = (Folder)item;

            var query = new InternalItemsQuery(user)
            {
                Limit = limit,
                StartIndex = startIndex,
                IsVirtualItem = false,
                ExcludeItemTypes = new[] { BaseItemKind.Book },
                IsPlaceHolder = false,
                DtoOptions = GetDtoOptions(),
                OrderBy = GetOrderBy(sort, folder.IsPreSorted)
            };

            var queryResult = folder.GetItems(query);

            return ToResult(startIndex, queryResult);
        }

        /// <summary>
        /// Returns the Live Tv Channels meeting the criteria.
        /// </summary>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetLiveTvChannels(User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                IncludeItemTypes = new[] { BaseItemKind.LiveTvChannel },
                OrderBy = GetOrderBy(sort, false)
            };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(startIndex, result);
        }

        /// <summary>
        /// Returns the music folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicFolders(BaseItem item, User user, StubType? stubType, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                OrderBy = GetOrderBy(sort, false)
            };

            switch (stubType)
            {
                case StubType.Latest:
                    return GetLatest(item, query, BaseItemKind.Audio);
                case StubType.Playlists:
                    return GetMusicPlaylists(query);
                case StubType.Albums:
                    return GetChildrenOfItem(item, query, BaseItemKind.MusicAlbum);
                case StubType.Artists:
                    return GetMusicArtists(item, query);
                case StubType.AlbumArtists:
                    return GetMusicAlbumArtists(item, query);
                case StubType.FavoriteAlbums:
                    return GetChildrenOfItem(item, query, BaseItemKind.MusicAlbum, true);
                case StubType.FavoriteArtists:
                    return GetFavoriteArtists(item, query);
                case StubType.FavoriteSongs:
                    return GetChildrenOfItem(item, query, BaseItemKind.Audio, true);
                case StubType.Songs:
                    return GetChildrenOfItem(item, query, BaseItemKind.Audio);
                case StubType.Genres:
                    return GetMusicGenres(item, query);
            }

            var serverItems = new ServerItem[]
            {
                new(item, StubType.Latest),
                new(item, StubType.Playlists),
                new(item, StubType.Albums),
                new(item, StubType.AlbumArtists),
                new(item, StubType.Artists),
                new(item, StubType.Songs),
                new(item, StubType.Genres),
                new(item, StubType.FavoriteArtists),
                new(item, StubType.FavoriteAlbums),
                new(item, StubType.FavoriteSongs)
            };

            if (limit < serverItems.Length)
            {
                serverItems = serverItems[..limit.Value];
            }

            return new QueryResult<ServerItem>(
                startIndex,
                serverItems.Length,
                serverItems);
        }

        /// <summary>
        /// Returns the movie folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieFolders(BaseItem item, User user, StubType? stubType, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                OrderBy = GetOrderBy(sort, false)
            };

            switch (stubType)
            {
                case StubType.ContinueWatching:
                    return GetMovieContinueWatching(item, query);
                case StubType.Latest:
                    return GetLatest(item, query, BaseItemKind.Movie);
                case StubType.Movies:
                    return GetChildrenOfItem(item, query, BaseItemKind.Movie);
                case StubType.Collections:
                    return GetMovieCollections(query);
                case StubType.Favorites:
                    return GetChildrenOfItem(item, query, BaseItemKind.Movie, true);
                case StubType.Genres:
                    return GetGenres(item, query);
            }

            var array = new ServerItem[]
            {
                new(item, StubType.ContinueWatching),
                new(item, StubType.Latest),
                new(item, StubType.Movies),
                new(item, StubType.Collections),
                new(item, StubType.Favorites),
                new(item, StubType.Genres)
            };

            if (limit < array.Length)
            {
                array = array[..limit.Value];
            }

            return new QueryResult<ServerItem>(
                startIndex,
                array.Length,
                array);
        }

        /// <summary>
        /// Returns the folders meeting the criteria.
        /// </summary>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFolders(User user, int? startIndex, int? limit)
        {
            var folders = _libraryManager.GetUserRootFolder().GetChildren(user, true);
            var totalRecordCount = folders.Count;
            // Handle paging
            var items = folders
                .OrderBy(i => i.SortName)
                .Skip(startIndex ?? 0)
                .Take(limit ?? int.MaxValue)
                .Select(i => new ServerItem(i, StubType.Folder))
                .ToArray();

            return new QueryResult<ServerItem>(
                startIndex,
                totalRecordCount,
                items);
        }

        /// <summary>
        /// Returns the TV folders meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="stubType">The <see cref="StubType"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetTvFolders(BaseItem item, User user, StubType? stubType, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                StartIndex = startIndex,
                Limit = limit,
                OrderBy = GetOrderBy(sort, false)
            };

            switch (stubType)
            {
                case StubType.ContinueWatching:
                    return GetMovieContinueWatching(item, query);
                case StubType.NextUp:
                    return GetNextUp(item, query);
                case StubType.Latest:
                    return GetLatest(item, query, BaseItemKind.Episode);
                case StubType.Series:
                    return GetChildrenOfItem(item, query, BaseItemKind.Series);
                case StubType.FavoriteSeries:
                    return GetChildrenOfItem(item, query, BaseItemKind.Series, true);
                case StubType.FavoriteEpisodes:
                    return GetChildrenOfItem(item, query, BaseItemKind.Episode, true);
                case StubType.Genres:
                    return GetGenres(item, query);
            }

            var serverItems = new ServerItem[]
            {
                new(item, StubType.ContinueWatching),
                new(item, StubType.NextUp),
                new(item, StubType.Latest),
                new(item, StubType.Series),
                new(item, StubType.FavoriteSeries),
                new(item, StubType.FavoriteEpisodes),
                new(item, StubType.Genres)
            };

            if (limit < serverItems.Length)
            {
                serverItems = serverItems[..limit.Value];
            }

            return new QueryResult<ServerItem>(
                startIndex,
                serverItems.Length,
                serverItems);
        }

        /// <summary>
        /// Returns the Movies that are part watched that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieContinueWatching(BaseItem parent, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;

            query.OrderBy = new[]
            {
                (ItemSortBy.DatePlayed, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            };

            query.IsResumable = true;
            query.Limit ??= 10;

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(query.StartIndex, result);
        }

        /// <summary>
        /// Returns the Movie collections meeting the criteria.
        /// </summary>
        /// <param name="query">The see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieCollections(InternalItemsQuery query)
        {
            query.Recursive = true;
            query.IncludeItemTypes = new[] { BaseItemKind.BoxSet };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(query.StartIndex, result);
        }

        /// <summary>
        /// Returns the children that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <param name="itemType">The item type.</param>
        /// <param name="isFavorite">A value indicating whether to only fetch favorite items.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetChildrenOfItem(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType, bool isFavorite = false)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.IsFavorite = isFavorite;
            query.IncludeItemTypes = new[] { itemType };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(query.StartIndex, result);
        }

        /// <summary>
        /// Returns the genres meeting the criteria.
        /// The GetGenres.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenres(BaseItem parent, InternalItemsQuery query)
        {
            // Don't sort
            query.OrderBy = Array.Empty<(string, SortOrder)>();
            query.AncestorIds = new[] { parent.Id };
            var genresResult = _libraryManager.GetGenres(query);

            return ToResult(query.StartIndex, genresResult);
        }

        /// <summary>
        /// Returns the music genres meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenres(BaseItem parent, InternalItemsQuery query)
        {
            // Don't sort
            query.OrderBy = Array.Empty<(string, SortOrder)>();
            query.AncestorIds = new[] { parent.Id };
            var genresResult = _libraryManager.GetMusicGenres(query);

            return ToResult(query.StartIndex, genresResult);
        }

        /// <summary>
        /// Returns the music albums by artist that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicAlbumArtists(BaseItem parent, InternalItemsQuery query)
        {
            // Don't sort
            query.OrderBy = Array.Empty<(string, SortOrder)>();
            query.AncestorIds = new[] { parent.Id };
            var artists = _libraryManager.GetAlbumArtists(query);

            return ToResult(query.StartIndex, artists);
        }

        /// <summary>
        /// Returns the music artists meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtists(BaseItem parent, InternalItemsQuery query)
        {
            // Don't sort
            query.OrderBy = Array.Empty<(string, SortOrder)>();
            query.AncestorIds = new[] { parent.Id };
            var artists = _libraryManager.GetArtists(query);
            return ToResult(query.StartIndex, artists);
        }

        /// <summary>
        /// Returns the artists tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteArtists(BaseItem parent, InternalItemsQuery query)
        {
            // Don't sort
            query.OrderBy = Array.Empty<(string, SortOrder)>();
            query.AncestorIds = new[] { parent.Id };
            query.IsFavorite = true;
            var artists = _libraryManager.GetArtists(query);
            return ToResult(query.StartIndex, artists);
        }

        /// <summary>
        /// Returns the music playlists meeting the criteria.
        /// </summary>
        /// <param name="query">The query<see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicPlaylists(InternalItemsQuery query)
        {
            query.Parent = null;
            query.IncludeItemTypes = new[] { BaseItemKind.Playlist };
            query.Recursive = true;

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(query.StartIndex, result);
        }

        /// <summary>
        /// Returns the next up item meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetNextUp(BaseItem parent, InternalItemsQuery query)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var result = _tvSeriesManager.GetNextUp(
                new NextUpQuery
                {
                    Limit = query.Limit,
                    StartIndex = query.StartIndex,
                    // User cannot be null here as the caller has set it
                    UserId = query.User!.Id
                },
                new[] { parent },
                query.DtoOptions);

            return ToResult(query.StartIndex, result);
        }

        /// <summary>
        /// Returns the latest items of [itemType] meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <param name="itemType">The item type.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetLatest(BaseItem parent, InternalItemsQuery query, BaseItemKind itemType)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    // User cannot be null here as the caller has set it
                    UserId = query.User!.Id,
                    Limit = query.Limit ?? 50,
                    IncludeItemTypes = new[] { itemType },
                    ParentId = parent?.Id ?? Guid.Empty,
                    GroupItems = true
                },
                query.DtoOptions).Select(i => i.Item1 ?? i.Item2.FirstOrDefault()).Where(i => i != null).ToArray();

            return ToResult(query.StartIndex, items);
        }

        /// <summary>
        /// Returns music artist items that meet the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtistItems(BaseItem item, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ArtistIds = new[] { item.Id },
                IncludeItemTypes = new[] { BaseItemKind.MusicAlbum },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions(),
                OrderBy = GetOrderBy(sort, false)
            };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(startIndex, result);
        }

        /// <summary>
        /// Returns the genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenreItems(BaseItem item, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                GenreIds = new[] { item.Id },
                IncludeItemTypes = new[]
                {
                    BaseItemKind.Movie,
                    BaseItemKind.Series
                },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions(),
                OrderBy = GetOrderBy(sort, false)
            };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(startIndex, result);
        }

        /// <summary>
        /// Returns the music genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenreItems(BaseItem item, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                GenreIds = new[] { item.Id },
                IncludeItemTypes = new[] { BaseItemKind.MusicAlbum },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions(),
                OrderBy = GetOrderBy(sort, false)
            };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(startIndex, result);
        }

        /// <summary>
        /// Converts <see cref="IReadOnlyCollection{BaseItem}"/> into a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="result">An array of <see cref="BaseItem"/>.</param>
        /// <returns>A <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(int? startIndex, IReadOnlyCollection<BaseItem> result)
        {
            var serverItems = result
                .Select(i => new ServerItem(i, null))
                .ToArray();

            return new QueryResult<ServerItem>(
                startIndex,
                result.Count,
                serverItems);
        }

        /// <summary>
        /// Converts a <see cref="QueryResult{BaseItem}"/> to a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="startIndex">The index the result started at.</param>
        /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(int? startIndex, QueryResult<BaseItem> result)
        {
            var length = result.Items.Count;
            var serverItems = new ServerItem[length];
            for (var i = 0; i < length; i++)
            {
                serverItems[i] = new ServerItem(result.Items[i], null);
            }

            return new QueryResult<ServerItem>(
                startIndex,
                result.TotalRecordCount,
                serverItems);
        }

        /// <summary>
        /// Converts a query result to a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(int? startIndex, QueryResult<(BaseItem Item, ItemCounts ItemCounts)> result)
        {
            var length = result.Items.Count;
            var serverItems = new ServerItem[length];
            for (var i = 0; i < length; i++)
            {
                serverItems[i] = new ServerItem(result.Items[i].Item, null);
            }

            return new QueryResult<ServerItem>(
                startIndex,
                result.TotalRecordCount,
                serverItems);
        }

        /// <summary>
        /// Gets the sorting method on a query.
        /// </summary>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="isPreSorted">True if pre-sorted.</param>
        private static (string SortName, SortOrder SortOrder)[] GetOrderBy(SortCriteria sort, bool isPreSorted)
        {
            return isPreSorted ? Array.Empty<(string, SortOrder)>() : new[] { (ItemSortBy.SortName, sort.SortOrder) };
        }

        /// <summary>
        /// Retrieves the ServerItem id.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <returns>The <see cref="ServerItem"/>.</returns>
        private ServerItem GetItemFromObjectId(string id)
        {
            return DidlBuilder.IsIdRoot(id)
                 ? new ServerItem(_libraryManager.GetUserRootFolder(), null)
                 : ParseItemId(id);
        }

        /// <summary>
        /// Parses the item id into a <see cref="ServerItem"/>.
        /// </summary>
        /// <param name="id">The <see cref="string"/>.</param>
        /// <returns>The corresponding <see cref="ServerItem"/>.</returns>
        private ServerItem ParseItemId(string id)
        {
            StubType? stubType = null;

            // After using PlayTo, MediaMonkey sends a request to the server trying to get item info
            const string ParamsSrch = "Params=";
            var paramsIndex = id.IndexOf(ParamsSrch, StringComparison.OrdinalIgnoreCase);
            if (paramsIndex != -1)
            {
                id = id[(paramsIndex + ParamsSrch.Length)..];

                var parts = id.Split(';');
                id = parts[23];
            }

            var dividerIndex = id.IndexOf('_', StringComparison.Ordinal);
            if (dividerIndex != -1 && Enum.TryParse<StubType>(id.AsSpan(0, dividerIndex), true, out var parsedStubType))
            {
                id = id[(dividerIndex + 1)..];
                stubType = parsedStubType;
            }

            if (Guid.TryParse(id, out var itemId))
            {
                var item = _libraryManager.GetItemById(itemId);

                return new ServerItem(item, stubType);
            }

            Logger.LogError("Error parsing item Id: {Id}. Returning user root folder.", id);

            return new ServerItem(_libraryManager.GetUserRootFolder(), null);
        }
    }
}
