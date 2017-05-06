using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<Audio> GetInstantMixFromSong(Audio item, User user)
        {
            var list = new List<Audio>
            {
                item
            };

            return list.Concat(GetInstantMixFromGenres(item.Genres, user));
        }

        public IEnumerable<Audio> GetInstantMixFromArtist(MusicArtist item, User user)
        {
            return GetInstantMixFromGenres(item.Genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromAlbum(MusicAlbum item, User user)
        {
            return GetInstantMixFromGenres(item.Genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromFolder(Folder item, User user)
        {
            var genres = item
               .GetRecursiveChildren(user, new InternalItemsQuery(user)
               {
                   IncludeItemTypes = new[] { typeof(Audio).Name }
               })
               .Cast<Audio>()
               .SelectMany(i => i.Genres)
               .Concat(item.Genres)
               .DistinctNames();

            return GetInstantMixFromGenres(genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromPlaylist(Playlist item, User user)
        {
            return GetInstantMixFromGenres(item.Genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromGenres(IEnumerable<string> genres, User user)
        {
            var genreIds = genres.DistinctNames().Select(i =>
            {
                try
                {
                    return _libraryManager.GetMusicGenre(i).Id.ToString("N");
                }
                catch
                {
                    return null;
                }

            }).Where(i => i != null);

            return GetInstantMixFromGenreIds(genreIds, user);
        }

        public IEnumerable<Audio> GetInstantMixFromGenreIds(IEnumerable<string> genreIds, User user)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery(user)
            {
                IncludeItemTypes = new[] { typeof(Audio).Name },

                GenreIds = genreIds.ToArray(),

                Limit = 200,

                SortBy = new[] { ItemSortBy.Random }

            }).Cast<Audio>();
        }

        public IEnumerable<Audio> GetInstantMixFromItem(BaseItem item, User user)
        {
            var genre = item as MusicGenre;
            if (genre != null)
            {
                return GetInstantMixFromGenreIds(new[] { item.Id.ToString("N") }, user);
            }

            var playlist = item as Playlist;
            if (playlist != null)
            {
                return GetInstantMixFromPlaylist(playlist, user);
            }

            var album = item as MusicAlbum;
            if (album != null)
            {
                return GetInstantMixFromAlbum(album, user);
            }

            var artist = item as MusicArtist;
            if (artist != null)
            {
                return GetInstantMixFromArtist(artist, user);
            }

            var song = item as Audio;
            if (song != null)
            {
                return GetInstantMixFromSong(song, user);
            }

            var folder = item as Folder;
            if (folder != null)
            {
                return GetInstantMixFromFolder(folder, user);
            }

            return new Audio[] { };
        }
    }
}
