using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class SoundtrackPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;

        public SoundtrackPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => RunInternal(progress, cancellationToken));
        }

        private void RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allItems = _libraryManager.RootFolder
                .RecursiveChildren
                .ToList();

            var musicAlbums = allItems
                .OfType<MusicAlbum>()
                .ToList();

            AttachMovieSoundtracks(allItems, musicAlbums, cancellationToken);

            progress.Report(25);

            AttachTvSoundtracks(allItems, musicAlbums, cancellationToken);

            progress.Report(50);

            AttachGameSoundtracks(allItems, musicAlbums, cancellationToken);

            progress.Report(75);

            AttachAlbumLinks(allItems, musicAlbums, cancellationToken);

            progress.Report(100);
        }

        private void AttachMovieSoundtracks(IEnumerable<BaseItem> allItems, List<MusicAlbum> allAlbums, CancellationToken cancellationToken)
        {
            foreach (var movie in allItems
                .Where(i => (i is Movie) || (i is Trailer)))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tmdbId = movie.GetProviderId(MetadataProviders.Tmdb);

                if (string.IsNullOrEmpty(tmdbId))
                {
                    movie.SoundtrackIds = new List<Guid>();
                    continue;
                }

                movie.SoundtrackIds = allAlbums
                .Where(i => string.Equals(tmdbId, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Id)
                .ToList();
            }
        }

        private void AttachTvSoundtracks(IEnumerable<BaseItem> allItems, List<MusicAlbum> allAlbums, CancellationToken cancellationToken)
        {
            foreach (var series in allItems.OfType<Series>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tvdbId = series.GetProviderId(MetadataProviders.Tvdb);

                if (string.IsNullOrEmpty(tvdbId))
                {
                    series.SoundtrackIds = new List<Guid>();
                    continue;
                }

                series.SoundtrackIds = allAlbums
                .Where(i => string.Equals(tvdbId, i.GetProviderId(MetadataProviders.Tvdb), StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Id)
                .ToList();
            }
        }

        private void AttachGameSoundtracks(IEnumerable<BaseItem> allItems, List<MusicAlbum> allAlbums, CancellationToken cancellationToken)
        {
            foreach (var game in allItems.OfType<Game>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var gamesdb = game.GetProviderId(MetadataProviders.Gamesdb);

                if (string.IsNullOrEmpty(gamesdb))
                {
                    game.SoundtrackIds = new List<Guid>();
                    continue;
                }

                game.SoundtrackIds = allAlbums
                .Where(i => string.Equals(gamesdb, i.GetProviderId(MetadataProviders.Gamesdb), StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Id)
                .ToList();
            }
        }

        private void AttachAlbumLinks(List<BaseItem> allItems, IEnumerable<MusicAlbum> allAlbums, CancellationToken cancellationToken)
        {
            foreach (var album in allAlbums)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tmdb = album.GetProviderId(MetadataProviders.Tmdb);
                var tvdb = album.GetProviderId(MetadataProviders.Tvdb);
                var gamesdb = album.GetProviderId(MetadataProviders.Gamesdb);

                if (string.IsNullOrEmpty(tmdb) && string.IsNullOrEmpty(tvdb) && string.IsNullOrEmpty(gamesdb))
                {
                    album.SoundtrackIds = new List<Guid>();
                    continue;
                }

                album.SoundtrackIds = allItems.
                Where(i =>
                {
                    if (!string.IsNullOrEmpty(tmdb) && string.Equals(tmdb, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase) && i is Movie)
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(tmdb) && string.Equals(tmdb, i.GetProviderId(MetadataProviders.Tmdb), StringComparison.OrdinalIgnoreCase) && i is Trailer)
                    {
                        return true;
                    }
                    if (!string.IsNullOrEmpty(tvdb) && string.Equals(tvdb, i.GetProviderId(MetadataProviders.Tvdb), StringComparison.OrdinalIgnoreCase) && i is Series)
                    {
                        return true;
                    }

                    return !string.IsNullOrEmpty(gamesdb) && string.Equals(gamesdb, i.GetProviderId(MetadataProviders.Gamesdb), StringComparison.OrdinalIgnoreCase) && i is Game;
                })
                    .Select(i => i.Id)
                    .ToList();
            }
        }
    }
}
