using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Movies
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

        public FanArtMovieProvider(IHttpClient httpClient, ILogManager logManager)
            : base(logManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
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
            return item is Movie || item is BoxSet;
        }

        /// <summary>
        /// Shoulds the fetch.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool ShouldFetch(BaseItem item, BaseProviderInfo providerInfo)
        {
            var baseItem = item;
            if (item.Path == null || item.DontFetchMeta || string.IsNullOrEmpty(baseItem.GetProviderId(MetadataProviders.Tmdb))) return false; //nothing to do
            var artExists = item.ResolveArgs.ContainsMetaFileByName(ART_FILE);
            var logoExists = item.ResolveArgs.ContainsMetaFileByName(LOGO_FILE);
            var discExists = item.ResolveArgs.ContainsMetaFileByName(DISC_FILE);

            return (!artExists && Kernel.Instance.Configuration.DownloadMovieArt)
                || (!logoExists && Kernel.Instance.Configuration.DownloadMovieLogo)
                || (!discExists && Kernel.Instance.Configuration.DownloadMovieDisc);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var movie = item;
            if (ShouldFetch(movie, movie.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo { ProviderId = Id })))
            {
                var language = Kernel.Instance.Configuration.PreferredMetadataLanguage.ToLower();
                var url = string.Format(FanArtBaseUrl, APIKey, movie.GetProviderId(MetadataProviders.Tmdb));
                var doc = new XmlDocument();

                try
                {
                    using (var xml = await HttpClient.Get(url, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false))
                    {
                        doc.Load(xml);
                    }
                }
                catch (HttpException)
                {
                }

                cancellationToken.ThrowIfCancellationRequested();

                if (doc.HasChildNodes)
                {
                    string path;
                    var hd = Kernel.Instance.Configuration.DownloadHDFanArt ? "hd" : "";
                    if (Kernel.Instance.Configuration.DownloadMovieLogo && !item.ResolveArgs.ContainsMetaFileByName(LOGO_FILE))
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
                            Logger.Debug("FanArtProvider getting ClearLogo for " + movie.Name);
                            try
                            {
                                movie.SetImage(ImageType.Logo, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(movie, path, LOGO_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
                            }
                            catch (HttpException)
                            {
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Kernel.Instance.Configuration.DownloadMovieArt && !item.ResolveArgs.ContainsMetaFileByName(ART_FILE))
                    {
                        var node =
                            doc.SelectSingleNode("//fanart/movie/moviearts/" + hd + "movieart[@lang = \"" + language + "\"]/@url") ??
                            doc.SelectSingleNode("//fanart/movie/moviearts/" + hd + "movieart/@url") ??
                            doc.SelectSingleNode("//fanart/movie/moviearts/movieart[@lang = \"" + language + "\"]/@url") ??
                            doc.SelectSingleNode("//fanart/movie/moviearts/movieart/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ClearArt for " + movie.Name);
                            try
                            {
                                movie.SetImage(ImageType.Art, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(movie, path, ART_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
                            }
                            catch (HttpException)
                            {
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Kernel.Instance.Configuration.DownloadMovieDisc && !item.ResolveArgs.ContainsMetaFileByName(DISC_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/movie/moviediscs/moviedisc[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/movie/moviediscs/moviedisc/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting DiscArt for " + movie.Name);
                            try
                            {
                                movie.SetImage(ImageType.Disc, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(movie, path, DISC_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
                            }
                            catch (HttpException)
                            {
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (Kernel.Instance.Configuration.DownloadMovieBanner && !item.ResolveArgs.ContainsMetaFileByName(BANNER_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/movie/moviebanners/moviebanner[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/movie/moviebanners/moviebanner/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting Banner for " + movie.Name);
                            try
                            {
                                movie.SetImage(ImageType.Banner, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(movie, path, BANNER_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
                            }
                            catch (HttpException)
                            {
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (Kernel.Instance.Configuration.DownloadMovieThumb && !item.ResolveArgs.ContainsMetaFileByName(THUMB_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/movie/moviethumbs/moviethumb[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/movie/moviethumbs/moviethumb/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting Banner for " + movie.Name);
                            try
                            {
                                movie.SetImage(ImageType.Thumb, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(movie, path, THUMB_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
                            }
                            catch (HttpException)
                            {
                            }
                            catch (IOException)
                            {

                            }
                        }
                    }
                }
            }
            SetLastRefreshed(movie, DateTime.UtcNow);
            return true;
        }
    }
}
