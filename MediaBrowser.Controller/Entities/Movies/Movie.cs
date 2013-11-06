using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class Movie
    /// </summary>
    public class Movie : Video, IHasCriticRating
    {
        public List<Guid> SpecialFeatureIds { get; set; }

        public Movie()
        {
            SpecialFeatureIds = new List<Guid>();
        }

        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        public float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the critic rating summary.
        /// </summary>
        /// <value>The critic rating summary.</value>
        public string CriticRatingSummary { get; set; }

        /// <summary>
        /// Gets or sets the name of the TMDB collection.
        /// </summary>
        /// <value>The name of the TMDB collection.</value>
        public string TmdbCollectionName { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return this.GetProviderId(MetadataProviders.Tmdb) ?? this.GetProviderId(MetadataProviders.Imdb) ?? base.GetUserDataKey();
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for special features
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="forceSave">if set to <c>true</c> [is new item].</param>
        /// <param name="forceRefresh">if set to <c>true</c> [force].</param>
        /// <param name="allowSlowProviders">if set to <c>true</c> [allow slow providers].</param>
        /// <param name="resetResolveArgs">The reset resolve args.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> RefreshMetadata(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true, bool resetResolveArgs = true)
        {
            // Kick off a task to refresh the main item
            var result = await base.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders, resetResolveArgs).ConfigureAwait(false);

            var specialFeaturesChanged = false;

            // Must have a parent to have special features
            // In other words, it must be part of the Parent/Child tree
            if (LocationType == LocationType.FileSystem && Parent != null && !IsInMixedFolder)
            {
                specialFeaturesChanged = await RefreshSpecialFeatures(cancellationToken, forceSave, forceRefresh, allowSlowProviders).ConfigureAwait(false);
            }

            return specialFeaturesChanged || result;
        }

        private async Task<bool> RefreshSpecialFeatures(CancellationToken cancellationToken, bool forceSave = false, bool forceRefresh = false, bool allowSlowProviders = true)
        {
            var newItems = LoadSpecialFeatures().ToList();
            var newItemIds = newItems.Select(i => i.Id).ToList();

            var itemsChanged = !SpecialFeatureIds.SequenceEqual(newItemIds);

            var tasks = newItems.Select(i => i.RefreshMetadata(cancellationToken, forceSave, forceRefresh, allowSlowProviders, resetResolveArgs: false));

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            SpecialFeatureIds = newItemIds;

            return itemsChanged || results.Contains(true);
        }

        /// <summary>
        /// Loads the special features.
        /// </summary>
        /// <returns>IEnumerable{Video}.</returns>
        private IEnumerable<Video> LoadSpecialFeatures()
        {
            FileSystemInfo folder;

            try
            {
                folder = ResolveArgs.GetFileSystemEntryByName("specials");
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
                return new List<Video>();
            }

            // Path doesn't exist. No biggie
            if (folder == null)
            {
                return new List<Video>();
            }

            IEnumerable<FileSystemInfo> files;

            try
            {
                files = new DirectoryInfo(folder.FullName).EnumerateFiles();
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error loading special features for {0}", ex, Name);
                return new List<Video>();
            }

            return LibraryManager.ResolvePaths<Video>(files, null).Select(video =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.RetrieveItem(video.Id) as Video;

                if (dbItem != null)
                {
                    dbItem.ResetResolveArgs(video.ResolveArgs);
                    video = dbItem;
                }

                return video;
            });
        }

    }
}
