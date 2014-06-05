using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Special class used for User Roots.  Children contain actual ones defined for this user
    /// PLUS the virtual folders from the physical root (added by plug-ins).
    /// </summary>
    public class UserRootFolder : Folder
    {
        /// <summary>
        /// Get the children of this folder from the actual file system
        /// </summary>
        /// <returns>IEnumerable{BaseItem}.</returns>
        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return base.GetNonCachedChildren(directoryService).Concat(LibraryManager.RootFolder.VirtualChildren);
        }

        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (string.Equals("default", Name, StringComparison.OrdinalIgnoreCase))
            {
                Name = "Media Folders";
                hasChanges = true;
            }

            return hasChanges;
        }

        protected override async Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            await base.ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService)
                .ConfigureAwait(false);

            // Not the best way to handle this, but it solves an issue
            // CollectionFolders aren't always getting saved after changes
            // This means that grabbing the item by Id may end up returning the old one
            // Fix is in two places - make sure the folder gets saved
            // And here to remedy it for affected users.
            // In theory this can be removed eventually.
            foreach (var item in Children)
            {
                LibraryManager.RegisterItem(item);
            }
        }

        public async Task<IEnumerable<Folder>> GetViews(User user, CancellationToken cancellationToken)
        {
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
                list.Add(await GetUserView(CollectionType.TvShows, user, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<MusicAlbum>().Any() ||
                recursiveChildren.OfType<MusicVideo>().Any())
            {
                list.Add(await GetUserView(CollectionType.Music, user, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<Movie>().Any())
            {
                list.Add(await GetUserView(CollectionType.Movies, user, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<Game>().Any())
            {
                list.Add(await GetUserView(CollectionType.Games, user, cancellationToken).ConfigureAwait(false));
            }

            if (recursiveChildren.OfType<BoxSet>().Any())
            {
                list.Add(await GetUserView(CollectionType.BoxSets, user, cancellationToken).ConfigureAwait(false));
            }

            return list.OrderBy(i => i.SortName);
        }

        // Use this to force new entity creation, as needed
        private const string DataVersion = "5";
        private async Task<UserView> GetUserView(string type, User user, CancellationToken cancellationToken)
        {
            var name = LocalizationManager.GetLocalizedString("ViewType" + type);

            var id = "view" + name + DataVersion + user.Id.ToString("N");
            var guid = id.GetMD5();

            var item = LibraryManager.GetItemById(guid) as UserView;

            if (item == null)
            {
                var path = System.IO.Path.Combine(user.ConfigurationDirectoryPath,
                    "views",
                    FileSystem.GetValidFilename(name));

                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

                item = new UserView
                {
                    Path = path,
                    Id = guid,
                    DateCreated = DateTime.UtcNow,
                    Name = name,
                    ViewType = type
                };

                await LibraryManager.CreateItem(item, cancellationToken).ConfigureAwait(false);

                await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
            }

            return item;
        }
    }
}
