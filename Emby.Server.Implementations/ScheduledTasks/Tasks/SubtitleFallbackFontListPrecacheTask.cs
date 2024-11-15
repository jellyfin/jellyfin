using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Precaches the mapping of fallback font file to font family name.
/// </summary>
public class SubtitleFallbackFontListPrecacheTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILocalizationManager _localization;
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ISubtitleManager _subtitleManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SubtitleFallbackFontListPrecacheTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleFallbackFontListPrecacheTask"/> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="subtitleManager">Instance of the <see cref="ISubtitleManager"/> interface.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="fileSystem">The filesystem.</param>
    public SubtitleFallbackFontListPrecacheTask(
        ILocalizationManager localization,
        IServerConfigurationManager serverConfigurationManager,
        ISubtitleManager subtitleManager,
        IFileSystem fileSystem,
        ILogger<SubtitleFallbackFontListPrecacheTask> logger)
    {
        _localization = localization;
        _serverConfigurationManager = serverConfigurationManager;
        _subtitleManager = subtitleManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskSubtitleFallbackFontListPrecache");

    /// <inheritdoc />
    public string Key => "SubtitleFallbackFontListPrecache";

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskSubtitleFallbackFontListPrecacheDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public bool IsHidden => true;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var fonts = _subtitleManager.GetFallbackFontList(progress, cancellationToken);
        _logger.LogDebug("Precached {Count} fallback font family names", fonts.Count());
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[] { new TaskTriggerInfo() { Type = TaskTriggerInfo.TriggerStartup } };
    }
}
