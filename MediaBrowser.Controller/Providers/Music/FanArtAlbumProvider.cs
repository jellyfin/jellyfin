using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Controller.Providers.Music
{
    public class FanArtAlbumProvider : FanartBaseProvider
    {
        public FanArtAlbumProvider(ILogManager logManager, IServerConfigurationManager configurationManager) : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum && item.Parent is MusicArtist;
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
            return string.IsNullOrEmpty(item.PrimaryImagePath) && 
                   DateTime.Today.Subtract(providerInfo.LastRefreshed).TotalDays > ConfigurationManager.Configuration.MetadataRefreshDays;
        }

        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var mbid = item.GetProviderId(MetadataProviders.Musicbrainz);
            if (mbid == null)
            {
                Logger.Warn("No Musicbrainz id associated with album {0}", item.Name);
                SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            //Look at our parent for our album cover
            var artist = (MusicArtist)item.Parent;
            var cover = artist.AlbumCovers != null ? artist.AlbumCovers.GetValueOrDefault(mbid, null) : null;
            if (cover == null)
            {
                // Not there - maybe it is new since artist last refreshed so refresh it and try again
                await artist.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                cover = artist.AlbumCovers != null ? artist.AlbumCovers.GetValueOrDefault(mbid, null) : null;
            }
            if (cover == null)
            {
                Logger.Warn("Unable to find cover art for {0}", item.Name);
                SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                return false;
            }

            item.SetImage(ImageType.Primary, await Kernel.Instance.ProviderManager.DownloadAndSaveImage(item, cover, "folder.jpg", FanArtResourcePool, cancellationToken).ConfigureAwait(false));
            return true;
        }
    }
}
