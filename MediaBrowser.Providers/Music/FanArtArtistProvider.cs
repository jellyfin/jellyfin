using MediaBrowser.Common.Configuration;
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Net;
using System.Net;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class FanArtArtistProvider
    /// </summary>
    public class FanArtArtistProvider : FanartBaseProvider
    {
        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;

        internal static FanArtArtistProvider Current;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtArtistProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public FanArtArtistProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
            _fileSystem = fileSystem;

            Current = this;
        }

        /// <summary>
        /// The fan art base URL
        /// </summary>
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/artist/{0}/{1}/xml/all/1/1";

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
            return item is MusicArtist;
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
                return "7";
            }
        }

        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.Fourth;
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
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Musicbrainz)))
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var musicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(musicBrainzId))
            {
                // Process images
                var artistXmlPath = GetArtistDataPath(ConfigurationManager.CommonApplicationPaths, musicBrainzId);
                artistXmlPath = Path.Combine(artistXmlPath, "fanart.xml");

                var file = new FileInfo(artistXmlPath);

                return !file.Exists || _fileSystem.GetLastWriteTimeUtc(file) > providerInfo.LastRefreshed;
            }

            return base.NeedsRefreshBasedOnCompareDate(item, providerInfo);
        }

        /// <summary>
        /// The us culture
        /// </summary>
        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <param name="musicBrainzArtistId">The music brainz artist identifier.</param>
        /// <returns>System.String.</returns>
        internal static string GetArtistDataPath(IApplicationPaths appPaths, string musicBrainzArtistId)
        {
            var dataPath = Path.Combine(GetArtistDataPath(appPaths), musicBrainzArtistId);

            return dataPath;
        }

        /// <summary>
        /// Gets the artist data path.
        /// </summary>
        /// <param name="appPaths">The application paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetArtistDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-music");

            return dataPath;
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
            cancellationToken.ThrowIfCancellationRequested();

            var musicBrainzId = item.GetProviderId(MetadataProviders.Musicbrainz);

            var artistDataPath = GetArtistDataPath(ConfigurationManager.ApplicationPaths, musicBrainzId);
            var xmlPath = Path.Combine(artistDataPath, "fanart.xml");

            // Only download the xml if it doesn't already exist. The prescan task will take care of getting updates
            if (!File.Exists(xmlPath))
            {
                await DownloadArtistXml(artistDataPath, musicBrainzId, cancellationToken).ConfigureAwait(false);
            }

            if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Art ||
                ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops ||
                ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner ||
                ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo ||
                ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary)
            {
                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, ManualFanartArtistProvider.ProviderName).ConfigureAwait(false);
                await FetchFromXml(item, images.ToList(), cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// Downloads the artist XML.
        /// </summary>
        /// <param name="artistPath">The artist path.</param>
        /// <param name="musicBrainzId">The music brainz id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        internal async Task DownloadArtistXml(string artistPath, string musicBrainzId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format(FanArtBaseUrl, ApiKey, musicBrainzId);

            var xmlPath = Path.Combine(artistPath, "fanart.xml");

            Directory.CreateDirectory(artistPath);
            
            using (var response = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = _fileSystem.GetFileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Fetches from XML.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchFromXml(BaseItem item, List<RemoteImageInfo> images , CancellationToken cancellationToken)
        {
            if (!item.LockedFields.Contains(MetadataFields.Images))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Primary && !item.HasImage(ImageType.Primary))
                {
                    await SaveImage(item, images, ImageType.Primary, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Logo && !item.HasImage(ImageType.Logo))
                {
                    await SaveImage(item, images, ImageType.Logo, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Art && !item.HasImage(ImageType.Art))
                {
                    await SaveImage(item, images, ImageType.Art, cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Banner && !item.HasImage(ImageType.Banner))
                {
                    await SaveImage(item, images, ImageType.Banner, cancellationToken).ConfigureAwait(false);
                }
            }

            if (!item.LockedFields.Contains(MetadataFields.Backdrops))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var backdropLimit = ConfigurationManager.Configuration.MusicOptions.MaxBackdrops;
                if (ConfigurationManager.Configuration.DownloadMusicArtistImages.Backdrops &&
                    item.BackdropImagePaths.Count < backdropLimit)
                {
                    foreach (var image in images.Where(i => i.Type == ImageType.Backdrop))
                    {
                        await _providerManager.SaveImage(item, image.Url, FanArtResourcePool, ImageType.Backdrop, null, cancellationToken)
                            .ConfigureAwait(false);

                        if (item.BackdropImagePaths.Count >= backdropLimit) break;
                    }
                }
            }
        }

        private async Task SaveImage(BaseItem item, List<RemoteImageInfo> images, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                try
                {
                    await _providerManager.SaveImage(item, image.Url, FanArtResourcePool, type, null, cancellationToken).ConfigureAwait(false);
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
