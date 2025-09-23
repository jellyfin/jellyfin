using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Class PeopleValidationTask.
/// </summary>
public class PeopleValidationTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleValidationTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="dbContextFactory">Instance of the <see cref="IDbContextFactory{TContext}"/> interface.</param>
    public PeopleValidationTask(ILibraryManager libraryManager, ILocalizationManager localization, IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public string Name => _localization.GetLocalizedString("TaskRefreshPeople");

    /// <inheritdoc />
    public string Description => _localization.GetLocalizedString("TaskRefreshPeopleDescription");

    /// <inheritdoc />
    public string Category => _localization.GetLocalizedString("TasksLibraryCategory");

    /// <inheritdoc />
    public string Key => "RefreshPeople";

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <summary>
    /// Creates the triggers that define when the task will run.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{TaskTriggerInfo}"/> containing the default trigger infos for this task.</returns>
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromDays(7).Ticks
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        IProgress<double> subProgress = new Progress<double>((val) => progress.Report(val / 2));
        await _libraryManager.ValidatePeopleAsync(subProgress, cancellationToken).ConfigureAwait(false);

        subProgress = new Progress<double>((val) => progress.Report((val / 2) + 50));
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                var duplicates = await context.Peoples
                    .GroupBy(e => new { e.Name, e.PersonType })
                    .Where(e => e.Count() > 1)
                    .Select(e => new
                    {
                        Duplicates = e.Select(f => f.Id)
                    })
                    .ToArrayAsync(cancellationToken)
                    .ConfigureAwait(false);
                subProgress.Report(49);
                for (var i = 0; i < duplicates.Length; i++)
                {
                    var item = duplicates[i];
                    var reference = item.Duplicates.First();
                    var dups = item.Duplicates.Skip(1).ToArray();
                    await context.PeopleBaseItemMap.WhereOneOrMany(dups, e => e.PeopleId)
                        .ExecuteUpdateAsync(e => e.SetProperty(f => f.PeopleId, reference), cancellationToken)
                        .ConfigureAwait(false);
                    await context.Peoples.Where(e => item.Duplicates.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                    subProgress.Report(100 / duplicates.Length * i);
                }

                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                subProgress.Report(100);
            }
        }
    }
}
