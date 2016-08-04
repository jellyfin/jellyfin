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
using CommonIO;

namespace MediaBrowser.Server.Implementations.Library.Validators
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

        private bool DownloadMetadata(PersonInfo i, PeopleMetadataOptions options)
        {
            if (i.IsType(PersonType.Actor))
            {
                return options.DownloadActorMetadata;
            }
            if (i.IsType(PersonType.Director))
            {
                return options.DownloadDirectorMetadata;
            }
            if (i.IsType(PersonType.Composer))
            {
                return options.DownloadComposerMetadata;
            }
            if (i.IsType(PersonType.Writer))
            {
                return options.DownloadWriterMetadata;
            }
            if (i.IsType(PersonType.Producer))
            {
                return options.DownloadProducerMetadata;
            }
            if (i.IsType(PersonType.GuestStar))
            {
                return options.DownloadGuestStarMetadata;
            }

            return options.DownloadOtherPeopleMetadata;
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

            var peopleOptions = _config.Configuration.PeopleMetadataOptions;

            var people = _libraryManager.GetPeople(new InternalPeopleQuery());

            var dict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            foreach (var person in people)
            {
                var isMetadataEnabled = DownloadMetadata(person, peopleOptions);

                bool currentValue;
                if (dict.TryGetValue(person.Name, out currentValue))
                {
                    if (!currentValue && isMetadataEnabled)
                    {
                        dict[person.Name] = true;
                    }
                }
                else
                {
                    dict[person.Name] = isMetadataEnabled;
                }
            }

            var numComplete = 0;

            _logger.Debug("Will refresh {0} people", dict.Count);

            foreach (var person in dict)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = _libraryManager.GetPerson(person.Key);

                    var hasMetdata = !string.IsNullOrWhiteSpace(item.Overview);
                    var performFullRefresh = !hasMetdata && (DateTime.UtcNow - item.DateLastRefreshed).TotalDays >= 90;
                    performFullRefresh = false;

                    var defaultMetadataRefreshMode = performFullRefresh
                        ? MetadataRefreshMode.FullRefresh
                        : MetadataRefreshMode.Default;

                    var imageRefreshMode = performFullRefresh
                        ? ImageRefreshMode.FullRefresh
                        : ImageRefreshMode.Default;

                    var options = new MetadataRefreshOptions(_fileSystem)
                    {
                        MetadataRefreshMode = person.Value ? defaultMetadataRefreshMode : MetadataRefreshMode.ValidationOnly,
                        ImageRefreshMode = person.Value ? imageRefreshMode : ImageRefreshMode.ValidationOnly
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
                percent /= people.Count;

                progress.Report(100 * percent);
            }

            progress.Report(100);

            _logger.Info("People validation complete");

            // Bad practice, i know. But we keep a lot in memory, unfortunately.
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);
        }
    }
}
