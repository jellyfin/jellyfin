using System;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Localization;
using Emby.Server.Implementations.ScheduledTasks.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.EntryPoints;

/// <summary>
/// <see cref="IHostedService"/> that queues <see cref="RecomputeParentalRatingScoresTask"/> when the stored
/// parental rating scores no longer match the rating data or the metadata countries.
/// </summary>
/// <remarks>
/// The check runs once the server finished starting up, after the server or a library configuration was saved,
/// and when a recompute completed successfully (a change made during that run is then picked up). Neither
/// configuration event carries the previous values, so the current state is compared with the state stored by
/// the last completed recompute instead. Configuration events are coalesced: the check runs once, a fixed
/// window after the first event of a burst. A failed or cancelled run is not retried by itself, it is retried by
/// the next check.
/// </remarks>
public sealed class RatingsRecomputeHost : IHostedService, IDisposable
{
    private static readonly TimeSpan _startupPollInterval = TimeSpan.FromMilliseconds(500);

    private readonly IServerConfigurationManager _configurationManager;
    private readonly IServerApplicationHost _applicationHost;
    private readonly ITaskManager _taskManager;
    private readonly RatingsStateStore _stateStore;
    private readonly ILogger<RatingsRecomputeHost> _logger;
    private readonly Lock _syncLock = new();

    private CancellationTokenSource? _stopping;
    private Timer? _evaluationTimer;
    private int _evaluationCount;
    private TaskCompletionSource _evaluated = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Initializes a new instance of the <see cref="RatingsRecomputeHost"/> class.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="applicationHost">The <see cref="IServerApplicationHost"/>.</param>
    /// <param name="taskManager">The <see cref="ITaskManager"/>.</param>
    /// <param name="stateStore">The <see cref="RatingsStateStore"/>.</param>
    /// <param name="logger">The <see cref="ILogger{RatingsRecomputeHost}"/>.</param>
    public RatingsRecomputeHost(
        IServerConfigurationManager configurationManager,
        IServerApplicationHost applicationHost,
        ITaskManager taskManager,
        RatingsStateStore stateStore,
        ILogger<RatingsRecomputeHost> logger)
    {
        _configurationManager = configurationManager;
        _applicationHost = applicationHost;
        _taskManager = taskManager;
        _stateStore = stateStore;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets how long after the first configuration change of a burst the state is checked.
    /// </summary>
    internal TimeSpan CoalesceDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the number of state checks that ran.
    /// </summary>
    internal int EvaluationCount => Volatile.Read(ref _evaluationCount);

    /// <summary>
    /// Gets a value indicating whether a state check is scheduled after a configuration change.
    /// </summary>
    internal bool IsEvaluationScheduled
    {
        get
        {
            lock (_syncLock)
            {
                return _evaluationTimer is not null;
            }
        }
    }

    /// <summary>
    /// Gets the startup check, which waits for the scheduled tasks to be available.
    /// </summary>
    internal Task? StartupCheck { get; private set; }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stopping = new CancellationTokenSource();
        _configurationManager.ConfigurationUpdated += OnConfigurationUpdated;
        CollectionFolder.LibraryOptionsUpdated += OnLibraryOptionsUpdated;
        _taskManager.TaskCompleted += OnTaskCompleted;

        // Hosted services start before the scheduled tasks are added to the task manager and before the
        // startup tasks completed, so the check cannot run here.
        StartupCheck = CheckAfterStartupAsync(_stopping.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _configurationManager.ConfigurationUpdated -= OnConfigurationUpdated;
        CollectionFolder.LibraryOptionsUpdated -= OnLibraryOptionsUpdated;
        _taskManager.TaskCompleted -= OnTaskCompleted;

        Timer? timer;
        lock (_syncLock)
        {
            timer = _evaluationTimer;
            _evaluationTimer = null;
        }

        if (_stopping is not null)
        {
            await _stopping.CancelAsync().ConfigureAwait(false);
        }

        if (timer is not null)
        {
            await timer.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _evaluationTimer?.Dispose();
        _stopping?.Dispose();
    }

    private async Task CheckAfterStartupAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!_applicationHost.CoreStartupHasCompleted)
            {
                await Task.Delay(_startupPollInterval, cancellationToken).ConfigureAwait(false);
            }

            Evaluate();
        }
        catch (OperationCanceledException)
        {
            // The server is stopping.
        }
    }

    internal void OnConfigurationUpdated(object? sender, EventArgs e) => ScheduleEvaluation();

    internal void OnLibraryOptionsUpdated(object? sender, LibraryOptionsUpdatedEventArgs e) => ScheduleEvaluation();

    private void OnTaskCompleted(object? sender, TaskCompletionEventArgs e)
    {
        // The worker is idle again at this point, so the task can be queued. A change made during the run is
        // found here. After a failed or cancelled run nothing is queued from here, or a failing run would repeat.
        if (e.Task.ScheduledTask is RecomputeParentalRatingScoresTask && e.Result.Status == TaskCompletionStatus.Completed)
        {
            Evaluate();
        }
    }

    private void ScheduleEvaluation()
    {
        lock (_syncLock)
        {
            if (_stopping is null || _stopping.IsCancellationRequested)
            {
                return;
            }

            // The window runs from the first change of a burst and is not extended by the changes after it.
            _evaluationTimer ??= new Timer(
                _ => OnEvaluationTimerElapsed(),
                null,
                CoalesceDelay,
                Timeout.InfiniteTimeSpan);
        }
    }

    private void OnEvaluationTimerElapsed()
    {
        lock (_syncLock)
        {
            _evaluationTimer?.Dispose();
            _evaluationTimer = null;
        }

        // Before the startup check ran there is nothing to queue on, and that check covers the change.
        if (_applicationHost.CoreStartupHasCompleted && _stopping is { IsCancellationRequested: false })
        {
            Evaluate();
        }
    }

    /// <summary>
    /// Completes when the next state check finished.
    /// </summary>
    /// <returns>A task representing the next state check.</returns>
    internal Task WaitForNextEvaluationAsync()
    {
        lock (_syncLock)
        {
            return _evaluated.Task;
        }
    }

    private void Evaluate()
    {
        try
        {
            if (_stateStore.IsRecomputeNeeded())
            {
                _logger.LogInformation("The stored parental rating scores are out of date, queuing the Recompute Parental Rating Scores task");

                // If a run is in progress the request is dropped, the check after its completion finds the change.
                _taskManager.QueueIfNotRunning<RecomputeParentalRatingScoresTask>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check whether the parental rating scores need to be recomputed");
        }
        finally
        {
            Interlocked.Increment(ref _evaluationCount);
            TaskCompletionSource completed;
            lock (_syncLock)
            {
                completed = _evaluated;
                _evaluated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            completed.TrySetResult();
        }
    }
}
