using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Providers.Manager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Providers.Music
{
    public class AlbumMetadataService : MetadataService<MusicAlbum, AlbumInfo>
    {
        public AlbumMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IProviderRepository providerRepo, IFileSystem fileSystem, IUserDataManager userDataManager) : base(serverConfigurationManager, logger, providerManager, providerRepo, fileSystem, userDataManager)
        {
        }

        /// <summary>
        /// Merges the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="lockedFields">The locked fields.</param>
        /// <param name="replaceData">if set to <c>true</c> [replace data].</param>
        /// <param name="mergeMetadataSettings">if set to <c>true</c> [merge metadata settings].</param>
        protected override void MergeData(MusicAlbum source, MusicAlbum target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            if (replaceData || target.Artists.Count == 0)
            {
                target.Artists = source.Artists;
            }
        }

        protected override ItemUpdateType BeforeSave(MusicAlbum item)
        {
            var updateType = base.BeforeSave(item);

            var songs = item.RecursiveChildren.OfType<Audio>().ToList();

            if (!item.IsLocked)
            {
                if (!item.LockedFields.Contains(MetadataFields.Genres))
                {
                    var currentList = item.Genres.ToList();

                    item.Genres = songs.SelectMany(i => i.Genres)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (currentList.Count != item.Genres.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Genres.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                    {
                        updateType = updateType | ItemUpdateType.MetadataDownload;
                    }
                }

                if (!item.LockedFields.Contains(MetadataFields.Studios))
                {
                    var currentList = item.Studios.ToList();

                    item.Studios = songs.SelectMany(i => i.Studios)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (currentList.Count != item.Studios.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Studios.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                    {
                        updateType = updateType | ItemUpdateType.MetadataDownload;
                    }
                }

                if (!item.LockedFields.Contains(MetadataFields.Name))
                {
                    var name = songs.Select(i => i.Album).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                    if (!string.IsNullOrEmpty(name))
                    {
                        if (!string.Equals(item.Name, name, StringComparison.Ordinal))
                        {
                            item.Name = name;
                            updateType = updateType | ItemUpdateType.MetadataDownload;
                        }
                    }
                }
            }

            updateType = updateType | SetAlbumArtistFromSongs(item, songs);
            updateType = updateType | SetArtistsFromSongs(item, songs);
            updateType = updateType | SetDateFromSongs(item, songs);

            return updateType;
        }

        private ItemUpdateType SetAlbumArtistFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;
            
            var albumArtists = songs
                .SelectMany(i => i.AlbumArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!item.AlbumArtists.SequenceEqual(albumArtists, StringComparer.OrdinalIgnoreCase))
            {
                item.AlbumArtists = albumArtists;
                updateType = updateType | ItemUpdateType.MetadataDownload;
            }

            return updateType;
        }

        private ItemUpdateType SetArtistsFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var currentList = item.Artists.ToList();

            item.Artists = songs.SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (currentList.Count != item.Artists.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Artists.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
            {
                updateType = updateType | ItemUpdateType.MetadataDownload;
            }

            return updateType;
        }

        private ItemUpdateType SetDateFromSongs(MusicAlbum item, List<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var date = songs.Select(i => i.PremiereDate)
                            .FirstOrDefault(i => i.HasValue);

            var originalPremiereDate = item.PremiereDate;
            var originalProductionYear = item.ProductionYear;

            if (date.HasValue)
            {
                item.PremiereDate = date.Value;
                item.ProductionYear = date.Value.Year;
            }
            else
            {
                var year = songs.Select(i => i.ProductionYear ?? 1800).FirstOrDefault(i => i != 1800);

                if (year != 1800)
                {
                    item.ProductionYear = year;
                }
            }

            if ((originalPremiereDate ?? DateTime.MinValue) != (item.PremiereDate ?? DateTime.MinValue) ||
                (originalProductionYear ?? -1) != (item.ProductionYear ?? -1))
            {
                updateType = updateType | ItemUpdateType.MetadataDownload;
            }

            return updateType;
        }
    }
}
