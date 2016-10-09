using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Configuration;
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
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.MediaEncoding;

namespace MediaBrowser.Dlna.ContentDirectory
{
    public class ControlHandler : BaseControlHandler
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IChannelManager _channelManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IServerConfigurationManager _config;
        private readonly User _user;
        private readonly IUserViewManager _userViewManager;
        private readonly IMediaEncoder _mediaEncoder;

        private const string NS_DC = "http://purl.org/dc/elements/1.1/";
        private const string NS_DIDL = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
        private const string NS_DLNA = "urn:schemas-dlna-org:metadata-1-0/";
        private const string NS_UPNP = "urn:schemas-upnp-org:metadata-1-0/upnp/";

        private readonly int _systemUpdateId;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly DidlBuilder _didlBuilder;

        private readonly DeviceProfile _profile;

        public ControlHandler(ILogger logger, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, string accessToken, IImageProcessor imageProcessor, IUserDataManager userDataManager, User user, int systemUpdateId, IServerConfigurationManager config, ILocalizationManager localization, IChannelManager channelManager, IMediaSourceManager mediaSourceManager, IUserViewManager userViewManager, IMediaEncoder mediaEncoder)
            : base(config, logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
            _channelManager = channelManager;
            _userViewManager = userViewManager;
            _mediaEncoder = mediaEncoder;
            _profile = profile;
            _config = config;

            _didlBuilder = new DidlBuilder(profile, user, imageProcessor, serverAddress, accessToken, userDataManager, localization, mediaSourceManager, Logger, libraryManager, _mediaEncoder);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetResult(string methodName, Headers methodParams)
        {
            var deviceId = "test";

            var user = _user;

            if (string.Equals(methodName, "GetSearchCapabilities", StringComparison.OrdinalIgnoreCase))
                return HandleGetSearchCapabilities();

            if (string.Equals(methodName, "GetSortCapabilities", StringComparison.OrdinalIgnoreCase))
                return HandleGetSortCapabilities();

            if (string.Equals(methodName, "GetSortExtensionCapabilities", StringComparison.OrdinalIgnoreCase))
                return HandleGetSortExtensionCapabilities();

            if (string.Equals(methodName, "GetSystemUpdateID", StringComparison.OrdinalIgnoreCase))
                return HandleGetSystemUpdateID();

            if (string.Equals(methodName, "Browse", StringComparison.OrdinalIgnoreCase))
                return HandleBrowse(methodParams, user, deviceId).Result;

            if (string.Equals(methodName, "X_GetFeatureList", StringComparison.OrdinalIgnoreCase))
                return HandleXGetFeatureList();

            if (string.Equals(methodName, "GetFeatureList", StringComparison.OrdinalIgnoreCase))
                return HandleGetFeatureList();

            if (string.Equals(methodName, "X_SetBookmark", StringComparison.OrdinalIgnoreCase))
                return HandleXSetBookmark(methodParams, user);

            if (string.Equals(methodName, "Search", StringComparison.OrdinalIgnoreCase))
                return HandleSearch(methodParams, user, deviceId).Result;

            throw new ResourceNotFoundException("Unexpected control request name: " + methodName);
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXSetBookmark(IDictionary<string, string> sparams, User user)
        {
            var id = sparams["ObjectID"];

            var serverItem = GetItemFromObjectId(id, user);

            var item = serverItem.Item;

            var newbookmark = int.Parse(sparams["PosSecond"], _usCulture);

            var userdata = _userDataManager.GetUserData(user, item);

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
            return new Headers(true)
            {
                { "SortCaps", "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating" }
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSortExtensionCapabilities()
        {
            return new Headers(true)
            {
                { "SortExtensionCaps", "res@duration,res@size,res@bitrate,dc:date,dc:title,dc:size,upnp:album,upnp:artist,upnp:albumArtist,upnp:episodeNumber,upnp:genre,upnp:originalTrackNumber,upnp:rating" }
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetSystemUpdateID()
        {
            var headers = new Headers(true);
            headers.Add("Id", _systemUpdateId.ToString(_usCulture));
            return headers;
        }

        private IEnumerable<KeyValuePair<string, string>> HandleGetFeatureList()
        {
            return new Headers(true)
            {
                { "FeatureList", GetFeatureListXml() }
            };
        }

        private IEnumerable<KeyValuePair<string, string>> HandleXGetFeatureList()
        {
            return new Headers(true)
            {
                { "FeatureList", GetFeatureListXml() }
            };
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

            // Default to null instead of 0
            // Upnp inspector sends 0 as requestedCount when it wants everything
            int? requestedCount = null;
            int? start = 0;

            int requestedVal;
            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
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

            var serverItem = GetItemFromObjectId(id, user);
            var item = serverItem.Item;

            int totalCount;

            if (string.Equals(flag, "BrowseMetadata"))
            {
                totalCount = 1;

                if (item.IsFolder || serverItem.StubType.HasValue)
                {
                    var childrenResult = (await GetUserItems(item, serverItem.StubType, user, sortCriteria, start, requestedCount).ConfigureAwait(false));

                    result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, item, serverItem.StubType, null, childrenResult.TotalRecordCount, filter, id));
                }
                else
                {
                    result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(_config.GetDlnaConfiguration(), result, item, null, null, deviceId, filter));
                }

                provided++;
            }
            else
            {
                var childrenResult = (await GetUserItems(item, serverItem.StubType, user, sortCriteria, start, requestedCount).ConfigureAwait(false));
                totalCount = childrenResult.TotalRecordCount;

                provided = childrenResult.Items.Length;

                foreach (var i in childrenResult.Items)
                {
                    var childItem = i.Item;
                    var displayStubType = i.StubType;

                    if (childItem.IsFolder || displayStubType.HasValue)
                    {
                        var childCount = (await GetUserItems(childItem, displayStubType, user, sortCriteria, null, 0).ConfigureAwait(false))
                            .TotalRecordCount;

                        result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, childItem, displayStubType, item, childCount, filter));
                    }
                    else
                    {
                        result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(_config.GetDlnaConfiguration(), result, childItem, item, serverItem.StubType, deviceId, filter));
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

            // Default to null instead of 0
            // Upnp inspector sends 0 as requestedCount when it wants everything
            int? requestedCount = null;
            int? start = 0;

            int requestedVal;
            if (sparams.ContainsKey("RequestedCount") && int.TryParse(sparams["RequestedCount"], out requestedVal) && requestedVal > 0)
            {
                requestedCount = requestedVal;
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

            var serverItem = GetItemFromObjectId(sparams["ContainerID"], user);

            var item = serverItem.Item;

            var childrenResult = (await GetChildrenSorted(item, user, searchCriteria, sortCriteria, start, requestedCount).ConfigureAwait(false));

            var totalCount = childrenResult.TotalRecordCount;

            var provided = childrenResult.Items.Length;

            foreach (var i in childrenResult.Items)
            {
                if (i.IsFolder)
                {
                    var childCount = (await GetChildrenSorted(i, user, searchCriteria, sortCriteria, null, 0).ConfigureAwait(false))
                        .TotalRecordCount;

                    result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, i, null, item, childCount, filter));
                }
                else
                {
                    result.DocumentElement.AppendChild(_didlBuilder.GetItemElement(_config.GetDlnaConfiguration(), result, i, item, serverItem.StubType, deviceId, filter));
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

        private Task<QueryResult<BaseItem>> GetChildrenSorted(BaseItem item, User user, SearchCriteria search, SortCriteria sort, int? startIndex, int? limit)
        {
            var folder = (Folder)item;

            var sortOrders = new List<string>();
            if (!folder.IsPreSorted)
            {
                sortOrders.Add(ItemSortBy.SortName);
            }

            var mediaTypes = new List<string>();
            bool? isFolder = null;

            if (search.SearchType == SearchType.Audio)
            {
                mediaTypes.Add(MediaType.Audio);
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Video)
            {
                mediaTypes.Add(MediaType.Video);
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Image)
            {
                mediaTypes.Add(MediaType.Photo);
                isFolder = false;
            }
            else if (search.SearchType == SearchType.Playlist)
            {
                //items = items.OfType<Playlist>();
                isFolder = true;
            }
            else if (search.SearchType == SearchType.MusicAlbum)
            {
                //items = items.OfType<MusicAlbum>();
                isFolder = true;
            }

            return folder.GetItems(new InternalItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                SortBy = sortOrders.ToArray(),
                SortOrder = sort.SortOrder,
                User = user,
                Recursive = true,
                IsMissing = false,
                ExcludeItemTypes = new[] { typeof(Game).Name, typeof(Book).Name },
                IsFolder = isFolder,
                MediaTypes = mediaTypes.ToArray()
            });
        }

        private async Task<QueryResult<ServerItem>> GetUserItems(BaseItem item, StubType? stubType, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            if (stubType.HasValue)
            {
                if (stubType.Value == StubType.People)
                {
                    var items = _libraryManager.GetPeopleItems(new InternalPeopleQuery
                    {
                        ItemId = item.Id

                    }).ToArray();

                    var result = new QueryResult<ServerItem>
                    {
                        Items = items.Select(i => new ServerItem { Item = i, StubType = StubType.Folder }).ToArray(),
                        TotalRecordCount = items.Length
                    };

                    return ApplyPaging(result, startIndex, limit);
                }

                var person = item as Person;
                if (person != null)
                {
                    return GetItemsFromPerson(person, user, startIndex, limit);
                }

                return ApplyPaging(new QueryResult<ServerItem>(), startIndex, limit);
            }

            var folder = (Folder)item;

            var sortOrders = new List<string>();
            if (!folder.IsPreSorted)
            {
                sortOrders.Add(ItemSortBy.SortName);
            }

            var queryResult = await folder.GetItems(new InternalItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                SortBy = sortOrders.ToArray(),
                SortOrder = sort.SortOrder,
                User = user,
                IsMissing = false,
                PresetViews = new[] { CollectionType.Movies, CollectionType.TvShows, CollectionType.Music },
                ExcludeItemTypes = new[] { typeof(Game).Name, typeof(Book).Name },
                IsPlaceHolder = false

            }).ConfigureAwait(false);

            var serverItems = queryResult
                .Items
                .Select(i => new ServerItem
                {
                    Item = i
                })
                .ToArray();

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = queryResult.TotalRecordCount,
                Items = serverItems
            };
        }

        private QueryResult<ServerItem> GetItemsFromPerson(Person person, User user, int? startIndex, int? limit)
        {
            var itemsResult = _libraryManager.GetItemsResult(new InternalItemsQuery(user)
            {
                Person = person.Name,
                IncludeItemTypes = new[] { typeof(Movie).Name, typeof(Series).Name, typeof(Trailer).Name },
                SortBy = new[] { ItemSortBy.SortName },
                Limit = limit,
                StartIndex = startIndex

            });

            var serverItems = itemsResult.Items.Select(i => new ServerItem
            {
                Item = i,
                StubType = null
            })
            .ToArray();

            return new QueryResult<ServerItem>
            {
                TotalRecordCount = itemsResult.TotalRecordCount,
                Items = serverItems
            };
        }

        private QueryResult<ServerItem> ApplyPaging(QueryResult<ServerItem> result, int? startIndex, int? limit)
        {
            result.Items = result.Items.Skip(startIndex ?? 0).Take(limit ?? int.MaxValue).ToArray();

            return result;
        }

        private bool EnablePeopleDisplay(BaseItem item)
        {
            if (_libraryManager.GetPeopleNames(new InternalPeopleQuery
            {
                ItemId = item.Id

            }).Count > 0)
            {
                return item is Movie;
            }

            return false;
        }

        private ServerItem GetItemFromObjectId(string id, User user)
        {
            return DidlBuilder.IsIdRoot(id)

                 ? new ServerItem { Item = user.RootFolder }
                 : ParseItemId(id, user);
        }

        private ServerItem ParseItemId(string id, User user)
        {
            Guid itemId;
            StubType? stubType = null;

            // After using PlayTo, MediaMonkey sends a request to the server trying to get item info
            const string paramsSrch = "Params=";
            var paramsIndex = id.IndexOf(paramsSrch, StringComparison.OrdinalIgnoreCase);
            if (paramsIndex != -1)
            {
                id = id.Substring(paramsIndex + paramsSrch.Length);

                var parts = id.Split(';');
                id = parts[23];
            }

            if (id.StartsWith("folder_", StringComparison.OrdinalIgnoreCase))
            {
                stubType = StubType.Folder;
                id = id.Split(new[] { '_' }, 2)[1];
            }
            else if (id.StartsWith("people_", StringComparison.OrdinalIgnoreCase))
            {
                stubType = StubType.People;
                id = id.Split(new[] { '_' }, 2)[1];
            }

            if (Guid.TryParse(id, out itemId))
            {
                var item = _libraryManager.GetItemById(itemId);

                return new ServerItem
                {
                    Item = item,
                    StubType = stubType
                };
            }

            Logger.Error("Error parsing item Id: {0}. Returning user root folder.", id);

            return new ServerItem { Item = user.RootFolder };
        }
    }

    internal class ServerItem
    {
        public BaseItem Item { get; set; }
        public StubType? StubType { get; set; }
    }

    public enum StubType
    {
        Folder = 0,
        People = 1
    }
}
