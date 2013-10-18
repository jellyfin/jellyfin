using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        private readonly IEnumerable<IPeoplePrescanTask> _prescanTasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="PeopleValidator" /> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="prescanTasks">The prescan tasks.</param>
        /// <param name="logger">The logger.</param>
        public PeopleValidator(ILibraryManager libraryManager, IEnumerable<IPeoplePrescanTask> prescanTasks, ILogger logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _prescanTasks = prescanTasks;
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

            // Run prescan tasks
            await RunPrescanTasks(innerProgress, cancellationToken).ConfigureAwait(false);

            progress.Report(15);
            
            var people = _libraryManager.RootFolder.GetRecursiveChildren()
                .SelectMany(c => c.People)
                .DistinctBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var numComplete = 0;

            foreach (var person in people)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var item = _libraryManager.GetPerson(person.Name);

                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error validating IBN entry {0}", ex, person.Name);
                }

                // Update progress
                numComplete++;
                double percent = numComplete;
                percent /= people.Count;

                progress.Report(15 + 85 * percent);
            }

            progress.Report(100);

            _logger.Info("People validation complete");

            // Bad practice, i know. But we keep a lot in memory, unfortunately.
            GC.Collect(2, GCCollectionMode.Forced, true);
            GC.Collect(2, GCCollectionMode.Forced, true);
        }

        /// <summary>
        /// Runs the prescan tasks.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RunPrescanTasks(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var tasks = _prescanTasks.ToList();

            var numComplete = 0;
            var numTasks = tasks.Count;

            foreach (var task in tasks)
            {
                var innerProgress = new ActionableProgress<double>();

                // Prevent access to modified closure
                var currentNumComplete = numComplete;

                innerProgress.RegisterAction(pct =>
                {
                    double innerPercent = (currentNumComplete * 100) + pct;
                    innerPercent /= numTasks;
                    progress.Report(innerPercent);
                });

                try
                {
                    await task.Run(innerProgress, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.Info("Pre-scan task cancelled: {0}", task.GetType().Name);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error running pre-scan task", ex);
                }

                numComplete++;
                double percent = numComplete;
                percent /= numTasks;
                progress.Report(percent * 100);
            }

            progress.Report(100);
        }
    }
}
