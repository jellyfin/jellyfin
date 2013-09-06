using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class LastFmArtistImageProvider
    /// </summary>
    public class LastFmImageProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LastFmImageProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public LastFmImageProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager) : 
            base(logManager, configurationManager)
        {
            _providerManager = providerManager;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Artist || item is MusicArtist || item is MusicAlbum;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.HasImage(ImageType.Primary))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(GetImageUrl(item)))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var url = GetImageUrl(item);

            if (!string.IsNullOrWhiteSpace(url))
            {
                await _providerManager.SaveImage(item, url, LastfmBaseProvider.LastfmResourcePool, ImageType.Primary, null, cancellationToken)
                                    .ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Fifth; }
        }

        /// <summary>
        /// Gets the image URL.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        private string GetImageUrl(BaseItem item)
        {
            var musicArtist = item as MusicArtist;

            if (musicArtist != null)
            {
                return musicArtist.LastFmImageUrl;
            }

            var artistByName = item as Artist;

            if (artistByName != null)
            {
                return artistByName.LastFmImageUrl;
            }

            var album = item as MusicAlbum;

            if (album != null)
            {
                return album.LastFmImageUrl;
            }

            return null;
        }
    }
}
