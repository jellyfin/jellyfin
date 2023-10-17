using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Metadata;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// A scheduled task that scans all media libraries.
/// </summary>
public class RefreshMediaLibraryTask : IScheduledTask
{
    private readonly ILogger<RefreshMediaLibraryTask> _logger;
    private readonly ILibraryRefreshManager _libraryRefreshManager;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly IItemRepository _itemRepository;
    private readonly ILocalizationManager _localization;
    private readonly ILibraryPostScanTask[] _postScanTasks;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshMediaLibraryTask" /> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="libraryRefreshManager">The <see cref="ILibraryRefreshManager"/>.</param>
    /// <param name="libraryMonitor">The <see cref="ILibraryMonitor"/>.</param>
    /// <param name="itemRepository">The <see cref="IItemRepository"/>.</param>
    /// <param name="localization">The <see cref="ILocalizationManager"/>.</param>
    /// <param name="postScanTasks">The <see cref="ILibraryPostScanTask"/>'s.</param>
    public RefreshMediaLibraryTask(
        ILogger<RefreshMediaLibraryTask> logger,
        ILibraryRefreshManager libraryRefreshManager,
        ILibraryMonitor libraryMonitor,
        IItemRepository itemRepository,
        ILocalizationManager localization,
        IEnumerable<ILibraryPostScanTask> postScanTasks)
    {
        _logger = logger;
        _libraryRefreshManager = libraryRefreshManager;
        _libraryMonitor = libraryMonitor;
        _itemRepository = itemRepository;
        _localization = localization;
        _postScanTasks = postScanTasks.ToArray();
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshLibrary");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshLibraryDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RefreshLibrary";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfo.TriggerInterval,
            IntervalTicks = TimeSpan.FromHours(12).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(progress);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _libraryMonitor.Stop();
            progress.Report(0);

            _logger.LogInformation("Validating media library");
            await _libraryRefreshManager.ValidateMediaLibrary(progress, cancellationToken).ConfigureAwait(false);

            progress.Report(96);

            var innerProgress = new ActionableProgress<double>();
            innerProgress.RegisterAction(pct => progress.Report(96 + (pct * .04)));

            await RunPostScanTasks(innerProgress, cancellationToken).ConfigureAwait(false);
            progress.Report(100);
        }
        finally
        {
            _libraryMonitor.Start();
        }
    }

    private async Task RunPostScanTasks(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var numComplete = 0;
        var numTasks = _postScanTasks.Length;

        foreach (var task in _postScanTasks)
        {
            var innerProgress = new ActionableProgress<double>();

            // Prevent access to modified closure
            var currentNumComplete = numComplete;

            innerProgress.RegisterAction(pct =>
            {
                double innerPercent = pct;
                innerPercent /= 100;
                innerPercent += currentNumComplete;

                innerPercent /= numTasks;
                innerPercent *= 100;

                progress.Report(innerPercent);
            });

            _logger.LogDebug("Running post-scan task {0}", task.GetType().Name);

            try
            {
                await task.Run(innerProgress, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Post-scan task cancelled: {0}", task.GetType().Name);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running post-scan task");
            }

            numComplete++;
            double percent = numComplete;
            percent /= numTasks;
            progress.Report(percent * 100);
        }

        _itemRepository.UpdateInheritedValues();
        progress.Report(100);
    }
}
