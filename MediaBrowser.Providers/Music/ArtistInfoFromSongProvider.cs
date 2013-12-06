using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class ArtistInfoFromSongProvider : BaseMetadataProvider
    {
        public ArtistInfoFromSongProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicArtist;
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            var artist = (MusicArtist)item;

            if (!artist.IsAccessedByName)
            {
                // If song metadata has changed
                if (GetComparisonData(artist) != providerInfo.FileStamp)
                {
                    return true;
                }
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(MusicArtist artist)
        {
            var songs = artist.RecursiveChildren.OfType<Audio>().ToList();

            return GetComparisonData(songs);
        }

        private Guid GetComparisonData(IEnumerable<Audio> songs)
        {
            var genres = songs.SelectMany(i => i.Genres)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList();

            return string.Join(string.Empty, genres.OrderBy(i => i).ToArray()).GetMD5();
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var artist = (MusicArtist)item;

            if (!artist.IsAccessedByName)
            {
                var songs = artist.RecursiveChildren.OfType<Audio>().ToList();

                if (!item.LockedFields.Contains(MetadataFields.Genres))
                {
                    artist.Genres = songs.SelectMany(i => i.Genres)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                providerInfo.FileStamp = GetComparisonData(songs);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return TrueTaskResult;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }
    }
}
