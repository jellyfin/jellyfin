using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library
{
    public class UserViewManager : IUserViewManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILocalizationManager _localizationManager;
        private readonly IUserManager _userManager;

        private readonly IChannelManager _channelManager;
        private readonly ILiveTvManager _liveTvManager;
        private readonly IPlaylistManager _playlists;
        private readonly ICollectionManager _collectionManager;
        private readonly IServerConfigurationManager _config;

        public UserViewManager(ILibraryManager libraryManager, ILocalizationManager localizationManager, IUserManager userManager, IChannelManager channelManager, ILiveTvManager liveTvManager, IPlaylistManager playlists, ICollectionManager collectionManager, IServerConfigurationManager config)
        {
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _userManager = userManager;
            _channelManager = channelManager;
            _liveTvManager = liveTvManager;
            _playlists = playlists;
            _collectionManager = collectionManager;
            _config = config;
        }

        public async Task<IEnumerable<Folder>> GetUserViews(UserViewQuery query, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(query.UserId);

            var folders = user.RootFolder
                .GetChildren(user, true)
                .OfType<Folder>()
                .ToList();

            var list = new List<Folder>();

            var excludeFolderIds = user.Configuration.ExcludeFoldersFromGrouping.Select(i => new Guid(i)).ToList();

            var standaloneFolders = folders.Where(i => UserView.IsExcludedFromGrouping(i) || excludeFolderIds.Contains(i.Id)).ToList();

            var foldersWithViewTypes = folders
                .Except(standaloneFolders)
                .OfType<ICollectionFolder>()
                .ToList();

            list.AddRange(standaloneFolders);

            if (foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.TvShows, StringComparison.OrdinalIgnoreCase)) ||
                foldersWithViewTypes.Any(i => string.IsNullOrWhiteSpace(i.CollectionType)))
            {
                list.Add(await GetUserView(CollectionType.TvShows, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase)) ||
                foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.MusicVideos, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(await GetUserView(CollectionType.Music, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.Movies, StringComparison.OrdinalIgnoreCase)) ||
                foldersWithViewTypes.Any(i => string.IsNullOrWhiteSpace(i.CollectionType)))
            {
                list.Add(await GetUserView(CollectionType.Movies, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.Games, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(await GetUserView(CollectionType.Games, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (user.Configuration.DisplayCollectionsView)
            {
                bool showCollectionView;
                if (_config.Configuration.EnableLegacyCollections)
                {
                    showCollectionView = folders
                        .Except(standaloneFolders)
                        .SelectMany(i => i.GetRecursiveChildren(user, false)).OfType<BoxSet>().Any();
                }
                else
                {
                    showCollectionView = _collectionManager.GetCollections(user).Any();
                }

                if (showCollectionView)
                {
                    list.Add(await GetUserView(CollectionType.BoxSets, string.Empty, cancellationToken).ConfigureAwait(false));
                }
            }

            if (foldersWithViewTypes.Any(i => string.Equals(i.CollectionType, CollectionType.Playlists, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(_playlists.GetPlaylistsFolder(user.Id.ToString("N")));
            }

            if (user.Configuration.DisplayFoldersView)
            {
                list.Add(await GetUserView(CollectionType.Folders, "zz_" + CollectionType.Folders, cancellationToken).ConfigureAwait(false));
            }

            if (query.IncludeExternalContent)
            {
                var channelResult = await _channelManager.GetChannels(new ChannelQuery
                {
                    UserId = query.UserId

                }, cancellationToken).ConfigureAwait(false);

                var channels = channelResult.Items;

                var embeddedChannels = channels
                    .Where(i => user.Configuration.DisplayChannelsWithinViews.Contains(i.Id))
                    .ToList();

                list.AddRange(embeddedChannels.Select(i => _channelManager.GetChannel(i.Id)));

                if (channels.Length > embeddedChannels.Count)
                {
                    list.Add(await _channelManager.GetInternalChannelFolder(query.UserId, cancellationToken).ConfigureAwait(false));
                }

                if (_liveTvManager.GetEnabledUsers().Select(i => i.Id.ToString("N")).Contains(query.UserId))
                {
                    list.Add(await _liveTvManager.GetInternalLiveTvFolder(query.UserId, cancellationToken).ConfigureAwait(false));
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

        public Task<UserView> GetUserView(string name, string parentId, string type, User user, string sortName, CancellationToken cancellationToken)
        {
            return _libraryManager.GetSpecialFolder(user, name, parentId, type, sortName, cancellationToken);
        }

        public Task<UserView> GetUserView(string parentId, string type, User user, string sortName, CancellationToken cancellationToken)
        {
            var name = _localizationManager.GetLocalizedString("ViewType" + type);

            return GetUserView(name, parentId, type, user, sortName, cancellationToken);
        }

        public Task<UserView> GetUserView(string type, string sortName, CancellationToken cancellationToken)
        {
            var name = _localizationManager.GetLocalizedString("ViewType" + type);

            return _libraryManager.GetNamedView(name, type, sortName, cancellationToken);
        }
    }
}
