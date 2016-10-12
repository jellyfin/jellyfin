using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder, IMetadataContainer, IItemByName, IHasMusicGenres, IHasDualAccess, IHasLookupInfo<ArtistInfo>
    {
        [IgnoreDataMember]
        public bool IsAccessedByName
        {
            get { return ParentId == Guid.Empty; }
        }

        [IgnoreDataMember]
        public override bool IsFolder
        {
            get
            {
                return !IsAccessedByName;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsCumulativeRunTimeTicks
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return false;
            }
        }

        public override bool CanDelete()
        {
            return !IsAccessedByName;
        }

        public IEnumerable<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                query.IncludeItemTypes = new[] { typeof(Audio).Name, typeof(MusicVideo).Name, typeof(MusicAlbum).Name };
                query.ArtistNames = new[] { Name };
            }

            return LibraryManager.GetItemList(query);
        }

        [IgnoreDataMember]
        protected override IEnumerable<BaseItem> ActualChildren
        {
            get
            {
                if (IsAccessedByName)
                {
                    return new List<BaseItem>();
                }

                return base.ActualChildren;
            }
        }

        public override int GetChildCount(User user)
        {
            if (IsAccessedByName)
            {
                return 0;
            }
            return base.GetChildCount(user);
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            if (IsAccessedByName)
            {
                return true;
            }

            return base.IsSaveLocalMetadataEnabled();
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            if (IsAccessedByName)
            {
                // Should never get in here anyway
                return _cachedTask;
            }

            return base.ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService);
        }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            list.InsertRange(0, GetUserDataKeys(this));
            return list;
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        [IgnoreDataMember]
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private static List<string> GetUserDataKeys(MusicArtist item)
        {
            var list = new List<string>();
            var id = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(id))
            {
                list.Add("Artist-Musicbrainz-" + id);
            }

            list.Add("Artist-" + (item.Name ?? string.Empty).RemoveDiacritics());
            return list;
        }
        public override string CreatePresentationUniqueKey()
        {
            return "Artist-" + (Name ?? string.Empty).RemoveDiacritics();
        }
        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Music;
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = GetRecursiveChildren().ToList();

            var songs = items.OfType<Audio>().ToList();

            var others = items.Except(songs).ToList();

            var totalItems = songs.Count + others.Count;
            var numComplete = 0;

            var childUpdateType = ItemUpdateType.None;

            // Refresh songs
            foreach (var item in songs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updateType = await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
                childUpdateType = childUpdateType | updateType;

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            var parentRefreshOptions = refreshOptions;
            if (childUpdateType > ItemUpdateType.None)
            {
                parentRefreshOptions = new MetadataRefreshOptions(refreshOptions);
                parentRefreshOptions.MetadataRefreshMode = MetadataRefreshMode.FullRefresh;
            }

            // Refresh current item
            await RefreshMetadata(parentRefreshOptions, cancellationToken).ConfigureAwait(false);

            // Refresh all non-songs
            foreach (var item in others)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var updateType = await item.RefreshMetadata(parentRefreshOptions, cancellationToken).ConfigureAwait(false);

                numComplete++;
                double percent = numComplete;
                percent /= totalItems;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }

        public ArtistInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<ArtistInfo>();

            info.SongInfos = GetRecursiveChildren(i => i is Audio)
                .Cast<Audio>()
                .Select(i => i.GetLookupInfo())
                .ToList();

            return info;
        }

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            return inputItems.Where(GetItemFilter());
        }

        public Func<BaseItem, bool> GetItemFilter()
        {
            return i =>
            {
                var hasArtist = i as IHasArtist;
                return hasArtist != null && hasArtist.HasAnyArtist(Name);
            };
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }

        public static string GetPath(string name, bool normalizeName = true)
        {
            // Trim the period at the end because windows will have a hard time with that
            var validName = normalizeName ?
                FileSystem.GetValidFilename(name).Trim().TrimEnd('.') :
                name;

            return System.IO.Path.Combine(ConfigurationManager.ApplicationPaths.ArtistsPath, validName);
        }

        private string GetRebasedPath()
        {
            return GetPath(System.IO.Path.GetFileName(Path), false);
        }

        public override bool RequiresRefresh()
        {
            if (IsAccessedByName)
            {
                var newPath = GetRebasedPath();
                if (!string.Equals(Path, newPath, StringComparison.Ordinal))
                {
                    Logger.Debug("{0} path has changed from {1} to {2}", GetType().Name, Path, newPath);
                    return true;
                }
            }
            return base.RequiresRefresh();
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            if (IsAccessedByName)
            {
                var newPath = GetRebasedPath();
                if (!string.Equals(Path, newPath, StringComparison.Ordinal))
                {
                    Path = newPath;
                    hasChanges = true;
                }
            }

            return hasChanges;
        }
    }
}
