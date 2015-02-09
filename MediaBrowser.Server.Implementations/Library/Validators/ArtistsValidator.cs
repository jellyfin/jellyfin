using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
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
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistsPostScanTask" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public ArtistsValidator(ILibraryManager libraryManager, ILogger logger)
        {
            _libraryManager = libraryManager;
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
            var allSongs = _libraryManager.RootFolder
                .GetRecursiveChildren(i => !i.IsFolder && (i is IHasArtist))
                .Cast<IHasArtist>()
                .ToList();

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
        }
    }
}
