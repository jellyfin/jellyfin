using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Dlna.ContentDirectory
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;
        private readonly User _user;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private readonly int _systemUpdateId;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly DidlBuilder _didlBuilder;

        private readonly DeviceProfile _profile;
        private readonly IUserViewManager _userViewManager;
        private readonly IChannelManager _channelManager;

        public ControlHandler(ILogger logger, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, IImageProcessor imageProcessor, IUserDataManager userDataManager, User user, int systemUpdateId, IServerConfigurationManager config, IUserViewManager userViewManager, IChannelManager channelManager)
            : base(config, logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
            _userViewManager = userViewManager;
            _channelManager = channelManager;
            _profile = profile;

            _didlBuilder = new DidlBuilder(profile, user, imageProcessor, serverAddress);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams)
        {
            var deviceId = "test";

            var user = _user;

            if (string.Equals(methodName, "GetSearchCapabilities", StringComparison.OrdinalIgnoreCase))
                return HandleGetSearchCapabilities();

            if (string.Equals(methodName, "GetSortCapabilities", StringComparison.OrdinalIgnoreCase))
                return HandleGetSortCapabilities();

            if (string.Equals(methodName, "GetSystemUpdateID", StringComparison.OrdinalIgnoreCase))
                return HandleGetSystemUpdateID();

            if (string.Equals(methodName, "Browse", StringComparison.OrdinalIgnoreCase))
                return HandleBrowse(methodParams, user, deviceId).Result;

            if (string.Equals(methodName, "X_GetFeatureList", StringComparison.OrdinalIgnoreCase))
                return HandleXGetFeatureList();

            if (string.Equals(methodName, "X_SetBookmark", StringComparison.OrdinalIgnoreCase))
                return HandleXSetBookmark(methodParams, user);

            if (string.Equals(methodName, "Search", StringComparison.OrdinalIgnoreCase))
                return HandleSearch(methodParams, user, deviceId).Result;

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
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
            return new Headers(true) { { "SearchCaps", "res@resolution,res@size,res@duration,dc:title,dc:creator,upnp:actor,upnp:artist,upnp:genre,upnp:album,dc:date,upnp:class,@id,@refID,@protocolInfo,upnp:author,dc:description,pv:avKeywords" } };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSortCapabilities()
        {
            return new Headers(true) { { "SortCaps", "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating" } };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSystemUpdateID()
        {
            var headers = new Headers(true);
            headers.Add("Id", _systemUpdateId.ToString(_usCulture));
            return headers;
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXGetFeatureList()
        {
            return new Headers(true) { { "FeatureList", GetFeatureListXml() } };
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

        private async Task<IEnumerable<KeyValuePair<string, string>>> HandleBrowse(Headers sparams, User user, string deviceId)
        {
            var id = sparams["ObjectID"];
            var flag = sparams["BrowseFlag"];
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", ""));

            var provided = 0;

            int? requested = 0;
            int? start = 0;

            int requestedVal;
            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requestedVal) && requestedVal > 0)
            {
                requested = requestedVal;
            }

            int startVal;
            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out startVal) && startVal > 0)
            {
                start = startVal;
            }

            //var root = GetItem(id) as IMediaFolder;
            var result = new XmlDocument();

            var didl = result.CreateElement(string.Empty, "DIDL-Lite", NS_DIDL);
            didl.SetAttribute("xmlns:dc", NS_DC);
            didl.SetAttribute("xmlns:dlna", NS_DLNA);
            didl.SetAttribute("xmlns:upnp", NS_UPNP);
            //didl.SetAttribute("xmlns:sec", NS_SEC);
            result.AppendChild(didl);

            var item = GetItemFromObjectId(id, user);

            var totalCount = 0;

            if (string.Equals(flag, "BrowseMetadata"))
            {


                var folder = item as Folder;

                if (folder == null)
                {
                    result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(result, item, deviceId, filter));
                }
                else
                {


                    var childrenResult = (await GetChildrenSorted(folder, user, sortCriteria, start, requested).ConfigureAwait(false));
                    totalCount = childrenResult.TotalRecordCount;

                    result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, folder, totalCount, filter, id));
                }
                provided++;
            }
            else
            {
                var folder = (Folder)item;

                var childrenResult = (await GetChildrenSorted(folder, user, sortCriteria, start, requested).ConfigureAwait(false));
                totalCount = childrenResult.TotalRecordCount;

                provided = childrenResult.Items.Length;

                foreach (var i in childrenResult.Items)
                {
                    if (i.IsFolder)
                    {
                        var f = (Folder)i;
                        var childCount = (await GetChildrenSorted(f, user, sortCriteria, null, 0).ConfigureAwait(false))
                            .TotalRecordCount;

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

        private async Task<IEnumerable<KeyValuePair<string, string>>> HandleSearch(Headers sparams, User user, string deviceId)
        {
            var searchCriteria = new SearchCriteria(sparams.GetValueOrDefault("SearchCriteria", ""));
            var sortCriteria = new SortCriteria(sparams.GetValueOrDefault("SortCriteria", ""));
            var filter = new Filter(sparams.GetValueOrDefault("Filter", "*"));

            // sort example: dc:title, dc:date

            int? requested = 0;
            int? start = 0;

            int requestedVal;
            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requestedVal) && requestedVal > 0)
            {
                requested = requestedVal;
            }

            int startVal;
            if (sparams.ContainsKey("StartingIndex") && int.TryParse(sparams["StartingIndex"], out startVal) && startVal > 0)
            {
                start = startVal;
            }

            //var root = GetItem(id) as IMediaFolder;
            var result = new XmlDocument();

            var didl = result.CreateElement(string.Empty, "DIDL-Lite", NS_DIDL);
            didl.SetAttribute("xmlns:dc", NS_DC);
            didl.SetAttribute("xmlns:dlna", NS_DLNA);
            didl.SetAttribute("xmlns:upnp", NS_UPNP);

            foreach (var att in _profile.XmlRootAttributes)
            {
                didl.SetAttribute(att.Name, att.Value);
            }

            result.AppendChild(didl);

            var folder = (Folder)GetItemFromObjectId(sparams["ContainerID"], user);

            var childrenResult = (await GetChildrenSorted(folder, user, searchCriteria, sortCriteria, start, requested).ConfigureAwait(false));

            var totalCount = childrenResult.TotalRecordCount;

            var provided = childrenResult.Items.Length;

            foreach (var i in childrenResult.Items)
            {
                if (i.IsFolder)
                {
                    var f = (Folder)i;
                    var childCount = (await GetChildrenSorted(f, user, searchCriteria, sortCriteria, null, 0).ConfigureAwait(false))
                        .TotalRecordCount;

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

        private async Task<QueryResult<BaseItem>> GetChildrenSorted(Folder folder, User user, SearchCriteria search, SortCriteria sort, int? startIndex, int? limit)
        {
            // TODO: Make a recursive version of GetChildrenSorted (although sorting isn't needed)
            var result = folder.GetRecursiveChildren(user, true);

            var items = FilterUnsupportedContent(result);

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
                items = items.OfType<Playlist>();
            }
            else if (search.SearchType == SearchType.MusicAlbum)
            {
                items = items.OfType<MusicAlbum>();
            }

            items = SortItems(items, user, sort);

            return ToResult(items, startIndex, limit);
        }

        private async Task<QueryResult<BaseItem>> GetChildrenSorted(Folder folder, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            if (folder is UserRootFolder)
            {
                var result = await _userViewManager.GetUserViews(new UserViewQuery
                {
                    UserId = user.Id.ToString("N")

                }, CancellationToken.None).ConfigureAwait(false);

                return ToResult(result, startIndex, limit);
            }

            var view = folder as UserView;

            if (view != null)
            {
                var result = await GetUserViewChildren(view, user, sort).ConfigureAwait(false);

                return ToResult(result, startIndex, limit);
            }

            var channel = folder as Channel;

            if (channel != null)
            {
                try
                {
                    // Don't blow up here because it could cause parent screens with other content to fail
                    return await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
                    {
                        ChannelId = channel.Id.ToString("N"),
                        Limit = limit,
                        StartIndex = startIndex,
                        UserId = user.Id.ToString("N")

                    }, CancellationToken.None);
                }
                catch
                {
                    // Already logged at lower levels
                }
            }

            var channelFolderItem = folder as ChannelFolderItem;

            if (channelFolderItem != null)
            {
                try
                {
                    // Don't blow up here because it could cause parent screens with other content to fail
                    return await _channelManager.GetChannelItemsInternal(new ChannelItemQuery
                    {
                        ChannelId = channelFolderItem.ChannelId,
                        FolderId = channelFolderItem.Id.ToString("N"),
                        Limit = limit,
                        StartIndex = startIndex,
                        UserId = user.Id.ToString("N")

                    }, CancellationToken.None);
                }
                catch
                {
                    // Already logged at lower levels
                }
            }

            return ToResult(GetPlainFolderChildrenSorted(folder, user, sort), startIndex, limit);
        }

        private QueryResult<BaseItem> ToResult(IEnumerable<BaseItem> items, int? startIndex, int? limit)
        {
            var list = items.ToArray();
            var totalCount = list.Length;

            if (startIndex.HasValue)
            {
                list = list.Skip(startIndex.Value).ToArray();
            }

            if (limit.HasValue)
            {
                list = list.Take(limit.Value).ToArray();
            }

            return new QueryResult<BaseItem>
            {
                Items = list,
                TotalRecordCount = totalCount
            };
        }

        private async Task<IEnumerable<BaseItem>> GetUserViewChildren(UserView folder, User user, SortCriteria sort)
        {
            if (string.Equals(folder.ViewType, CollectionType.Channels, StringComparison.OrdinalIgnoreCase))
            {
                var result = await _channelManager.GetChannelsInternal(new ChannelQuery()
                {
                    UserId = user.Id.ToString("N")

                }, CancellationToken.None).ConfigureAwait(false);

                return result.Items;
            }
            if (string.Equals(folder.ViewType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true).OfType<Series>(), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetRecursiveChildren(user, true).OfType<Movie>(), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.Music, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true).OfType<MusicArtist>(), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.Folders, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.LiveTvRecordingGroups, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true), user, sort);
            }
            if (string.Equals(folder.ViewType, CollectionType.LiveTvChannels, StringComparison.OrdinalIgnoreCase))
            {
                return SortItems(folder.GetChildren(user, true), user, sort);
            }

            return GetPlainFolderChildrenSorted(folder, user, sort);
        }

        private IEnumerable<BaseItem> GetPlainFolderChildrenSorted(Folder folder, User user, SortCriteria sort)
        {
            var items = folder.GetChildren(user, true);

            items = FilterUnsupportedContent(items);

            if (folder.IsPreSorted)
            {
                return items;
            }

            return SortItems(items, user, sort);
        }

        private IEnumerable<BaseItem> SortItems(IEnumerable<BaseItem> items, User user, SortCriteria sort)
        {
            return _libraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, sort.SortOrder);
        }

        private IEnumerable<BaseItem> FilterUnsupportedContent(IEnumerable<BaseItem> items)
        {
            return items.Where(i =>
            {
                // Unplayable
                if (i.LocationType == LocationType.Virtual && !i.IsFolder)
                {
                    return false;
                }

                // Unplayable
                var supportsPlaceHolder = i as ISupportsPlaceHolders;
                if (supportsPlaceHolder != null && supportsPlaceHolder.IsPlaceHolder)
                {
                    return false;
                }

                if (i is Game || i is Book)
                {
                    return false;
                }

                return true;
            });
        }

        private BaseItem GetItemFromObjectId(string id, User user)
        {
            return DidlBuilder.IsIdRoot(id)

                 ? user.RootFolder
                 : ParseItemId(id, user);
        }

        private BaseItem ParseItemId(string id, User user)
        {
            Guid itemId;

            if (Guid.TryParse(id, out itemId))
            {
                return _libraryManager.GetItemById(itemId);
            }

            Logger.Error("Error parsing item Id: {0}. Returning user root folder.", id);

            return user.RootFolder;
        }
    }
}
