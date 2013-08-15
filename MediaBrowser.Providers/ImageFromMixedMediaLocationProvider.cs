using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    class ImageFromMixedMediaLocationProvider : BaseMetadataProvider
    {
        public ImageFromMixedMediaLocationProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
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
            if (item.LocationType != LocationType.FileSystem || item.ResolveArgs.IsDirectory)
            {
                return false;
            }

            var video = item as Video;

            if (video != null && !(item is Episode))
            {
                return video.IsInMixedFolder;
            }

            var game = item as Game;

            if (game != null)
            {
                return game.IsInMixedFolder;
            }

            return false;
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

        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            return TrueTaskResult;
        }
    }
}
