#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

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

        public List<BaseItem> GetInstantMixFromArtist(MusicArtist item, User user, DtoOptions dtoOptions)
        {
            return GetInstantMixFromGenres(item.Genres, user, dtoOptions);
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
                   IncludeItemTypes = new[] { typeof(Audio).Name },
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

            }).Where(i => !i.Equals(Guid.Empty)).ToArray();

            return GetInstantMixFromGenreIds(genreIds, user, dtoOptions);
        }

        public List<BaseItem> GetInstantMixFromGenreIds(Guid[] genreIds, User user, DtoOptions dtoOptions)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Audio).Name },

                GenreIds = genreIds.ToArray(),

                Limit = 200,

                OrderBy = new[] { (ItemSortBy.Random, SortOrder.Ascending) },

                DtoOptions = dtoOptions
            });
        }

        public List<BaseItem> GetInstantMixFromItem(BaseItem item, User user, DtoOptions dtoOptions)
        {
            var genre = item as MusicGenre;
            if (genre != null)
            {
                return GetInstantMixFromGenreIds(new[] { item.Id }, user, dtoOptions);
            }

            var playlist = item as Playlist;
            if (playlist != null)
            {
                return GetInstantMixFromPlaylist(playlist, user, dtoOptions);
            }

            var album = item as MusicAlbum;
            if (album != null)
            {
                return GetInstantMixFromAlbum(album, user, dtoOptions);
            }

            var artist = item as MusicArtist;
            if (artist != null)
            {
                return GetInstantMixFromArtist(artist, user, dtoOptions);
            }

            var song = item as Audio;
            if (song != null)
            {
                return GetInstantMixFromSong(song, user, dtoOptions);
            }

            var folder = item as Folder;
            if (folder != null)
            {
                return GetInstantMixFromFolder(folder, user, dtoOptions);
            }

            return new List<BaseItem>();
        }
    }
}
