using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    class FanArtTvProvider : FanartBaseProvider
    {
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/series/{0}/{1}/xml/all/1/1";

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        public FanArtTvProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            HttpClient = httpClient;
        }

        public override bool Supports(BaseItem item)
        {
            return item is Series;
        }

        protected override bool ShouldFetch(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta || string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.Tvdb))) return false; //nothing to do
            var artExists = item.ResolveArgs.ContainsMetaFileByName(ART_FILE);
            var logoExists = item.ResolveArgs.ContainsMetaFileByName(LOGO_FILE);
            var thumbExists = item.ResolveArgs.ContainsMetaFileByName(THUMB_FILE);


            return (!artExists && ConfigurationManager.Configuration.DownloadTVArt)
                || (!logoExists && ConfigurationManager.Configuration.DownloadTVLogo)
                || (!thumbExists && ConfigurationManager.Configuration.DownloadTVThumb);
        }

        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var series = (Series)item;
            if (ShouldFetch(series, series.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo { ProviderId = Id })))
            {
                string language = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower();
                string url = string.Format(FanArtBaseUrl, APIKey, series.GetProviderId(MetadataProviders.Tvdb));
                var doc = new XmlDocument();

                try
                {
                    using (var xml = await HttpClient.Get(url, FanArtMovieProvider.Current.FanArtResourcePool, cancellationToken).ConfigureAwait(false))
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
                    if (ConfigurationManager.Configuration.DownloadTVLogo && !series.ResolveArgs.ContainsMetaFileByName(LOGO_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ClearLogo for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Logo, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, LOGO_FILE, FanArtMovieProvider.Current.FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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

                    if (ConfigurationManager.Configuration.DownloadTVArt && !series.ResolveArgs.ContainsMetaFileByName(ART_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/cleararts/clearart[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/cleararts/clearart/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ClearArt for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Art, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, ART_FILE, FanArtMovieProvider.Current.FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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

                    if (ConfigurationManager.Configuration.DownloadTVThumb && !series.ResolveArgs.ContainsMetaFileByName(THUMB_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ThumbArt for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Disc, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, THUMB_FILE, FanArtMovieProvider.Current.FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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
            SetLastRefreshed(series, DateTime.UtcNow);
            return true;
        }
    }
}
