using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class FanArtMovieProvider
    /// </summary>
    class FanArtMovieProvider : FanartBaseProvider
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

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        internal static FanArtMovieProvider Current { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FanArtMovieProvider" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="providerManager">The provider manager.</param>
        /// <exception cref="System.ArgumentNullException">httpClient</exception>
        public FanArtMovieProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
            _providerManager = providerManager;
            Current = this;
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
                return "13";
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
        /// The fan art base URL
        /// </summary>
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/movie/{0}/{1}/xml/all/1/1";

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            return item is Movie || item is BoxSet || item is MusicVideo;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tmdb)))
            {
                return false;
            }

            if (!ConfigurationManager.Configuration.DownloadMovieImages.Art &&
                !ConfigurationManager.Configuration.DownloadMovieImages.Logo &&
                !ConfigurationManager.Configuration.DownloadMovieImages.Disc &&
                !ConfigurationManager.Configuration.DownloadMovieImages.Backdrops &&
                !ConfigurationManager.Configuration.DownloadMovieImages.Banner &&
                !ConfigurationManager.Configuration.DownloadMovieImages.Thumb)
            {
                return false;
            }

            if (item.HasImage(ImageType.Art) &&
                item.HasImage(ImageType.Logo) &&
                item.HasImage(ImageType.Disc) &&
                item.HasImage(ImageType.Banner) &&
                item.HasImage(ImageType.Thumb) &&
                item.BackdropImagePaths.Count > 0)
            {
                return false;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override DateTime CompareDate(BaseItem item)
        {
            var id = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(id))
            {
                // Process images
                var path = GetMovieDataPath(ConfigurationManager.ApplicationPaths, id);

                var files = new DirectoryInfo(path)
                    .EnumerateFiles("*.xml", SearchOption.TopDirectoryOnly)
                    .Select(i => i.LastWriteTimeUtc)
                    .ToList();

                if (files.Count > 0)
                {
                    return files.Max();
                }
            }

            return base.CompareDate(item);
        }

        /// <summary>
        /// Gets the movie data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="tmdbId">The TMDB id.</param>
        /// <returns>System.String.</returns>
        internal static string GetMovieDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = Path.Combine(GetMoviesDataPath(appPaths), tmdbId);

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return dataPath;
        }

        /// <summary>
        /// Gets the movie data path.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <returns>System.String.</returns>
        internal static string GetMoviesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "fanart-movies");

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            return dataPath;
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

            var movieId = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(movieId))
            {
                var movieDataPath = GetMovieDataPath(ConfigurationManager.ApplicationPaths, movieId);
                var xmlPath = Path.Combine(movieDataPath, "fanart.xml");

                // Only download the xml if it doesn't already exist. The prescan task will take care of getting updates
                if (!File.Exists(xmlPath))
                {
                    await DownloadMovieXml(movieDataPath, movieId, cancellationToken).ConfigureAwait(false);
                }

                if (File.Exists(xmlPath))
                {
                    await FetchFromXml(item, xmlPath, cancellationToken).ConfigureAwait(false);
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Downloads the movie XML.
        /// </summary>
        /// <param name="movieDataPath">The movie data path.</param>
        /// <param name="tmdbId">The TMDB id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task DownloadMovieXml(string movieDataPath, string tmdbId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string url = string.Format(FanArtBaseUrl, ApiKey, tmdbId);

            var xmlPath = Path.Combine(movieDataPath, "fanart.xml");

            using (var response = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = FanArtResourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                using (var xmlFileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await response.CopyToAsync(xmlFileStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Fetches from XML.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="xmlFilePath">The XML file path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchFromXml(BaseItem item, string xmlFilePath, CancellationToken cancellationToken)
        {
            var doc = new XmlDocument();
            doc.Load(xmlFilePath);
            
            var language = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower();
            
            cancellationToken.ThrowIfCancellationRequested();

            string path;
            var hd = ConfigurationManager.Configuration.DownloadHDFanArt ? "hd" : "";

            if (ConfigurationManager.Configuration.DownloadMovieImages.Logo && !item.HasImage(ImageType.Logo))
            {
                var node =
                    doc.SelectSingleNode("//fanart/movie/movielogos/" + hd + "movielogo[@lang = \"" + language + "\"]/@url") ??
                    doc.SelectSingleNode("//fanart/movie/movielogos/movielogo[@lang = \"" + language + "\"]/@url");
                if (node == null && language != "en")
                {
                    //maybe just couldn't find language - try just first one
                    node = doc.SelectSingleNode("//fanart/movie/movielogos/" + hd + "movielogo/@url");
                }
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Logo, null, cancellationToken).ConfigureAwait(false);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMovieImages.Art && !item.HasImage(ImageType.Art))
            {
                var node =
                    doc.SelectSingleNode("//fanart/movie/moviearts/" + hd + "movieart[@lang = \"" + language + "\"]/@url") ??
                    doc.SelectSingleNode("//fanart/movie/moviearts/" + hd + "movieart/@url") ??
                    doc.SelectSingleNode("//fanart/movie/moviearts/movieart[@lang = \"" + language + "\"]/@url") ??
                    doc.SelectSingleNode("//fanart/movie/moviearts/movieart/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Art, null, cancellationToken)
                                        .ConfigureAwait(false);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMovieImages.Disc && !item.HasImage(ImageType.Disc))
            {
                var node = doc.SelectSingleNode("//fanart/movie/moviediscs/moviedisc[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/movie/moviediscs/moviedisc/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Disc, null, cancellationToken)
                                        .ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMovieImages.Banner && !item.HasImage(ImageType.Banner))
            {
                var node = doc.SelectSingleNode("//fanart/movie/moviebanners/moviebanner[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/movie/moviebanners/moviebanner/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Banner, null, cancellationToken)
                                        .ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (ConfigurationManager.Configuration.DownloadMovieImages.Thumb && !item.HasImage(ImageType.Thumb))
            {
                var node = doc.SelectSingleNode("//fanart/movie/moviethumbs/moviethumb[@lang = \"" + language + "\"]/@url") ??
                           doc.SelectSingleNode("//fanart/movie/moviethumbs/moviethumb/@url");
                path = node != null ? node.Value : null;
                if (!string.IsNullOrEmpty(path))
                {
                    await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Thumb, null, cancellationToken)
                                        .ConfigureAwait(false);
                }
            }

            if (ConfigurationManager.Configuration.DownloadMovieImages.Backdrops && item.BackdropImagePaths.Count == 0)
            {
                var nodes = doc.SelectNodes("//fanart/movie/moviebackgrounds//@url");

                if (nodes != null)
                {
                    var numBackdrops = item.BackdropImagePaths.Count;

                    foreach (XmlNode node in nodes)
                    {
                        path = node.Value;

                        if (!string.IsNullOrEmpty(path))
                        {
                            await _providerManager.SaveImage(item, path, FanArtResourcePool, ImageType.Backdrop, numBackdrops, cancellationToken)
                                                .ConfigureAwait(false);

                            numBackdrops++;

                            if (item.BackdropImagePaths.Count >= ConfigurationManager.Configuration.MaxBackdrops) break;
                        }
                    }

                }
            }
        }
    }
}
