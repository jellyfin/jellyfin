#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Querying;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;

namespace Emby.Server.Implementations.Library
{
    public class MusicManager : IMusicManager
    {
        private readonly ILibraryManager _libraryManager;

        public MusicManager(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public List<BaseItem> GetInstantMixFromSong(Audio item, User user, DtoOptions dtoOptions)
        {
            var list = new List<Audio>
            {
                item
            };

            return list.Concat(GetInstantMixFromGenres(item.Genres, user, dtoOptions)).ToList();
        }

        /// <inheritdoc />
        public List<BaseItem> GetInstantMixFromArtist(MusicArtist artist, User user, DtoOptions dtoOptions)
        {
            return GetInstantMixFromGenres(artist.Genres, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromAlbum(MusicAlbum item, User user, DtoOptions dtoOptions)
        {
            return GetInstantMixFromGenres(item.Genres, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromFolder(Folder item, User user, DtoOptions dtoOptions)
        {
            var genres = item
               .GetRecursiveChildren(user, new InternalItemsQuery(user)
               {
                   IncludeItemTypes = new[] { BaseItemKind.Audio },
                   DtoOptions = dtoOptions
               })
               .Cast<Audio>()
               .SelectMany(i => i.Genres)
               .Concat(item.Genres)
               .DistinctNames();

            return GetInstantMixFromGenres(genres, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromPlaylist(Playlist item, User user, DtoOptions dtoOptions)
        {
            return GetInstantMixFromGenres(item.Genres, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromGenres(IEnumerable<string> genres, User user, DtoOptions dtoOptions)
        {
            var genreIds = genres.DistinctNames().Select(i =>
            {
                try
                {
                    return _libraryManager.GetMusicGenre(i).Id;
                }
                catch
                {
                    return Guid.Empty;
                }
            }).Where(i => !i.Equals(default)).ToArray();

            return GetInstantMixFromGenreIds(genreIds, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromGenreIds(Guid[] genreIds, User user, DtoOptions dtoOptions)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { BaseItemKind.Audio },

                GenreIds = genreIds.ToArray(),

                Limit = 200,

                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },

                DtoOptions = dtoOptions
            });
        }

        public List<BaseItem> GetInstantMixFromItem(BaseItem item, User user, DtoOptions dtoOptions)
        {
            if (item is MusicGenre)
            {
                return GetInstantMixFromGenreIds(new[] { item.Id }, user, dtoOptions);
            }

            if (item is Playlist playlist)
            {
                return GetInstantMixFromPlaylist(playlist, user, dtoOptions);
            }

            if (item is MusicAlbum album)
            {
                return GetInstantMixFromAlbum(album, user, dtoOptions);
            }

            if (item is MusicArtist artist)
            {
                return GetInstantMixFromArtist(artist, user, dtoOptions);
            }

            if (item is Audio song)
            {
                return GetInstantMixFromSong(song, user, dtoOptions);
            }

            if (item is Folder folder)
            {
                return GetInstantMixFromFolder(folder, user, dtoOptions);
            }

            return new List<BaseItem>();
        }
    }
}
