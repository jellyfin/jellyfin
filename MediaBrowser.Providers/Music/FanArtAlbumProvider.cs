using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using System.Net;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class FanArtAlbumProvider
    /// </summary>
    public class FanArtAlbumProvider : BaseMetadataProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtAlbumProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public FanArtAlbumProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            _fileSystem = fileSystem;
            HttpClient = httpClient;
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
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }
        
        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "18";
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (!ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc &&
                !ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary)
            {
                return false;
            }

            if (item.HasImage(ImageType.Primary) && item.HasImage(ImageType.Disc))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var artistMusicBrainzId = item.Parent.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(artistMusicBrainzId))
            {
                var artistXmlPath = FanartArtistProvider.GetArtistXmlPath(ConfigurationManager.CommonApplicationPaths, artistMusicBrainzId);

                var file = new FileInfo(artistXmlPath);

                if (file.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(file);
                }
            } 
            
            return base.CompareDate(item);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualFanartAlbumProvider.ProviderName).ConfigureAwait(false);
                await FetchFromXml(item, images.ToList(), cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);

            return true;
        }

        /// <summary>
        /// Fetches from XML.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchFromXml(BaseItem item, List<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary && !item.HasImage(ImageType.Primary))
            {
                await SaveImage(item, images, ImageType.Primary, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc && !item.HasImage(ImageType.Disc))
            {
                await SaveImage(item, images, ImageType.Disc, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SaveImage(BaseItem item, List<RemoteImageInfo> images, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                try
                {
                    await _providerManager.SaveImage(item, image.Url, FanartArtistProvider.FanArtResourcePool, type, null, cancellationToken).ConfigureAwait(false);
                    break;
                }
                catch (HttpException ex)
                {
                    // Sometimes fanart has bad url's in their xml
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    break;
                }
            }
        }
    }
}
