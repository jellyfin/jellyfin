using System.Runtime.Serialization;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder, IMetadataContainer, IItemByName, IHasMusicGenres, IHasDualAccess, IHasProductionLocations, IHasLookupInfo<ArtistInfo>
    {
        public bool IsAccessedByName { get; set; }
        public List<string> ProductionLocations { get; set; }
        
        public override bool IsFolder
        {
            get
            {
                return !IsAccessedByName;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAddingToPlaylist
        {
            get { return true; }
        }

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

        public MusicArtist()
        {
            ProductionLocations = new List<string>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetUserDataKey(this);
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
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
        private static string GetUserDataKey(MusicArtist item)
        {
            var id = item.GetProviderId(MetadataProviders.MusicBrainzArtist);

            if (!string.IsNullOrEmpty(id))
            {
                return "Artist-Musicbrainz-" + id;
            }

            return "Artist-" + item.Name;
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Music);
        }

        public async Task RefreshAllMetadata(MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var items = RecursiveChildren.ToList();

            var songs = items.OfType<Audio>().ToList();

            var others = items.Except(songs).ToList();

            var totalItems = songs.Count + others.Count;
            var percentages = new Dictionary<Guid, double>(totalItems);

            var tasks = new List<Task>();

            // Refresh songs
            foreach (var item in songs)
            {
                if (tasks.Count >= 3)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    tasks.Clear();
                }

                cancellationToken.ThrowIfCancellationRequested();
                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentChild = item;
                innerProgress.RegisterAction(p =>
                {
                    lock (percentages)
                    {
                        percentages[currentChild.Id] = p / 100;

                        var percent = percentages.Values.Sum();
                        percent /= totalItems;
                        percent *= 100;
                        progress.Report(percent);
                    }
                });

                var taskChild = item;
                tasks.Add(Task.Run(async () => await RefreshItem(taskChild, refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false), cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            tasks.Clear();

            // Refresh current item
            await RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);
            
            // Refresh all non-songs
            foreach (var item in others)
            {
                if (tasks.Count >= 3)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    tasks.Clear();
                }

                cancellationToken.ThrowIfCancellationRequested();
                var innerProgress = new ActionableProgress<double>();

                // Avoid implicitly captured closure
                var currentChild = item;
                innerProgress.RegisterAction(p =>
                {
                    lock (percentages)
                    {
                        percentages[currentChild.Id] = p / 100;

                        var percent = percentages.Values.Sum();
                        percent /= totalItems;
                        percent *= 100;
                        progress.Report(percent);
                    }
                });

                // Avoid implicitly captured closure
                var taskChild = item;
                tasks.Add(Task.Run(async () => await RefreshItem(taskChild, refreshOptions, innerProgress, cancellationToken).ConfigureAwait(false), cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            progress.Report(100);
        }

        private async Task RefreshItem(BaseItem item, MetadataRefreshOptions refreshOptions, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await item.RefreshMetadata(refreshOptions, cancellationToken).ConfigureAwait(false);

            progress.Report(100);
        }

        public ArtistInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<ArtistInfo>();

            info.SongInfos = RecursiveChildren.OfType<Audio>()
                .Select(i => i.GetLookupInfo())
                .ToList();

            return info;
        }

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            return inputItems.OfType<IHasArtist>()
                .Where(i => i.HasArtist(Name))
                .Cast<BaseItem>();
        }
    }
}
