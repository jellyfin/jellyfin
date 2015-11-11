using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Audio;

namespace MediaBrowser.Server.Implementations.Library
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

        public async Task<IEnumerable<Folder>> GetUserViews(UserViewQuery query, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(query.UserId);

            var folders = user.RootFolder
                .GetChildren(user, true)
                .OfType<Folder>()
                .ToList();

            if (!query.IncludeHidden)
            {
                folders = folders.Where(i =>
                {
                    var hidden = i as IHiddenFromDisplay;
                    return hidden == null || !hidden.IsHiddenFromUser(user);
                }).ToList();
            }

            var plainFolderIds = user.Configuration.PlainFolderViews.Select(i => new Guid(i)).ToList();

            var standaloneFolders = folders
                .Where(i => UserView.IsExcludedFromGrouping(i) || !user.IsFolderGrouped(i.Id))
                .ToList();

            var foldersWithViewTypes = folders
                .Except(standaloneFolders)
                .OfType<ICollectionFolder>()
                .ToList();

            var list = new List<Folder>();

            var enableUserViews = _config.Configuration.EnableUserViews || user.EnableUserViews;

            if (enableUserViews)
            {
                foreach (var folder in standaloneFolders)
                {
                    var collectionFolder = folder as ICollectionFolder;
                    var folderViewType = collectionFolder == null ? null : collectionFolder.CollectionType;

                    if (UserView.IsUserSpecific(folder))
                    {
                        list.Add(await GetUserView(folder.Id, folder.Name, folderViewType, true, string.Empty, user, cancellationToken).ConfigureAwait(false));
                    }
                    else if (plainFolderIds.Contains(folder.Id) && UserView.IsEligibleForEnhancedView(folderViewType))
                    {
                        list.Add(await GetUserView(folder, folderViewType, false, string.Empty, cancellationToken).ConfigureAwait(false));
                    }
                    else if (!string.IsNullOrWhiteSpace(folderViewType))
                    {
                        list.Add(await GetUserView(folder, folderViewType, true, string.Empty, cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        list.Add(folder);
                    }
                }
            }
            else
            {
                // TODO: Deprecate this whole block
                foreach (var folder in standaloneFolders)
                {
                    var collectionFolder = folder as ICollectionFolder;
                    var folderViewType = collectionFolder == null ? null : collectionFolder.CollectionType;

                    if (UserView.IsUserSpecific(folder))
                    {
                        list.Add(await GetUserView(folder.Id, folder.Name, folderViewType, true, string.Empty, user, cancellationToken).ConfigureAwait(false));
                    }
                    else if (plainFolderIds.Contains(folder.Id) && UserView.IsEligibleForEnhancedView(folderViewType))
                    {
                        list.Add(await GetUserView(folder.Id, folder.Name, folderViewType, false, string.Empty, user, cancellationToken).ConfigureAwait(false));
                    }
                    else if (!string.IsNullOrWhiteSpace(folderViewType))
                    {
                        list.Add(await GetUserView(folder.Id, folder.Name, folderViewType, true, string.Empty, user, cancellationToken).ConfigureAwait(false));
                    }
                    else
                    {
                        list.Add(folder);
                    }
                }
            }

            var parents = foldersWithViewTypes.Where(i => string.Equals(i.GetViewType(user), CollectionType.TvShows, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(i.GetViewType(user)))
                .ToList();

            if (parents.Count > 0)
            {
                list.Add(await GetUserView(parents, CollectionType.TvShows, string.Empty, user, enableUserViews, cancellationToken).ConfigureAwait(false));
            }

            parents = foldersWithViewTypes.Where(i => string.Equals(i.GetViewType(user), CollectionType.Movies, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(i.GetViewType(user)))
               .ToList();

            if (parents.Count > 0)
            {
                list.Add(await GetUserView(parents, CollectionType.Movies, string.Empty, user, enableUserViews, cancellationToken).ConfigureAwait(false));
            }

            if (user.Configuration.DisplayFoldersView)
            {
                var name = _localizationManager.GetLocalizedString("ViewType" + CollectionType.Folders);
                list.Add(await _libraryManager.GetNamedView(name, CollectionType.Folders, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (query.IncludeExternalContent)
            {
                var channelResult = await _channelManager.GetChannelsInternal(new ChannelQuery
                {
                    UserId = query.UserId

                }, cancellationToken).ConfigureAwait(false);

                var channels = channelResult.Items;

                var embeddedChannels = channels
                    .Where(i => user.Configuration.DisplayChannelsInline || user.Configuration.DisplayChannelsWithinViews.Contains(i.Id.ToString("N")))
                    .ToList();

                list.AddRange(embeddedChannels);

                if (channels.Length > embeddedChannels.Count)
                {
                    list.Add(await _channelManager.GetInternalChannelFolder(cancellationToken).ConfigureAwait(false));
                }

                if (_liveTvManager.GetEnabledUsers().Select(i => i.Id.ToString("N")).Contains(query.UserId))
                {
                    list.Add(await _liveTvManager.GetInternalLiveTvFolder(CancellationToken.None).ConfigureAwait(false));
                }
            }

            var sorted = _libraryManager.Sort(list, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending).ToList();

            var orders = user.Configuration.OrderedViews.ToList();

            return list
                .OrderBy(i =>
                {
                    var index = orders.IndexOf(i.Id.ToString("N"));

                    return index == -1 ? int.MaxValue : index;
                })
                .ThenBy(sorted.IndexOf)
                .ThenBy(i => i.SortName);
        }

        public Task<UserView> GetUserSubView(string name, string parentId, string type, string sortName, CancellationToken cancellationToken)
        {
            var uniqueId = parentId + "subview" + type;

            return _libraryManager.GetNamedView(name, parentId, type, sortName, uniqueId, cancellationToken);
        }

        public Task<UserView> GetUserSubView(string parentId, string type, string sortName, CancellationToken cancellationToken)
        {
            var name = _localizationManager.GetLocalizedString("ViewType" + type);

            return GetUserSubView(name, parentId, type, sortName, cancellationToken);
        }

        private async Task<UserView> GetUserView(List<ICollectionFolder> parents, string viewType, string sortName, User user, bool enableUserViews, CancellationToken cancellationToken)
        {
            if (parents.Count == 1 && parents.All(i => string.Equals((enableUserViews ? i.GetViewType(user) : i.CollectionType), viewType, StringComparison.OrdinalIgnoreCase)))
            {
                var parentId = parents[0].Id;

                var enableRichView = !user.Configuration.PlainFolderViews.Contains(parentId.ToString("N"), StringComparer.OrdinalIgnoreCase);

                return await GetUserView((Folder)parents[0], viewType, enableRichView, string.Empty, cancellationToken).ConfigureAwait(false);
            }

            var name = _localizationManager.GetLocalizedString("ViewType" + viewType);
            return await _libraryManager.GetNamedView(user, name, viewType, sortName, cancellationToken).ConfigureAwait(false);
        }

        public Task<UserView> GetUserView(Guid parentId, string name, string viewType, bool enableRichView, string sortName, User user, CancellationToken cancellationToken)
        {
            viewType = enableRichView ? viewType : null;
            return _libraryManager.GetNamedView(user, name, parentId.ToString("N"), viewType, sortName, null, cancellationToken);
        }

        public Task<UserView> GetUserView(Folder parent, string viewType, bool enableRichView, string sortName, CancellationToken cancellationToken)
        {
            viewType = enableRichView ? viewType : null;

            return _libraryManager.GetShadowView(parent, viewType, sortName, null, cancellationToken);
        }

        public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var includeTypes = request.IncludeItemTypes;

            var currentUser = user;

            var libraryItems = GetItemsForLatestItems(user, request.ParentId, includeTypes, request.Limit ?? 10).Where(i =>
            {
                if (request.IsPlayed.HasValue)
                {
                    var val = request.IsPlayed.Value;
                    if (i is Video && i.IsPlayed(currentUser) != val)
                    {
                        return false;
                    }
                }

                return true;
            });

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
                    var current = list.FirstOrDefault(i => i.Item1 != null && i.Item1.Id == container.Id);

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

        private IEnumerable<BaseItem> GetItemsForLatestItems(User user, string parentId, string[] includeItemTypes, int limit)
        {
            var parentIds = string.IsNullOrEmpty(parentId)
              ? new string[] { }
              : new[] { parentId };

            if (parentIds.Length == 0)
            {
                parentIds = user.RootFolder.GetChildren(user, true)
                    .OfType<Folder>()
                    .Select(i => i.Id.ToString("N"))
                    .Where(i => !user.Configuration.LatestItemsExcludes.Contains(i))
                    .ToArray();
            }

            var excludeItemTypes = includeItemTypes.Length == 0 ? new[] { "ChannelItem", "LiveTvItem", typeof(Person).Name, typeof(Studio).Name, typeof(Year).Name, typeof(GameGenre).Name, typeof(MusicGenre).Name, typeof(Genre).Name } : new string[] { };

            return _libraryManager.GetItems(new InternalItemsQuery(user)
            {
                IncludeItemTypes = includeItemTypes,
                SortOrder = SortOrder.Descending,
                SortBy = new[] { ItemSortBy.DateCreated },
                IsFolder = includeItemTypes.Length == 0 ? false : (bool?)null,
                ExcludeItemTypes = excludeItemTypes,
                ExcludeLocationTypes = new[] { LocationType.Virtual },
                Limit = limit * 20

            }, parentIds);
        }
    }
}
