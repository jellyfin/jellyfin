using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
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
        private readonly IFileSystem _fileSystem;
        private readonly IUserManager _userManager;

        private readonly IChannelManager _channelManager;
        private readonly ILiveTvManager _liveTvManager;

        public UserViewManager(ILibraryManager libraryManager, ILocalizationManager localizationManager, IFileSystem fileSystem, IUserManager userManager, IChannelManager channelManager, ILiveTvManager liveTvManager)
        {
            _libraryManager = libraryManager;
            _localizationManager = localizationManager;
            _fileSystem = fileSystem;
            _userManager = userManager;
            _channelManager = channelManager;
            _liveTvManager = liveTvManager;
        }

        public async Task<IEnumerable<Folder>> GetUserViews(UserViewQuery query, CancellationToken cancellationToken)
        {
            var user = _userManager.GetUserById(new Guid(query.UserId));

            var folders = user.RootFolder
                .GetChildren(user, true)
                .OfType<Folder>()
                .ToList();

            var list = new List<Folder>();

            var excludeFolderIds = user.Configuration.ExcludeFoldersFromGrouping.Select(i => new Guid(i)).ToList();

            var standaloneFolders = folders.Where(i => UserView.IsExcludedFromGrouping(i) || excludeFolderIds.Contains(i.Id)).ToList();

            list.AddRange(standaloneFolders);

            var recursiveChildren = folders
                .Except(standaloneFolders)
                .SelectMany(i => i.GetRecursiveChildren(user, false))
                .ToList();

            if (recursiveChildren.OfType<Series>().Any())
            {
                list.Add(await GetUserView(CollectionType.TvShows, user, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<MusicAlbum>().Any() ||
                recursiveChildren.OfType<MusicVideo>().Any())
            {
                list.Add(await GetUserView(CollectionType.Music, user, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<Movie>().Any())
            {
                list.Add(await GetUserView(CollectionType.Movies, user, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<Game>().Any())
            {
                list.Add(await GetUserView(CollectionType.Games, user, string.Empty, cancellationToken).ConfigureAwait(false));
            }

            if (user.Configuration.DisplayCollectionsView ||
                recursiveChildren.OfType<BoxSet>().Any())
            {
                list.Add(await GetUserView(CollectionType.BoxSets, user, "zzz_" + CollectionType.BoxSets, cancellationToken).ConfigureAwait(false));
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

            return list.OrderBy(i => i.SortName);
        }

        private Task<UserView> GetUserView(string type, User user, string sortName, CancellationToken cancellationToken)
        {
            var name = _localizationManager.GetLocalizedString("ViewType" + type);

            return _libraryManager.GetNamedView(name, type, sortName, cancellationToken);
        }
    }
}
