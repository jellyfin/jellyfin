using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class ArtistsValidator
    /// </summary>
    public class ArtistsValidator
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsPostScanTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger.</param>
        public ArtistsValidator(ILibraryManager libraryManager, IUserManager userManager, ILogger logger)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Runs the specified progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allItems = _libraryManager.RootFolder.GetRecursiveChildren();

            var allMusicArtists = allItems.OfType<MusicArtist>().ToList();
            var allSongs = allItems.OfType<Audio>().ToList();

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * .8));

            var allArtists = await GetAllArtists(allSongs, cancellationToken, innerProgress).ConfigureAwait(false);

            progress.Report(80);

            var numComplete = 0;

            var userLibraries = _userManager.Users
                .Select(i => new Tuple<Guid, List<IHasArtist>>(i.Id, i.RootFolder.GetRecursiveChildren(i).OfType<IHasArtist>().ToList()))
                .ToList();

            var numArtists = allArtists.Count;

            foreach (var artist in allArtists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                artist.ValidateImages();
                artist.ValidateBackdrops();

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

                // Populate counts of items
                //SetItemCounts(artist, null, allItems.OfType<IHasArtist>());

                foreach (var lib in userLibraries)
                {
                    SetItemCounts(artist, lib.Item1, lib.Item2);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numArtists;
                percent *= 20;

                progress.Report(80 + percent);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Sets the item counts.
        /// </summary>
        /// <param name="artist">The artist.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="allItems">All items.</param>
        private void SetItemCounts(Artist artist, Guid? userId, IEnumerable<IHasArtist> allItems)
        {
            var name = artist.Name;

            var items = allItems
                .Where(i => i.HasArtist(name))
                .ToList();

            var counts = new ItemByNameCounts
            {
                TotalCount = items.Count,

                SongCount = items.OfType<Audio>().Count(),

                AlbumCount = items.OfType<MusicAlbum>().Count(),

                MusicVideoCount = items.OfType<MusicVideo>().Count()
            };

            if (userId.HasValue)
            {
                artist.UserItemCounts[userId.Value] = counts;
            }
        }

        /// <summary>
        /// Merges the images.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        private void MergeImages(Dictionary<ImageType, string> source, Dictionary<ImageType, string> target)
        {
            foreach (var key in source.Keys
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
        /// <param name="allSongs">All songs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{Artist[]}.</returns>
        private async Task<List<Artist>> GetAllArtists(IEnumerable<Audio> allSongs, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var allArtists = allSongs
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
                .ToList();

            var returnArtists = new List<Artist>(allArtists.Count);

            var numComplete = 0;
            var numArtists = allArtists.Count;

            foreach (var artist in allArtists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var artistItem = _libraryManager.GetArtist(artist);

                    await artistItem.RefreshMetadata(cancellationToken).ConfigureAwait(false);

                    returnArtists.Add(artistItem);
                }
                catch (IOException ex)
                {
                    _logger.ErrorException("Error validating Artist {0}", ex, artist);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= numArtists;

                progress.Report(100 * percent);
            }

            return returnArtists;
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
