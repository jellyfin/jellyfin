using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class MusicAlbumDynamicInfoProvider
    /// </summary>
    public class AlbumDynamicInfoProvider : BaseMetadataProvider, IDynamicInfoProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMetadataProvider" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public AlbumDynamicInfoProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            return true;
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
            var album = (MusicAlbum)item;

            var songs = album.RecursiveChildren
                             .OfType<Audio>()
                             .ToList();

            album.AlbumArtist = songs
                .Select(i => i.AlbumArtist)
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            album.Artists = songs.SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Don't save to the db
            return FalseTaskResult;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Last; }
        }
    }
}
