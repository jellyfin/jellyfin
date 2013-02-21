using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Net;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.TV
{
    [Export(typeof(BaseMetadataProvider))]
    class FanArtTVProvider : FanartBaseProvider
    {
        protected string FanArtBaseUrl = "http://api.fanart.tv/webservice/series/{0}/{1}/xml/all/1/1";

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


            return (!artExists && Kernel.Instance.Configuration.DownloadTVArt)
                || (!logoExists && Kernel.Instance.Configuration.DownloadTVLogo)
                || (!thumbExists && Kernel.Instance.Configuration.DownloadTVThumb);
        }

        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var series = (Series)item;
            if (ShouldFetch(series, series.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo { ProviderId = Id })))
            {
                string language = Kernel.Instance.Configuration.PreferredMetadataLanguage.ToLower();
                string url = string.Format(FanArtBaseUrl, APIKey, series.GetProviderId(MetadataProviders.Tvdb));
                var doc = new XmlDocument();

                try
                {
                    using (var xml = await Kernel.Instance.HttpManager.Get(url, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false))
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
                    if (Kernel.Instance.Configuration.DownloadTVLogo && !series.ResolveArgs.ContainsMetaFileByName(LOGO_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/clearlogos/clearlogo/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ClearLogo for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Logo, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, LOGO_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
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
                    
                    if (Kernel.Instance.Configuration.DownloadTVArt && !series.ResolveArgs.ContainsMetaFileByName(ART_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/cleararts/clearart[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/cleararts/clearart/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ClearArt for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Art, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, ART_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
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
                    
                    if (Kernel.Instance.Configuration.DownloadTVThumb && !series.ResolveArgs.ContainsMetaFileByName(THUMB_FILE))
                    {
                        var node = doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb[@lang = \"" + language + "\"]/@url") ??
                                   doc.SelectSingleNode("//fanart/series/tvthumbs/tvthumb/@url");
                        path = node != null ? node.Value : null;
                        if (!string.IsNullOrEmpty(path))
                        {
                            Logger.Debug("FanArtProvider getting ThumbArt for " + series.Name);
                            try
                            {
                                series.SetImage(ImageType.Disc, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(series, path, THUMB_FILE, Kernel.Instance.ResourcePools.FanArt, cancellationToken).ConfigureAwait(false));
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
