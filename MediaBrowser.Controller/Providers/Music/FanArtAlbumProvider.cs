using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Controller.Providers.Music
{
    public class FanArtAlbumProvider : FanartBaseProvider
    {
        private readonly IProviderManager _providerManager;

        protected IHttpClient HttpClient { get; private set; }

        public FanArtAlbumProvider(IHttpClient httpClient, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
            HttpClient = httpClient;
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if we need refreshing, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            //we fetch if image needed and haven't already tried recently
            return (string.IsNullOrEmpty(item.PrimaryImagePath) || !item.HasImage(ImageType.Disc)) &&
                   DateTime.Today.Subtract(providerInfo.LastRefreshed).TotalDays > ConfigurationManager.Configuration.MetadataRefreshDays;
        }

        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var mbid = item.GetProviderId(MetadataProviders.Musicbrainz);
            if (mbid == null)
            {
                Logger.Warn("No Musicbrainz id associated with album {0}", item.Name);
                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var url = string.Format("http://api.fanart.tv/webservice/album/{0}/{1}/xml/all/1/1", APIKey, item.GetProviderId(MetadataProviders.Musicbrainz));

            var doc = new XmlDocument();

            try
            {
                using (var xml = await HttpClient.Get(url, FanArtResourcePool, cancellationToken).ConfigureAwait(false))
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
                if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Disc && !item.ResolveArgs.ContainsMetaFileByName(DISC_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/music/albums/album//cdart/@url");

                    var path = node != null ? node.Value : null;

                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting Disc for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Disc, await _providerManager.DownloadAndSaveImage(item, path, DISC_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }
                }

                if (ConfigurationManager.Configuration.DownloadMusicAlbumImages.Primary && !item.ResolveArgs.ContainsMetaFileByName(PRIMARY_FILE))
                {
                    var node = doc.SelectSingleNode("//fanart/music/albums/album//albumcover/@url");

                    var path = node != null ? node.Value : null;

                    if (!string.IsNullOrEmpty(path))
                    {
                        Logger.Debug("FanArtProvider getting albumcover for " + item.Name);
                        try
                        {
                            item.SetImage(ImageType.Primary, await _providerManager.DownloadAndSaveImage(item, path, PRIMARY_FILE, ConfigurationManager.Configuration.SaveLocalMeta, FanArtResourcePool, cancellationToken).ConfigureAwait(false));
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

            SetLastRefreshed(item, DateTime.UtcNow);

            return true;
        }
    }
}
