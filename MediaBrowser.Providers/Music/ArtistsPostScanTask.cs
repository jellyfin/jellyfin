using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    /// <summary>
    /// Class ArtistsPostScanTask
    /// </summary>
    public class ArtistsPostScanTask : ILibraryPostScanTask
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsPostScanTask"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        public ArtistsPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allItems = _libraryManager.RootFolder.RecursiveChildren.ToList();

            var allArtists = await GetAllArtists(allItems).ConfigureAwait(false);

            progress.Report(10);

            var allMusicArtists = allItems.OfType<MusicArtist>().ToList();
            var allSongs = allItems.OfType<Audio>().ToList();

            var numComplete = 0;

            foreach (var artist in allArtists)
            {
                var musicArtist = FindMusicArtist(artist, allMusicArtists);

                if (musicArtist != null)
                {
                    MergeImages(musicArtist.Images, artist.Images);

                    // Merge backdrops
                    var backdrops = musicArtist.BackdropImagePaths.ToList();
                    backdrops.InsertRange(0, artist.BackdropImagePaths);
                    artist.BackdropImagePaths = backdrops.Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                if (!artist.LockedFields.Contains(MetadataFields.Genres))
                {
                    // Avoid implicitly captured closure
                    var artist1 = artist;

                    artist.Genres = allSongs.Where(i => i.HasArtist(artist1.Name))
                        .SelectMany(i => i.Genres)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }

                numComplete++;
                double percent = numComplete;
                percent /= allArtists.Length;
                percent *= 5;

                progress.Report(10 + percent);
            }

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(15 + pct * .85));

            await _libraryManager.ValidateArtists(cancellationToken, innerProgress).ConfigureAwait(false);
        }

        private void MergeImages(Dictionary<ImageType, string> source, Dictionary<ImageType, string> target)
        {
            foreach (var key in source.Keys
                .ToList()
                .Where(k => !target.ContainsKey(k)))
            {
                string path;

                if (source.TryGetValue(key, out path))
                {
                    target[key] = path;
                }
            }
        }

        /// <summary>
        /// Gets all artists.
        /// </summary>
        /// <param name="allItems">All items.</param>
        /// <returns>Task{Artist[]}.</returns>
        private Task<Artist[]> GetAllArtists(IEnumerable<BaseItem> allItems)
        {
            var itemsList = allItems.OfType<Audio>().ToList();

            var tasks = itemsList
                .SelectMany(i =>
                {
                    var list = new List<string>();

                    if (!string.IsNullOrEmpty(i.AlbumArtist))
                    {
                        list.Add(i.AlbumArtist);
                    }
                    list.AddRange(i.Artists);

                    return list;
                })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => _libraryManager.GetArtist(i));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Finds the music artist.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="allMusicArtists">All music artists.</param>
        /// <returns>MusicArtist.</returns>
        private static MusicArtist FindMusicArtist(Artist artist, IEnumerable<MusicArtist> allMusicArtists)
        {
            var musicBrainzId = artist.GetProviderId(MetadataProviders.Musicbrainz);

            return allMusicArtists.FirstOrDefault(i =>
            {
                if (!string.IsNullOrWhiteSpace(musicBrainzId) && string.Equals(musicBrainzId, i.GetProviderId(MetadataProviders.Musicbrainz), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return string.Compare(i.Name, artist.Name, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols) == 0;
            });
        }
    }
}
