using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MediaBrowser.Controller.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Validators
{
    /// <summary>
    /// Class PeopleValidator
    /// </summary>
    public class PeopleValidator
    {
        /// <summary>
        /// The _library manager
        /// </summary>
        private readonly ILibraryManager _libraryManager;
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeopleValidator" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public PeopleValidator(ILibraryManager libraryManager, ILogger logger, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _config = config;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Validates the people.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public async Task ValidatePeople(CancellationToken cancellationToken, IProgress<double> progress)
        {
            var innerProgress = new ActionableProgress<double>();

            innerProgress.RegisterAction(pct => progress.Report(pct * .15));

            var people = _libraryManager.GetPeople(new InternalPeopleQuery());

            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var person in people)
            {
                dict[person.Name] = true;
            }

            var numComplete = 0;

            _logger.Debug("Will refresh {0} people", dict.Count);

            var numPeople = dict.Count;

            foreach (var person in dict)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = _libraryManager.GetPerson(person.Key);

                    var options = new MetadataRefreshOptions(_fileSystem)
                    {
                        ImageRefreshMode = ImageRefreshMode.ValidationOnly,
                        MetadataRefreshMode = MetadataRefreshMode.ValidationOnly
                    };

                    await item.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error validating IBN entry {0}", ex, person);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= numPeople;

                progress.Report(100 * percent);
            }

            progress.Report(100);

            _logger.Info("People validation complete");
        }
    }
}
