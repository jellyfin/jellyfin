using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Dlna.Didl;
using MediaBrowser.Dlna.Server;
using MediaBrowser.Dlna.Service;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public ControlHandler(ILogger logger, ILibraryManager libraryManager, DeviceProfile profile, string serverAddress, IImageProcessor imageProcessor, IUserDataManager userDataManager, User user, int systemUpdateId, IServerConfigurationManager config)
            : base(config, logger)
        {
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
            _user = user;
            _systemUpdateId = systemUpdateId;
            _profile = profile;

            _didlBuilder = new DidlBuilder(profile, user, imageProcessor, serverAddress, userDataManager);
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


                    var childrenResult = (await GetUserItems(folder, user, sortCriteria, start, requested).ConfigureAwait(false));
                    totalCount = childrenResult.TotalRecordCount;

                    result.DocumentElement.AppendChild(_didlBuilder.GetFolderElement(result, folder, totalCount, filter, id));
                }
                provided++;
            }
            else
            {
                var folder = (Folder)item;

                var childrenResult = (await GetUserItems(folder, user, sortCriteria, start, requested).ConfigureAwait(false));
                totalCount = childrenResult.TotalRecordCount;

                provided = childrenResult.Items.Length;

                foreach (var i in childrenResult.Items)
                {
                    if (i.IsFolder)
                    {
                        var f = (Folder)i;
                        var childCount = (await GetUserItems(f, user, sortCriteria, null, 0).ConfigureAwait(false))
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
            
            return await folder.GetUserItems(new UserItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                SortBy = sortOrders.ToArray(),
                SortOrder = sort.SortOrder,
                User = user,
                Recursive = true,
                Filter = FilterUnsupportedContent,
                IsFolder = isFolder,
                MediaTypes = mediaTypes.ToArray()

            }).ConfigureAwait(false);
        }

        private async Task<QueryResult<BaseItem>> GetUserItems(Folder folder, User user, SortCriteria sort, int? startIndex, int? limit)
        {
            var sortOrders = new List<string>();
            if (!folder.IsPreSorted)
            {
                sortOrders.Add(ItemSortBy.SortName);
            }

            return await folder.GetUserItems(new UserItemsQuery
            {
                Limit = limit,
                StartIndex = startIndex,
                SortBy = sortOrders.ToArray(),
                SortOrder = sort.SortOrder,
                User = user,
                Filter = FilterUnsupportedContent

            }).ConfigureAwait(false);
        }

        private bool FilterUnsupportedContent(BaseItem i, User user)
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
                //return false;
            }

            return true;
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
