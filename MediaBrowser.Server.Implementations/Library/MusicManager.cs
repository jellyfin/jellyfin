using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library
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

        public IEnumerable<Audio> GetInstantMixFromArtist(string name, User user)
        {
            var artist = _libraryManager.GetArtist(name);

            var genres = user.RootFolder
                .GetRecursiveChildren(user, i => i is Audio)
                .Cast<Audio>()
                .Where(i => i.HasArtist(name))
                .SelectMany(i => i.Genres)
                .Concat(artist.Genres)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return GetInstantMixFromGenres(genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromAlbum(MusicAlbum item, User user)
        {
            var genres = item
                .GetRecursiveChildren(user, i => i is Audio)
               .Cast<Audio>()
               .SelectMany(i => i.Genres)
               .Concat(item.Genres)
               .Distinct(StringComparer.OrdinalIgnoreCase);

            return GetInstantMixFromGenres(genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromPlaylist(Playlist item, User user)
        {
            var genres = item
               .GetRecursiveChildren(user, i => i is Audio)
               .Cast<Audio>()
               .SelectMany(i => i.Genres)
               .Concat(item.Genres)
               .Distinct(StringComparer.OrdinalIgnoreCase);

            return GetInstantMixFromGenres(genres, user);
        }

        public IEnumerable<Audio> GetInstantMixFromGenres(IEnumerable<string> genres, User user)
        {
            var inputItems = user.RootFolder
                .GetRecursiveChildren(user, i => i is Audio);

            var genresDictionary = genres.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

            return inputItems
                .Cast<Audio>()
                .Select(i => new Tuple<Audio, int>(i, i.Genres.Count(genresDictionary.ContainsKey)))
                .Where(i => i.Item2 > 0)
                .OrderByDescending(i => i.Item2)
                .ThenBy(i => Guid.NewGuid())
                .Select(i => i.Item1)
                .Take(100)
                .OrderBy(i => Guid.NewGuid());
        }

        public IEnumerable<Audio> GetInstantMixFromItem(BaseItem item, User user)
        {
            var genre = item as MusicGenre;
            if (genre != null)
            {
                return GetInstantMixFromGenres(new[] { item.Name }, user);
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
                return GetInstantMixFromArtist(artist.Name, user);
            }

            var song = item as Audio;
            if (song != null)
            {
                return GetInstantMixFromSong(song, user);
            }
            
            return new Audio[] { };
        }
    }
}
