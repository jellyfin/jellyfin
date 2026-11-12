using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.ShareLinks;
using MediaBrowser.Model.Tasks;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Deletes share links that are revoked or have expired.
/// </summary>
public class ShareLinkExpiryTask : IScheduledTask
{
    private readonly IShareLinkRepository _shareLinkRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShareLinkExpiryTask"/> class.
    /// </summary>
    /// <param name="shareLinkRepository">Instance of the <see cref="IShareLinkRepository"/> interface.</param>
    public ShareLinkExpiryTask(IShareLinkRepository shareLinkRepository)
    {
        _shareLinkRepository = shareLinkRepository;
    }

    /// <inheritdoc />
    public string Name => "Share link expiry";

    /// <inheritdoc />
    public string Key => "ShareLinkExpiry";

    /// <inheritdoc />
    public string Description => "Deletes revoked or expired share links.";

    /// <inheritdoc />
    public string Category => "Maintenance";

    /// <summary>
    /// Gets a value indicating whether the task is hidden from the task list.
    /// </summary>
    public bool IsHidden => false;

    /// <summary>
    /// Gets a value indicating whether the task is enabled.
    /// </summary>
    public bool IsEnabled => true;

    /// <summary>
    /// Gets a value indicating whether the task is logged.
    /// </summary>
    public bool IsLogged => true;

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await _shareLinkRepository.PurgeExpiredAsync(now, cancellationToken).ConfigureAwait(false);
        progress.Report(100);
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromDays(1).Ticks
        };
    }
}
