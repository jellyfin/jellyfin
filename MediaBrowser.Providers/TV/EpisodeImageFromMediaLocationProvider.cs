using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class EpisodeImageFromMediaLocationProvider
    /// </summary>
    public class EpisodeImageFromMediaLocationProvider : BaseMetadataProvider
    {
        public EpisodeImageFromMediaLocationProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }
        
        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode && item.LocationType == LocationType.FileSystem;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the filestamp extensions.
        /// </summary>
        /// <value>The filestamp extensions.</value>
        protected override string[] FilestampExtensions
        {
            get
            {
                return BaseItem.SupportedImageExtensions;
            }
        }
        
        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var episode = (Episode)item;

            var episodeFileName = Path.GetFileName(episode.Path);

            var parent = item.ResolveArgs.Parent;

            ValidateImage(episode);

            cancellationToken.ThrowIfCancellationRequested();

            SetPrimaryImagePath(episode, parent, item.MetaLocation, episodeFileName);

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        /// <summary>
        /// Validates the primary image path still exists
        /// </summary>
        /// <param name="episode">The episode.</param>
        /// <param name="metadataFolderPath">The metadata folder path.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private void ValidateImage(Episode episode)
        {
            var path = episode.PrimaryImagePath;

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                episode.PrimaryImagePath = null;
            }
        }

        /// <summary>
        /// Sets the primary image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="metadataFolder">The metadata folder.</param>
        /// <param name="episodeFileName">Name of the episode file.</param>
        private void SetPrimaryImagePath(Episode item, Folder parent, string metadataFolder, string episodeFileName)
        {
            // Look for the image file in the metadata folder, and if found, set PrimaryImagePath
            var imageFiles = new[] {
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".jpg")),
                Path.Combine(metadataFolder, Path.ChangeExtension(episodeFileName, ".png"))
            };

            var file = parent.ResolveArgs.GetMetaFileByPath(imageFiles[0]) ??
                       parent.ResolveArgs.GetMetaFileByPath(imageFiles[1]);

            if (file != null)
            {
                item.PrimaryImagePath = file.FullName;
            }
        }
    }
}
