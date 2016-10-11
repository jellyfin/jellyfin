using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
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

            var groupedFolders = new List<ICollectionFolder>();

            var list = new List<Folder>();

            foreach (var folder in folders)
            {
                var collectionFolder = folder as ICollectionFolder;
                var folderViewType = collectionFolder == null ? null : collectionFolder.CollectionType;

                if (UserView.IsUserSpecific(folder))
                {
                    list.Add(await _libraryManager.GetNamedView(user, folder.Name, folder.Id.ToString("N"), folderViewType, null, cancellationToken).ConfigureAwait(false));
                    continue;
                }

                if (plainFolderIds.Contains(folder.Id) && UserView.IsEligibleForEnhancedView(folderViewType))
                {
                    list.Add(folder);
                    continue;
                }

                if (collectionFolder != null && UserView.IsEligibleForGrouping(folder) && user.IsFolderGrouped(folder.Id))
                {
                    groupedFolders.Add(collectionFolder);
                    continue;
                }

                if (query.PresetViews.Contains(folderViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(await GetUserView(folder, folderViewType, string.Empty, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    list.Add(folder);
                }
            }

            foreach (var viewType in new[] { CollectionType.Movies, CollectionType.TvShows })
            {
                var parents = groupedFolders.Where(i => string.Equals(i.CollectionType, viewType, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(i.CollectionType))
                    .ToList();

                if (parents.Count > 0)
                {
                    list.Add(await GetUserView(parents, viewType, string.Empty, user, query.PresetViews, cancellationToken).ConfigureAwait(false));
                }
            }

            if (_config.Configuration.EnableFolderView)
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
                
                if (_config.Configuration.EnableChannelView && channels.Length > 0)
                {
                    list.Add(await _channelManager.GetInternalChannelFolder(cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    list.AddRange(channels);
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

                    if (index == -1)
                    {
                        var view = i as UserView;
                        if (view != null)
                        {
                            if (view.DisplayParentId != Guid.Empty)
                            {
                                index = orders.IndexOf(view.DisplayParentId.ToString("N"));
                            }
                        }
                    }

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

        private async Task<Folder> GetUserView(List<ICollectionFolder> parents, string viewType, string sortName, User user, string[] presetViews, CancellationToken cancellationToken)
        {
            if (parents.Count == 1 && parents.All(i => string.Equals(i.CollectionType, viewType, StringComparison.OrdinalIgnoreCase)))
            {
                if (!presetViews.Contains(viewType, StringComparer.OrdinalIgnoreCase))
                {
                    return (Folder)parents[0];
                }

                return await GetUserView((Folder)parents[0], viewType, string.Empty, cancellationToken).ConfigureAwait(false);
            }

            var name = _localizationManager.GetLocalizedString("ViewType" + viewType);
            return await _libraryManager.GetNamedView(user, name, viewType, sortName, cancellationToken).ConfigureAwait(false);
        }

        public Task<UserView> GetUserView(Folder parent, string viewType, string sortName, CancellationToken cancellationToken)
        {
            return _libraryManager.GetShadowView(parent, viewType, sortName, cancellationToken);
        }

        public List<Tuple<BaseItem, List<BaseItem>>> GetLatestItems(LatestItemsQuery request)
        {
            var user = _userManager.GetUserById(request.UserId);

            var libraryItems = GetItemsForLatestItems(user, request);

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

        private IEnumerable<BaseItem> GetItemsForLatestItems(User user, LatestItemsQuery request)
        {
            var parentId = request.ParentId;

            var includeItemTypes = request.IncludeItemTypes;
            var limit = request.Limit ?? 10;

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

            if (parentIds.Length == 0)
            {
                return new List<BaseItem>();
            }

            var excludeItemTypes = includeItemTypes.Length == 0 ? new[]
            {
                typeof(Person).Name,
                typeof(Studio).Name,
                typeof(Year).Name,
                typeof(GameGenre).Name,
                typeof(MusicGenre).Name,
                typeof(Genre).Name

            } : new string[] { };

            return _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = includeItemTypes,
                SortOrder = SortOrder.Descending,
                SortBy = new[] { ItemSortBy.DateCreated },
                IsFolder = includeItemTypes.Length == 0 ? false : (bool?)null,
                ExcludeItemTypes = excludeItemTypes,
                ExcludeLocationTypes = new[] { LocationType.Virtual },
                Limit = limit * 5,
                SourceTypes = parentIds.Length == 0 ? new[] { SourceType.Library } : new SourceType[] { },
                IsPlayed = request.IsPlayed

            }, parentIds);
        }
    }
}
