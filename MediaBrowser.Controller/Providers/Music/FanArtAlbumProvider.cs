using System.Collections.Generic;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Music
{
    public class FanArtAlbumProvider : FanartBaseProvider
    {
        private readonly IProviderManager _providerManager;
        
        public FanArtAlbumProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            _providerManager = providerManager;
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

        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
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

            var cover = artist.AlbumCovers != null ? GetValueOrDefault(artist.AlbumCovers, mbid, null) : null;

            if (cover == null)
            {
                Logger.Warn("Unable to find cover art for {0}", item.Name);
                SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                return false;
            }

            item.SetImage(ImageType.Primary, await _providerManager.DownloadAndSaveImage(item, cover, "folder.jpg", FanArtResourcePool, cancellationToken).ConfigureAwait(false));
            return true;
        }

        /// <summary>
        /// Helper method for Dictionaries since they throw on not-found keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>``1.</returns>
        private static U GetValueOrDefault<T, U>(Dictionary<T, U> dictionary, T key, U defaultValue)
        {
            U val;
            if (!dictionary.TryGetValue(key, out val))
            {
                val = defaultValue;
            }
            return val;

        }
    }
}
