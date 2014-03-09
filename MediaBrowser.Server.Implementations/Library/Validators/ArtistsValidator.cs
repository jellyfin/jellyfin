using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
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

            var allSongs = allItems.OfType<Audio>().ToList();

            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * .8));

            var allArtists = await GetAllArtists(allSongs, cancellationToken, innerProgress).ConfigureAwait(false);

            progress.Report(80);

            var numComplete = 0;

            var numArtists = allArtists.Count;

            foreach (var artist in allArtists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Only do this for artists accessed by name. Folder-based artists get it from the normal refresh
                if (artist.IsAccessedByName && !artist.LockedFields.Contains(MetadataFields.Genres))
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
                percent /= numArtists;
                percent *= 20;

                progress.Report(80 + percent);
            }

            progress.Report(100);
        }

        /// <summary>
        /// Gets all artists.
        /// </summary>
        /// <param name="allSongs">All songs.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{Artist[]}.</returns>
        private async Task<List<MusicArtist>> GetAllArtists(IEnumerable<Audio> allSongs, CancellationToken cancellationToken, IProgress<double> progress)
        {
            var allArtists = allSongs.SelectMany(i => i.AllArtists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var returnArtists = new List<MusicArtist>(allArtists.Count);

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
    }
}
