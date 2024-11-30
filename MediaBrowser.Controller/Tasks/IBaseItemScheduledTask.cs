using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Tasks;

namespace MediaBrowser.Controller;

/// <summary>
/// A scheduled task that can be run in the context of one or many base items.
/// </summary>
public interface IBaseItemScheduledTask : IScheduledTask
{
    /// <summary>
    /// Runs this task in the context of one or many base items.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="baseItems">The list of items that where changed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task ExecuteAsync(IProgress<double> progress, IReadOnlyList<(BaseItem Item, TaskActionTypes ChangeType)> baseItems, CancellationToken cancellationToken);
}
