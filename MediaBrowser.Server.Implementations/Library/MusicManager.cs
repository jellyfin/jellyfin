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
    }
}
