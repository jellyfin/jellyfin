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
/// Task that runs Phase 1 of the library scan: file discovery and local NFO metadata.
/// </summary>
public class RefreshMediaLibraryPhase1Task : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshMediaLibraryPhase1Task" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    public RefreshMediaLibraryPhase1Task(ILibraryManager libraryManager, ILocalizationManager localization)
    {
        _libraryManager = libraryManager;
        _localization = localization;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshLibraryPhase1");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshLibraryPhase1Description");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RefreshLibraryPhase1";

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

        await ((LibraryManager)_libraryManager).ValidateMediaLibraryPhase1Async(progress, cancellationToken).ConfigureAwait(false);
    }
}
