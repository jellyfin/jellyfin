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
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Music
{
    public class AlbumMetadataService : MetadataService<MusicAlbum, AlbumInfo>
    {
        protected override async Task<ItemUpdateType> BeforeSave(MusicAlbum item, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = await base.BeforeSave(item, isFullRefresh, currentUpdateType).ConfigureAwait(false);

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.IsLocked)
                {
                    var songs = item.GetRecursiveChildren(i => i is Audio)
                        .Cast<Audio>()
                        .ToList();

                    if (!item.LockedFields.Contains(MetadataFields.Genres))
                    {
                        var currentList = item.Genres.ToList();

                        item.Genres = songs.SelectMany(i => i.Genres)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        if (currentList.Count != item.Genres.Count || !currentList.OrderBy(i => i).SequenceEqual(item.Genres.OrderBy(i => i), StringComparer.OrdinalIgnoreCase))
                        {
                            updateType = updateType | ItemUpdateType.MetadataEdit;
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
                            updateType = updateType | ItemUpdateType.MetadataEdit;
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
                                updateType = updateType | ItemUpdateType.MetadataEdit;
                            }
                        }
                    }

                    updateType = updateType | SetAlbumArtistFromSongs(item, songs);
                    updateType = updateType | SetArtistsFromSongs(item, songs);
                    updateType = updateType | SetDateFromSongs(item, songs);
                }
            }

            return updateType;
        }

        private ItemUpdateType SetAlbumArtistFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;
            
            var artists = songs
                .SelectMany(i => i.AlbumArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToList();

            if (!item.AlbumArtists.SequenceEqual(artists, StringComparer.OrdinalIgnoreCase))
            {
                item.AlbumArtists = artists;
                updateType = updateType | ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        private ItemUpdateType SetArtistsFromSongs(MusicAlbum item, IEnumerable<Audio> songs)
        {
            var updateType = ItemUpdateType.None;

            var artists = songs
                .SelectMany(i => i.Artists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(i => i)
                .ToList();

            if (!item.Artists.SequenceEqual(artists, StringComparer.OrdinalIgnoreCase))
            {
                item.Artists = artists;
                updateType = updateType | ItemUpdateType.MetadataEdit;
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
                updateType = updateType | ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        protected override void MergeData(MetadataResult<MusicAlbum> source, MetadataResult<MusicAlbum> target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            ProviderUtils.MergeBaseItemData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Artists.Count == 0)
            {
                targetItem.Artists = sourceItem.Artists;
            }
        }

        public AlbumMetadataService(IServerConfigurationManager serverConfigurationManager, ILogger logger, IProviderManager providerManager, IFileSystem fileSystem, IUserDataManager userDataManager, ILibraryManager libraryManager) : base(serverConfigurationManager, logger, providerManager, fileSystem, userDataManager, libraryManager)
        {
        }
    }
}
