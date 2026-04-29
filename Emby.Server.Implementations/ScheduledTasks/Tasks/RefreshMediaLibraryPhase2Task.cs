using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Task that runs Phase 2 of the library scan: external metadata refresh.
/// </summary>
public class RefreshMediaLibraryPhase2Task : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshMediaLibraryPhase2Task" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    public RefreshMediaLibraryPhase2Task(ILibraryManager libraryManager, ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _localization = localization;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshLibraryPhase2");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshLibraryPhase2Description");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RefreshLibraryPhase2";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield break;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        progress.Report(0);

        await ((LibraryManager)_libraryManager).ValidateMediaLibraryPhase2Async(progress, cancellationToken).ConfigureAwait(false);
    }
}
