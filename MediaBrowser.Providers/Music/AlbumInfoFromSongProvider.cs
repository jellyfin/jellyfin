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
    public class AlbumInfoFromSongProvider : BaseMetadataProvider
    {
        public AlbumInfoFromSongProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
        }

        public override bool Supports(BaseItem item)
        {
            return item is MusicAlbum;
        }

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "2";
            }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            // If song metadata has changed
            if (GetComparisonData((MusicAlbum)item) != providerInfo.FileStamp)
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="album">The album.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(MusicAlbum album)
        {
            var songs = album.RecursiveChildren.OfType<Audio>().ToList();

            return GetComparisonData(songs);
        }

        private Guid GetComparisonData(List<Audio> songs)
        {
            var albumArtistNames = songs.Select(i => i.AlbumArtist)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var studios = songs.SelectMany(i => i.Studios)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList();

            var genres = songs.SelectMany(i => i.Genres)
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .ToList();

            albumArtistNames.AddRange(studios);
            albumArtistNames.AddRange(genres);

            return string.Join(string.Empty, albumArtistNames.OrderBy(i => i).ToArray()).GetMD5();
        }

        public override Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            var album = (MusicAlbum)item;

            var songs = album.RecursiveChildren.OfType<Audio>().ToList();

            if (!item.LockedFields.Contains(MetadataFields.Name))
            {
                var name = songs.Select(i => i.Album).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                if (!string.IsNullOrEmpty(name))
                {
                    album.Name = name;
                }
            }

            if (!item.LockedFields.Contains(MetadataFields.Studios))
            {
                album.Studios = songs.SelectMany(i => i.Studios)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            if (!item.LockedFields.Contains(MetadataFields.Genres))
            {
                album.Genres = songs.SelectMany(i => i.Genres)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            } 
            
            album.AlbumArtist = songs
                .Select(i => i.AlbumArtist)
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            album.Artists = songs.SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var date = songs.Select(i => i.PremiereDate)
                .FirstOrDefault(i => i.HasValue);

            if (date.HasValue)
            {
                album.PremiereDate = date.Value;
                album.ProductionYear = date.Value.Year;
            }
            else
            {
                var year = songs.Select(i => i.ProductionYear ?? 1800).FirstOrDefault(i => i != 1800);

                if (year != 1800)
                {
                    album.ProductionYear = year;
                }
            }


            providerInfo.FileStamp = GetComparisonData(songs);

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return TrueTaskResult;
        }

        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }
    }
}
