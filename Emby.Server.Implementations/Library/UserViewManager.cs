#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Library
{
    public class UserViewManager : IUserViewManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;

        private readonly IChannelManager _channelManager;
        private readonly ILiveTvManager _liveTvManager;
        private readonly IServerConfigurationManager _config;

        public UserViewManager(ILibraryManager libraryManager, ILocalizationManager localizationManager, IChannelManager channelManager, ILiveTvManager liveTvManager, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _channelManager = channelManager;
            _liveTvManager = liveTvManager;
            _config = config;
        }

        public Folder[] GetUserViews(UserViewQuery query)
        {
            var user = query.User;

            var folders = _libraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .OfType<Folder>()
                .ToList();

            var groupedFolders = new List<ICollectionFolder>();
            var list = new List<Folder>();

            foreach (var folder in folders)
            {
                var collectionFolder = folder as ICollectionFolder;
                var folderViewType = collectionFolder?.CollectionType;

                // Playlist library requires special handling because the folder only references user playlists
                if (folderViewType == CollectionType.playlists)
                {
                    var items = folder.GetItemList(new InternalItemsQuery(user)
                    {
                        ParentId = folder.ParentId
                    });

                    if (!items.Any(item => item.IsVisible(user)))
                    {
                        continue;
                    }
                }

                if (UserView.IsUserSpecific(folder))
                {
                    list.Add(_libraryManager.GetNamedView(user, folder.Name, folder.Id, folderViewType, null));
                    continue;
                }

                if (collectionFolder is not null && UserView.IsEligibleForGrouping(folder) && user.IsFolderGrouped(folder.Id))
                {
                    groupedFolders.Add(collectionFolder);
                    continue;
                }

                if (query.PresetViews.Contains(folderViewType))
                {
                    list.Add(GetUserView(folder, folderViewType, string.Empty));
                }
                else
                {
                    list.Add(folder);
                }
            }

            foreach (var viewType in new[] { CollectionType.movies, CollectionType.tvshows })
            {
                var parents = groupedFolders.Where(i => i.CollectionType == viewType || i.CollectionType is null)
                    .ToList();

                if (parents.Count > 0)
                {
                    var localizationKey = viewType == CollectionType.tvshows
                        ? "TvShows"
                        : "Movies";

                    list.Add(GetUserView(parents, viewType, localizationKey, string.Empty, user, query.PresetViews));
                }
            }

            if (_config.Configuration.EnableFolderView)
            {
                var name = _localizationManager.GetLocalizedString("Folders");
                list.Add(_libraryManager.GetNamedView(name, CollectionType.folders, string.Empty));
            }

            if (query.IncludeExternalContent)
            {
                var channelResult = _channelManager.GetChannelsInternalAsync(new ChannelQuery
                {
                    UserId = user.Id
                }).GetAwaiter().GetResult();

                var channels = channelResult.Items;

                list.AddRange(channels);

                if (_liveTvManager.GetEnabledUsers().Select(i => i.Id).Contains(user.Id))
                {
                    list.Add(_liveTvManager.GetInternalLiveTvFolder(CancellationToken.None));
                }
            }

            if (!query.IncludeHidden)
            {
                list = list.Where(i => !user.GetPreferenceValues<Guid>(PreferenceKind.MyMediaExcludes).Contains(i.Id)).ToList();
            }

            var sorted = _libraryManager.Sort(list, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending).ToList();
            var orders = user.GetPreferenceValues<Guid>(PreferenceKind.OrderedViews);

            return list
                .OrderBy(i =>
                {
                    var index = Array.IndexOf(orders, i.Id);
                    if (index == -1
                        && i is UserView view
                        && !view.DisplayParentId.IsEmpty())
                    {
                        index = Array.IndexOf(orders, view.DisplayParentId);
                    }

                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(sorted.IndexOf)
                .ThenBy(i => i.SortName)
                .ToArray();
        }

        public UserView GetUserSubViewWithName(string name, Guid parentId, CollectionType? type, string sortName)
        {
            var uniqueId = parentId + "subview" + type;

            return _libraryManager.GetNamedView(name, parentId, type, sortName, uniqueId);
        }

        public UserView GetUserSubView(Guid parentId, CollectionType? type, string localizationKey, string sortName)
        {
            var name = _localizationManager.GetLocalizedString(localizationKey);

            return GetUserSubViewWithName(name, parentId, type, sortName);
        }

        private Folder GetUserView(
            List<ICollectionFolder> parents,
            CollectionType? viewType,
            string localizationKey,
            string sortName,
            User user,
            CollectionType?[] presetViews)
        {
            if (parents.Count == 1 && parents.All(i => i.CollectionType == viewType))
            {
                if (!presetViews.Contains(viewType))
                {
                    return (Folder)parents[0];
                }

                return GetUserView((Folder)parents[0], viewType, string.Empty);
            }

            var name = _localizationManager.GetLocalizedString(localizationKey);
            return _libraryManager.GetNamedView(user, name, viewType, sortName);
        }

        public UserView GetUserView(Folder parent, CollectionType? viewType, string sortName)
        {
            return _libraryManager.GetShadowView(parent, viewType, sortName);
        }

        public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options)
        {
            var user = request.User;
            var limit = request.Limit ?? 10;

            // Handle Channel items separately
            if (!request.ParentId.IsEmpty())
            {
                var parentItem = _libraryManager.GetItemById(request.ParentId);
                if (parentItem is Channel)
                {
                    var channelItems = _channelManager.GetLatestChannelItemsInternal(
                        new InternalItemsQuery(user)
                        {
                            ChannelIds = new[] { request.ParentId },
                            IsPlayed = request.IsPlayed,
                            StartIndex = request.StartIndex,
                            Limit = request.Limit,
                            IncludeItemTypes = request.IncludeItemTypes,
                            EnableTotalRecordCount = false
                        },
                        CancellationToken.None).GetAwaiter().GetResult().Items;

                    return channelItems
                        .Select(item => new Tuple<BaseItem, List<BaseItem>>(null, new List<BaseItem> { item }))
                        .ToList();
                }
            }

            // Determine parents and collection type
            var (parents, collectionType) = GetParentsAndCollectionType(user, request);
            if (parents.Count == 0)
            {
                return new List<Tuple<BaseItem, List<BaseItem>>>();
            }

            // Build query with filters
            var query = BuildLatestItemsQuery(user, request, parents, limit, options);

            // Get grouped results from repository
            var grouped = _libraryManager.GetLatestItemsGrouped(query, parents, collectionType);

            // Convert to expected return type
            return grouped
                .Take(limit)
                .Select(g => new Tuple<BaseItem, List<BaseItem>>(g.Container, g.Items.ToList()))
                .ToList();
        }

        private (List<BaseItem> Parents, CollectionType? CollectionType) GetParentsAndCollectionType(User user, LatestItemsQuery request)
        {
            var parents = new List<BaseItem>();

            if (!request.ParentId.IsEmpty())
            {
                var parentItem = _libraryManager.GetItemById(request.ParentId);
                if (parentItem is Folder parent)
                {
                    parents.Add(parent);
                }
            }

            if (parents.Count == 0)
            {
                var excludedIds = user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes).ToHashSet();
                parents = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .Where(i => i is Folder && !excludedIds.Contains(i.Id))
                    .ToList();
            }

            var collectionType = parents
                .Select(parent => parent switch
                {
                    ICollectionFolder collectionFolder => collectionFolder.CollectionType,
                    UserView userView => userView.CollectionType,
                    _ => null
                })
                .FirstOrDefault(type => type is not null);

            return (parents, collectionType);
        }

        private InternalItemsQuery BuildLatestItemsQuery(User user, LatestItemsQuery request, List<BaseItem> parents, int limit, DtoOptions options)
        {
            var includeItemTypes = request.IncludeItemTypes;

            if (includeItemTypes.Length == 0)
            {
                var hasCollectionType = parents.OfType<UserView>().ToList();
                if (hasCollectionType.Count > 0)
                {
                    if (hasCollectionType.All(i => i.CollectionType == CollectionType.movies))
                    {
                        includeItemTypes = new[] { BaseItemKind.Movie };
                    }
                    else if (hasCollectionType.All(i => i.CollectionType == CollectionType.tvshows))
                    {
                        includeItemTypes = new[] { BaseItemKind.Episode };
                    }
                }
            }

            var isPlayed = request.IsPlayed;
            if (parents.OfType<ICollectionFolder>().Any(i => i.CollectionType == CollectionType.music))
            {
                isPlayed = null;
            }

            MediaType[] mediaTypes = [];
            if (includeItemTypes.Length == 0)
            {
                HashSet<MediaType> tmpMediaTypes = [];
                foreach (var parent in parents.OfType<ICollectionFolder>())
                {
                    switch (parent.CollectionType)
                    {
                        case CollectionType.books:
                            tmpMediaTypes.Add(MediaType.Book);
                            tmpMediaTypes.Add(MediaType.Audio);
                            break;
                        case CollectionType.music:
                            tmpMediaTypes.Add(MediaType.Audio);
                            break;
                        case CollectionType.photos:
                        case CollectionType.homevideos:
                            tmpMediaTypes.Add(MediaType.Photo);
                            tmpMediaTypes.Add(MediaType.Video);
                            break;
                        default:
                            tmpMediaTypes.Add(MediaType.Video);
                            break;
                    }
                }

                mediaTypes = tmpMediaTypes.ToArray();
            }

            var excludeItemTypes = includeItemTypes.Length == 0 && mediaTypes.Length == 0
                ? new[] { BaseItemKind.Person, BaseItemKind.Studio, BaseItemKind.Year, BaseItemKind.MusicGenre, BaseItemKind.Genre }
                : Array.Empty<BaseItemKind>();

            return new InternalItemsQuery(user)
            {
                IncludeItemTypes = includeItemTypes,
                OrderBy = new[]
                {
                    (ItemSortBy.DateCreated, SortOrder.Descending),
                    (ItemSortBy.SortName, SortOrder.Descending),
                    (ItemSortBy.ProductionYear, SortOrder.Descending)
                },
                IsFolder = includeItemTypes.Length == 0 ? false : null,
                ExcludeItemTypes = excludeItemTypes,
                IsVirtualItem = false,
                Limit = limit,
                IsPlayed = isPlayed,
                DtoOptions = options,
                MediaTypes = mediaTypes
            };
        }
    }
}
