using MediaBrowser.Common.IO;
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
using System.Xml;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class FanArtSeasonProvider
    /// </summary>
    class FanArtSeasonProvider : FanartBaseProvider
    {
        /// <summary>
        /// The _provider manager
        /// </summary>
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtSeasonProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        public FanArtSeasonProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            _fileSystem = fileSystem;
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
            return item is Season;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var season = (Season)item;
            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(FanArtTvProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "fanart.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(imagesFileInfo);
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
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var season = (Season)item;

            var seriesId = season.Series != null ? season.Series.GetProviderId(MetadataProviders.Tvdb) : null;

            if (!string.IsNullOrEmpty(seriesId))
            {
                // Process images
                var imagesXmlPath = Path.Combine(FanArtTvProvider.GetSeriesDataPath(ConfigurationManager.ApplicationPaths, seriesId), "fanart.xml");

                var imagesFileInfo = new FileInfo(imagesXmlPath);

                if (imagesFileInfo.Exists)
                {
                    if (!season.HasImage(ImageType.Thumb))
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(imagesXmlPath);

                        await FetchImages(season, xmlDoc, cancellationToken).ConfigureAwait(false);
                    }
                }

                BaseProviderInfo data;
                if (!item.ProviderData.TryGetValue(Id, out data))
                {
                    data = new BaseProviderInfo();
                    item.ProviderData[Id] = data;
                }

                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fetches the images.
        /// </summary>
        /// <param name="season">The season.</param>
        /// <param name="doc">The doc.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchImages(Season season, XmlDocument doc, CancellationToken cancellationToken)
        {
            var seasonNumber = season.IndexNumber ?? -1;

            if (seasonNumber == -1)
            {
                return;
            }

            var language = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower();

            if (ConfigurationManager.Configuration.DownloadSeasonImages.Thumb && !season.HasImage(ImageType.Thumb))
            {
                var node = doc.SelectSingleNode("//fanart/series/seasonthumbs/seasonthumb[@lang = \"" + language + "\"][@season = \"" + seasonNumber + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/series/seasonthumbs/seasonthumb[@season = \"" + seasonNumber + "\"]/@url");
                
                var path = node != null ? node.Value : null;
                
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(season, path, FanArtResourcePool, ImageType.Thumb, null, cancellationToken)
                                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
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
                return "3";
            }
        }
    }
}
