#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Library
{
    public class UserViewManager : IUserViewManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IUserManager _userManager;

        private readonly IChannelManager _channelManager;
        private readonly ILiveTvManager _liveTvManager;
        private readonly IServerConfigurationManager _config;

        public UserViewManager(ILibraryManager libraryManager, ILocalizationManager localizationManager, IUserManager userManager, IChannelManager channelManager, ILiveTvManager liveTvManager, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _userManager = userManager;
            _channelManager = channelManager;
            _liveTvManager = liveTvManager;
            _config = config;
        }

        public Folder[] GetUserViews(UserViewQuery query)
        {
            var user = _userManager.GetUserById(query.UserId);

            if (user == null)
            {
                throw new ArgumentException("User Id specified in the query does not exist.", nameof(query));
            }

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

                if (UserView.IsUserSpecific(folder))
                {
                    list.Add(_libraryManager.GetNamedView(user, folder.Name, folder.Id, folderViewType, null));
                    continue;
                }

                if (collectionFolder != null && UserView.IsEligibleForGrouping(folder) && user.IsFolderGrouped(folder.Id))
                {
                    groupedFolders.Add(collectionFolder);
                    continue;
                }

                if (query.PresetViews.Contains(folderViewType ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(GetUserView(folder, folderViewType, string.Empty));
                }
                else
                {
                    list.Add(folder);
                }
            }

            foreach (var viewType in new[] { CollectionType.Movies, CollectionType.TvShows })
            {
                var parents = groupedFolders.Where(i => string.Equals(i.CollectionType, viewType, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(i.CollectionType))
                    .ToList();

                if (parents.Count > 0)
                {
                    var localizationKey = string.Equals(viewType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) ?
                        "TvShows" :
                        "Movies";

                    list.Add(GetUserView(parents, viewType, localizationKey, string.Empty, user, query.PresetViews));
                }
            }

            if (_config.Configuration.EnableFolderView)
            {
                var name = _localizationManager.GetLocalizedString("Folders");
                list.Add(_libraryManager.GetNamedView(name, CollectionType.Folders, string.Empty));
            }

            if (query.IncludeExternalContent)
            {
                var channelResult = _channelManager.GetChannelsInternal(new ChannelQuery
                {
                    UserId = query.UserId
                });

                var channels = channelResult.Items;

                list.AddRange(channels);

                if (_liveTvManager.GetEnabledUsers().Select(i => i.Id).Contains(query.UserId))
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
                        && !view.DisplayParentId.Equals(default))
                    {
                        index = Array.IndexOf(orders, view.DisplayParentId);
                    }

                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(sorted.IndexOf)
                .ThenBy(i => i.SortName)
                .ToArray();
        }

        public UserView GetUserSubViewWithName(string name, Guid parentId, string type, string sortName)
        {
            var uniqueId = parentId + "subview" + type;

            return _libraryManager.GetNamedView(name, parentId, type, sortName, uniqueId);
        }

        public UserView GetUserSubView(Guid parentId, string type, string localizationKey, string sortName)
        {
            var name = _localizationManager.GetLocalizedString(localizationKey);

            return GetUserSubViewWithName(name, parentId, type, sortName);
        }

        private Folder GetUserView(
            List<ICollectionFolder> parents,
            string viewType,
            string localizationKey,
            string sortName,
            User user,
            string[] presetViews)
        {
            if (parents.Count == 1 && parents.All(i => string.Equals(i.CollectionType, viewType, StringComparison.OrdinalIgnoreCase)))
            {
                if (!presetViews.Contains(viewType, StringComparison.OrdinalIgnoreCase))
                {
                    return (Folder)parents[0];
                }

                return GetUserView((Folder)parents[0], viewType, string.Empty);
            }

            var name = _localizationManager.GetLocalizedString(localizationKey);
            return _libraryManager.GetNamedView(user, name, viewType, sortName);
        }

        public UserView GetUserView(Folder parent, string viewType, string sortName)
        {
            return _libraryManager.GetShadowView(parent, viewType, sortName);
        }

        public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request, DtoOptions options)
        {
            var user = _userManager.GetUserById(request.UserId);

            var libraryItems = GetItemsForLatestItems(user, request, options);

            var list = new List<Tuple<BaseItem, List<BaseItem>>>();

            foreach (var item in libraryItems)
            {
                // Only grab the index container for media
                var container = item.IsFolder || !request.GroupItems ? null : item.LatestItemsIndexContainer;

                if (container == null)
                {
                    list.Add(new Tuple<BaseItem, List<BaseItem>>(null, new List<BaseItem> { item }));
                }
                else
                {
                    var current = list.FirstOrDefault(i => i.Item1 != null && i.Item1.Id.Equals(container.Id));

                    if (current != null)
                    {
                        current.Item2.Add(item);
                    }
                    else
                    {
                        list.Add(new Tuple<BaseItem, List<BaseItem>>(container, new List<BaseItem> { item }));
                    }
                }

                if (list.Count >= request.Limit)
                {
                    break;
                }
            }

            return list;
        }

        private IReadOnlyList<BaseItem> GetItemsForLatestItems(User user, LatestItemsQuery request, DtoOptions options)
        {
            var parentId = request.ParentId;

            var includeItemTypes = request.IncludeItemTypes;
            var limit = request.Limit ?? 10;

            var parents = new List<BaseItem>();

            if (!parentId.Equals(default))
            {
                var parentItem = _libraryManager.GetItemById(parentId);
                if (parentItem is Channel)
                {
                    return _channelManager.GetLatestChannelItemsInternal(
                        new InternalItemsQuery(user)
                        {
                            ChannelIds = new[] { parentId },
                            IsPlayed = request.IsPlayed,
                            StartIndex = request.StartIndex,
                            Limit = request.Limit,
                            IncludeItemTypes = request.IncludeItemTypes,
                            EnableTotalRecordCount = false
                        },
                        CancellationToken.None).GetAwaiter().GetResult().Items;
                }

                if (parentItem is Folder parent)
                {
                    parents.Add(parent);
                }
            }

            var isPlayed = request.IsPlayed;

            if (parents.OfType<ICollectionFolder>().Any(i => string.Equals(i.CollectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase)))
            {
                isPlayed = null;
            }

            if (parents.Count == 0)
            {
                parents = _libraryManager.GetUserRootFolder().GetChildren(user, true)
                    .Where(i => i is Folder)
                    .Where(i => !user.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes)
                        .Contains(i.Id))
                    .ToList();
            }

            if (parents.Count == 0)
            {
                return new List<BaseItem>();
            }

            if (includeItemTypes.Length == 0)
            {
                // Handle situations with the grouping setting, e.g. movies showing up in tv, etc.
                // Thanks to mixed content libraries included in the UserView
                var hasCollectionType = parents.OfType<UserView>().ToArray();
                if (hasCollectionType.Length > 0)
                {
                    if (hasCollectionType.All(i => string.Equals(i.CollectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase)))
                    {
                        includeItemTypes = new[] { BaseItemKind.Movie };
                    }
                    else if (hasCollectionType.All(i => string.Equals(i.CollectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase)))
                    {
                        includeItemTypes = new[] { BaseItemKind.Episode };
                    }
                }
            }

            var mediaTypes = new List<string>();

            if (includeItemTypes.Length == 0)
            {
                foreach (var parent in parents.OfType<ICollectionFolder>())
                {
                    switch (parent.CollectionType)
                    {
                        case CollectionType.Books:
                            mediaTypes.Add(MediaType.Book);
                            mediaTypes.Add(MediaType.Audio);
                            break;
                        case CollectionType.Music:
                            mediaTypes.Add(MediaType.Audio);
                            break;
                        case CollectionType.Photos:
                            mediaTypes.Add(MediaType.Photo);
                            mediaTypes.Add(MediaType.Video);
                            break;
                        case CollectionType.HomeVideos:
                            mediaTypes.Add(MediaType.Photo);
                            mediaTypes.Add(MediaType.Video);
                            break;
                        default:
                            mediaTypes.Add(MediaType.Video);
                            break;
                    }
                }

                mediaTypes = mediaTypes.Distinct().ToList();
            }

            var excludeItemTypes = includeItemTypes.Length == 0 && mediaTypes.Count == 0
                ? new[]
                {
                    BaseItemKind.Person,
                    BaseItemKind.Studio,
                    BaseItemKind.Year,
                    BaseItemKind.MusicGenre,
                    BaseItemKind.Genre
                }
                : Array.Empty<BaseItemKind>();

            var query = new InternalItemsQuery(user)
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
                Limit = limit * 5,
                IsPlayed = isPlayed,
                DtoOptions = options,
                MediaTypes = mediaTypes.ToArray()
            };

            if (parents.Count == 0)
            {
                return _libraryManager.GetItemList(query, false);
            }

            return _libraryManager.GetItemList(query, parents);
        }
    }
}
