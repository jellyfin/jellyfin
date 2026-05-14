using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Options;

namespace Jellyfin.LiveTv.Guide;

/// <summary>
/// The "Refresh Guide" scheduled task.
/// </summary>
public class RefreshGuideScheduledTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILiveTvManager _liveTvManager;
    private readonly IGuideManager _guideManager;
    private readonly IOptionsMonitor<LiveTvOptions> _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshGuideScheduledTask"/> class.
    /// </summary>
    /// <param name="liveTvManager">The live tv manager.</param>
    /// <param name="guideManager">The guide manager.</param>
    /// <param name="config">The configuration manager.</param>
    public RefreshGuideScheduledTask(
        ILiveTvManager liveTvManager,
        IGuideManager guideManager,
        IOptionsMonitor<LiveTvOptions> config)
    {
        _liveTvManager = liveTvManager;
        _guideManager = guideManager;
        _config = config;
    }

    /// <inheritdoc />
    public string Name => "Refresh Guide";

    /// <inheritdoc />
    public string Description => "Downloads channel information from live tv services.";

    /// <inheritdoc />
    public string Category => "Live TV";

    /// <inheritdoc />
    public bool IsHidden => _liveTvManager.Services.Count == 1 && _config.CurrentValue.TunerHosts.Length == 0;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public string Key => "RefreshGuide";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => _guideManager.RefreshGuide(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            }
        };
    }
}
