using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using Emby.Dlna.Configuration;
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
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using Book = MediaBrowser.Controller.Entities.Book;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Genre = MediaBrowser.Controller.Entities.Genre;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Series = MediaBrowser.Controller.Entities.TV.Series;

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
        private readonly IServerConfigurationManager _config;
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
            _config = config;

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
        protected override void WriteResult(string methodName, IDictionary<string, string> methodParams, XmlWriter xmlWriter)
        {
            if (xmlWriter == null)
            {
                throw new ArgumentNullException(nameof(xmlWriter));
            }

            if (methodParams == null)
            {
                throw new ArgumentNullException(nameof(methodParams));
            }

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
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        private void HandleXSetBookmark(IDictionary<string, string> sparams)
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
            // TODO: clean this up
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

        /// <summary>
        /// Returns the value in the key of the dictionary, or defaultValue if it doesn't exist.
        /// </summary>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The defaultValue.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public static string GetValueOrDefault(IDictionary<string, string> sparams, string key, string defaultValue)
        {
            if (sparams != null && sparams.TryGetValue(key, out string val))
            {
                return val;
            }

            return defaultValue;
        }

        /// <summary>
        /// Builds the "Browse" xml response.
        /// </summary>
        /// <param name="xmlWriter">The <see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        /// <param name="deviceId">The device Id to use.</param>
        private void HandleBrowse(XmlWriter xmlWriter, IDictionary<string, string> sparams, string deviceId)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];
            var filter = new Filter(GetValueOrDefault(sparams, "Filter", "*"));
            var sortCriteria = new SortCriteria(GetValueOrDefault(sparams, "SortCriteria", string.Empty));

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

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    CloseOutput = false,
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment
                };

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
                }

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
        /// <param name="sparams">The <see cref="IDictionary"/>.</param>
        /// <param name="deviceId">The device id.</param>
        private void HandleXBrowseByLetter(XmlWriter xmlWriter, IDictionary<string, string> sparams, string deviceId)
        {
            // TODO: Implement this method
            HandleSearch(xmlWriter, sparams, deviceId);
        }

        /// <summary>
        /// Builds a response to the "Search" request.
        /// </summary>
        /// <param name="xmlWriter">The xmlWriter<see cref="XmlWriter"/>.</param>
        /// <param name="sparams">The sparams<see cref="IDictionary"/>.</param>
        /// <param name="deviceId">The deviceId<see cref="string"/>.</param>
        private void HandleSearch(XmlWriter xmlWriter, IDictionary<string, string> sparams, string deviceId)
        {
            var searchCriteria = new SearchCriteria(GetValueOrDefault(sparams, "SearchCriteria", string.Empty));
            var sortCriteria = new SortCriteria(GetValueOrDefault(sparams, "SortCriteria", string.Empty));
            var filter = new Filter(GetValueOrDefault(sparams, "Filter", "*"));

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

            using (StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8))
            {
                var settings = new XmlWriterSettings()
                {
                    Encoding = Encoding.UTF8,
                    CloseOutput = false,
                    OmitXmlDeclaration = true,
                    ConformanceLevel = ConformanceLevel.Fragment
                };

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

                    var dlnaOptions = _config.GetDlnaConfiguration();

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
                }

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

            var sortOrders = folder.IsPreSorted
                ? Array.Empty<(string, SortOrder)>()
                : new[] { (ItemSortBy.SortName, sort.SortOrder) };

            string[] mediaTypes = Array.Empty<string>();
            bool? isFolder = null;

            if (search.SearchType == SearchType.Audio)
            {
                mediaTypes = new[] { MediaType.Audio };
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Video)
            {
                mediaTypes = new[] { MediaType.Video };
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Image)
            {
                mediaTypes = new[] { MediaType.Photo };
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Playlist)
            {
                // items = items.OfType<Playlist>();
                isFolder = true;
            }
            else if (search.SearchType == SearchType.MusicAlbum)
            {
                // items = items.OfType<MusicAlbum>();
                isFolder = true;
            }

            return folder.GetItems(new InternalItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                OrderBy = sortOrders,
                User = user,
                Recursive = true,
                IsMissing = false,
                ExcludeItemTypes = new[] { nameof(Book) },
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
            if (item is MusicGenre)
            {
                return GetMusicGenreItems(item, Guid.Empty, user, sort, startIndex, limit);
            }

            if (item is MusicArtist)
            {
                return GetMusicArtistItems(item, Guid.Empty, user, sort, startIndex, limit);
            }

            if (item is Genre)
            {
                return GetGenreItems(item, Guid.Empty, user, sort, startIndex, limit);
            }

            if ((!stubType.HasValue || stubType.Value != StubType.Folder)
                && item is IHasCollectionType collectionFolder)
            {
                if (string.Equals(CollectionType.Music, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMusicFolders(item, user, stubType, sort, startIndex, limit);
                }
                else if (string.Equals(CollectionType.Movies, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMovieFolders(item, user, stubType, sort, startIndex, limit);
                }
                else if (string.Equals(CollectionType.TvShows, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetTvFolders(item, user, stubType, sort, startIndex, limit);
                }
                else if (string.Equals(CollectionType.Folders, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetFolders(user, startIndex, limit);
                }
                else if (string.Equals(CollectionType.LiveTv, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase))
                {
                    return GetLiveTvChannels(user, sort, startIndex, limit);
                }
            }

            if (stubType.HasValue)
            {
                if (stubType.Value != StubType.Folder)
                {
                    return ApplyPaging(new QueryResult<ServerItem>(), startIndex, limit);
                }
            }

            var folder = (Folder)item;

            var query = new InternalItemsQuery(user)
            {
                Limit = limit,
                StartIndex = startIndex,
                IsVirtualItem = false,
                ExcludeItemTypes = new[] { nameof(Book) },
                IsPlaceHolder = false,
                DtoOptions = GetDtoOptions()
            };

            SetSorting(query, sort, folder.IsPreSorted);

            var queryResult = folder.GetItems(query);

            return ToResult(queryResult);
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
            };
            query.IncludeItemTypes = new[] { nameof(LiveTvChannel) };

            SetSorting(query, sort, false);

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
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
                Limit = limit
            };
            SetSorting(query, sort, false);

            if (stubType.HasValue && stubType.Value == StubType.Latest)
            {
                return GetMusicLatest(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Playlists)
            {
                return GetMusicPlaylists(user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Albums)
            {
                return GetMusicAlbums(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Artists)
            {
                return GetMusicArtists(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.AlbumArtists)
            {
                return GetMusicAlbumArtists(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.FavoriteAlbums)
            {
                return GetFavoriteAlbums(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.FavoriteArtists)
            {
                return GetFavoriteArtists(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.FavoriteSongs)
            {
                return GetFavoriteSongs(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Songs)
            {
                return GetMusicSongs(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Genres)
            {
                return GetMusicGenres(item, user, query);
            }

            var list = new List<ServerItem>
            {
                new ServerItem(item)
                {
                    StubType = StubType.Latest
                },

                new ServerItem(item)
                {
                    StubType = StubType.Playlists
                },

                new ServerItem(item)
                {
                    StubType = StubType.Albums
                },

                new ServerItem(item)
                {
                    StubType = StubType.AlbumArtists
                },

                new ServerItem(item)
                {
                    StubType = StubType.Artists
                },

                new ServerItem(item)
                {
                    StubType = StubType.Songs
                },

                new ServerItem(item)
                {
                    StubType = StubType.Genres
                },

                new ServerItem(item)
                {
                    StubType = StubType.FavoriteArtists
                },

                new ServerItem(item)
                {
                    StubType = StubType.FavoriteAlbums
                },

                new ServerItem(item)
                {
                    StubType = StubType.FavoriteSongs
                }
            };

            return new QueryResult<ServerItem>
            {
                Items = list,
                TotalRecordCount = list.Count
            };
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
                Limit = limit
            };
            SetSorting(query, sort, false);

            if (stubType.HasValue && stubType.Value == StubType.ContinueWatching)
            {
                return GetMovieContinueWatching(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Latest)
            {
                return GetMovieLatest(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Movies)
            {
                return GetMovieMovies(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Collections)
            {
                return GetMovieCollections(user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Favorites)
            {
                return GetMovieFavorites(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Genres)
            {
                return GetGenres(item, user, query);
            }

            var array = new[]
            {
                new ServerItem(item)
                {
                    StubType = StubType.ContinueWatching
                },
                new ServerItem(item)
                {
                    StubType = StubType.Latest
                },
                new ServerItem(item)
                {
                    StubType = StubType.Movies
                },
                new ServerItem(item)
                {
                    StubType = StubType.Collections
                },
                new ServerItem(item)
                {
                    StubType = StubType.Favorites
                },
                new ServerItem(item)
                {
                    StubType = StubType.Genres
                }
            };

            return new QueryResult<ServerItem>
            {
                Items = array,
                TotalRecordCount = array.Length
            };
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
            var folders = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                .OrderBy(i => i.SortName)
                .Select(i => new ServerItem(i)
                {
                    StubType = StubType.Folder
                })
                .ToArray();

            return ApplyPaging(
                new QueryResult<ServerItem>
                {
                    Items = folders,
                    TotalRecordCount = folders.Length
                },
                startIndex,
                limit);
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
                Limit = limit
            };
            SetSorting(query, sort, false);

            if (stubType.HasValue && stubType.Value == StubType.ContinueWatching)
            {
                return GetMovieContinueWatching(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.NextUp)
            {
                return GetNextUp(item, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Latest)
            {
                return GetTvLatest(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Series)
            {
                return GetSeries(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.FavoriteSeries)
            {
                return GetFavoriteSeries(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.FavoriteEpisodes)
            {
                return GetFavoriteEpisodes(item, user, query);
            }

            if (stubType.HasValue && stubType.Value == StubType.Genres)
            {
                return GetGenres(item, user, query);
            }

            var list = new List<ServerItem>
            {
                new ServerItem(item)
                {
                    StubType = StubType.ContinueWatching
                },

                new ServerItem(item)
                {
                    StubType = StubType.NextUp
                },

                new ServerItem(item)
                {
                    StubType = StubType.Latest
                },

                new ServerItem(item)
                {
                    StubType = StubType.Series
                },

                new ServerItem(item)
                {
                    StubType = StubType.FavoriteSeries
                },

                new ServerItem(item)
                {
                    StubType = StubType.FavoriteEpisodes
                },

                new ServerItem(item)
                {
                    StubType = StubType.Genres
                }
            };

            return new QueryResult<ServerItem>
            {
                Items = list,
                TotalRecordCount = list.Count
            };
        }

        /// <summary>
        /// Returns the Movies that are part watched that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieContinueWatching(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.OrderBy = new[]
            {
                (ItemSortBy.DatePlayed, SortOrder.Descending),
                (ItemSortBy.SortName, SortOrder.Ascending)
            };

            query.IsResumable = true;
            query.Limit = 10;

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the series meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetSeries(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { nameof(Series) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the Movie folders meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieMovies(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { nameof(Movie) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the Movie collections meeting the criteria.
        /// </summary>
        /// <param name="user">The see cref="User"/>.</param>
        /// <param name="query">The see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieCollections(User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            // query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { nameof(BoxSet) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the Music albums meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicAlbums(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { nameof(MusicAlbum) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the Music songs meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicSongs(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);

            query.IncludeItemTypes = new[] { nameof(Audio) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the songs tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteSongs(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { nameof(Audio) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the series tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteSeries(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { nameof(Series) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the episodes tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteEpisodes(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { nameof(Episode) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the movies tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieFavorites(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { nameof(Movie) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// /// Returns the albums tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteAlbums(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.Recursive = true;
            query.Parent = parent;
            query.SetUser(user);
            query.IsFavorite = true;
            query.IncludeItemTypes = new[] { nameof(MusicAlbum) };

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the genres meeting the criteria.
        /// The GetGenres.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenres(BaseItem parent, User user, InternalItemsQuery query)
        {
            var genresResult = _libraryManager.GetGenres(new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = genresResult.TotalRecordCount,
                Items = genresResult.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result);
        }

        /// <summary>
        /// Returns the music genres meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenres(BaseItem parent, User user, InternalItemsQuery query)
        {
            var genresResult = _libraryManager.GetMusicGenres(new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = genresResult.TotalRecordCount,
                Items = genresResult.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result);
        }

        /// <summary>
        /// Returns the music albums by artist that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicAlbumArtists(BaseItem parent, User user, InternalItemsQuery query)
        {
            var artists = _libraryManager.GetAlbumArtists(new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result);
        }

        /// <summary>
        /// Returns the music artists meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtists(BaseItem parent, User user, InternalItemsQuery query)
        {
            var artists = _libraryManager.GetArtists(new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result);
        }

        /// <summary>
        /// Returns the artists tagged as favourite that meet the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetFavoriteArtists(BaseItem parent, User user, InternalItemsQuery query)
        {
            var artists = _libraryManager.GetArtists(new InternalItemsQuery(user)
            {
                AncestorIds = new[] { parent.Id },
                StartIndex = query.StartIndex,
                Limit = query.Limit,
                IsFavorite = true
            });

            var result = new QueryResult<BaseItem>
            {
                TotalRecordCount = artists.TotalRecordCount,
                Items = artists.Items.Select(i => i.Item1).ToArray()
            };

            return ToResult(result);
        }

        /// <summary>
        /// Returns the music playlists meeting the criteria.
        /// </summary>
        /// <param name="user">The user<see cref="User"/>.</param>
        /// <param name="query">The query<see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicPlaylists(User user, InternalItemsQuery query)
        {
            query.Parent = null;
            query.IncludeItemTypes = new[] { nameof(Playlist) };
            query.SetUser(user);
            query.Recursive = true;

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the latest music meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicLatest(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = user.Id,
                    Limit = 50,
                    IncludeItemTypes = new[] { nameof(Audio) },
                    ParentId = parent?.Id ?? Guid.Empty,
                    GroupItems = true
                },
                query.DtoOptions).Select(i => i.Item1 ?? i.Item2.FirstOrDefault()).Where(i => i != null).ToArray();

            return ToResult(items);
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
                    UserId = query.User.Id
                },
                new[] { parent },
                query.DtoOptions);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the latest tv meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetTvLatest(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = user.Id,
                    Limit = 50,
                    IncludeItemTypes = new[] { nameof(Episode) },
                    ParentId = parent == null ? Guid.Empty : parent.Id,
                    GroupItems = false
                },
                query.DtoOptions).Select(i => i.Item1 ?? i.Item2.FirstOrDefault()).Where(i => i != null).ToArray();

            return ToResult(items);
        }

        /// <summary>
        /// Returns the latest movies meeting the criteria.
        /// </summary>
        /// <param name="parent">The <see cref="BaseItem"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMovieLatest(BaseItem parent, User user, InternalItemsQuery query)
        {
            query.OrderBy = Array.Empty<(string, SortOrder)>();

            var items = _userViewManager.GetLatestItems(
                new LatestItemsQuery
                {
                    UserId = user.Id,
                    Limit = 50,
                    IncludeItemTypes = new[] { nameof(Movie) },
                    ParentId = parent?.Id ?? Guid.Empty,
                    GroupItems = true
                },
                query.DtoOptions).Select(i => i.Item1 ?? i.Item2.FirstOrDefault()).Where(i => i != null).ToArray();

            return ToResult(items);
        }

        /// <summary>
        /// Returns music artist items that meet the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicArtistItems(BaseItem item, Guid parentId, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                ArtistIds = new[] { item.Id },
                IncludeItemTypes = new[] { nameof(MusicAlbum) },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions()
            };

            SetSorting(query, sort, false);

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetGenreItems(BaseItem item, Guid parentId, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                GenreIds = new[] { item.Id },
                IncludeItemTypes = new[]
                {
                    nameof(Movie),
                    nameof(Series)
                },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions()
            };

            SetSorting(query, sort, false);

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Returns the music genre items meeting the criteria.
        /// </summary>
        /// <param name="item">The <see cref="BaseItem"/>.</param>
        /// <param name="parentId">The <see cref="Guid"/>.</param>
        /// <param name="user">The <see cref="User"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private QueryResult<ServerItem> GetMusicGenreItems(BaseItem item, Guid parentId, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                ParentId = parentId,
                GenreIds = new[] { item.Id },
                IncludeItemTypes = new[] { nameof(MusicAlbum) },
                Limit = limit,
                StartIndex = startIndex,
                DtoOptions = GetDtoOptions()
            };

            SetSorting(query, sort, false);

            var result = _libraryManager.GetItemsResult(query);

            return ToResult(result);
        }

        /// <summary>
        /// Converts a <see cref="BaseItem"/> array into a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="result">An array of <see cref="BaseItem"/>.</param>
        /// <returns>A <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(BaseItem[] result)
        {
            var serverItems = result
                .Select(i => new ServerItem(i))
                .ToArray();

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = result.Length,
                Items = serverItems
            };
        }

        /// <summary>
        /// Converts a <see cref="QueryResult{BaseItem}"/> to a <see cref="QueryResult{ServerItem}"/>.
        /// </summary>
        /// <param name="result">A <see cref="QueryResult{BaseItem}"/>.</param>
        /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ToResult(QueryResult<BaseItem> result)
        {
            var serverItems = result
                .Items
                .Select(i => new ServerItem(i))
                .ToArray();

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = result.TotalRecordCount,
                Items = serverItems
            };
        }

        /// <summary>
        /// Sets the sorting method on a query.
        /// </summary>
        /// <param name="query">The <see cref="InternalItemsQuery"/>.</param>
        /// <param name="sort">The <see cref="SortCriteria"/>.</param>
        /// <param name="isPreSorted">True if pre-sorted.</param>
        private static void SetSorting(InternalItemsQuery query, SortCriteria sort, bool isPreSorted)
        {
            if (isPreSorted)
            {
                query.OrderBy = Array.Empty<(string, SortOrder)>();
            }
            else
            {
                query.OrderBy = new[] { (ItemSortBy.SortName, sort.SortOrder) };
            }
        }

        /// <summary>
        /// Apply paging to a query.
        /// </summary>
        /// <param name="result">The <see cref="QueryResult{ServerItem}"/>.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="limit">The maximum number to return.</param>
        /// <returns>A <see cref="QueryResult{ServerItem}"/>.</returns>
        private static QueryResult<ServerItem> ApplyPaging(QueryResult<ServerItem> result, int? startIndex, int? limit)
        {
            result.Items = result.Items.Skip(startIndex ?? 0).Take(limit ?? int.MaxValue).ToArray();

            return result;
        }

        /// <summary>
        /// Retrieves the ServerItem id.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <returns>The <see cref="ServerItem"/>.</returns>
        private ServerItem GetItemFromObjectId(string id)
        {
            return DidlBuilder.IsIdRoot(id)
                 ? new ServerItem(_libraryManager.GetUserRootFolder())
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
                id = id.Substring(paramsIndex + ParamsSrch.Length);

                var parts = id.Split(';');
                id = parts[23];
            }

            var enumNames = Enum.GetNames(typeof(StubType));
            foreach (var name in enumNames)
            {
                if (id.StartsWith(name + "_", StringComparison.OrdinalIgnoreCase))
                {
                    stubType = Enum.Parse<StubType>(name, true);
                    id = id.Split('_', 2)[1];

                    break;
                }
            }

            if (Guid.TryParse(id, out var itemId))
            {
                var item = _libraryManager.GetItemById(itemId);

                return new ServerItem(item)
                {
                    StubType = stubType
                };
            }

            Logger.LogError("Error parsing item Id: {Id}. Returning user root folder.", id);

            return new ServerItem(_libraryManager.GetUserRootFolder());
        }
    }
}
