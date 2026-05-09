using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// Class PeopleValidationTask.
/// </summary>
public class PeopleValidationTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILocalizationManager _localization;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PeopleValidationTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleValidationTask" /> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    /// <param name="dbContextFactory">Instance of the <see cref="IDbContextFactory{TContext}"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{PeopleValidationTask}"/> interface.</param>
    public PeopleValidationTask(
        ILibraryManager libraryManager,
        ILocalizationManager localization,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        IFileSystem fileSystem,
        ILogger<PeopleValidationTask> logger)
    {
        _libraryManager = libraryManager;
        _localization = localization;
        _dbContextFactory = dbContextFactory;
        _fileSystem = fileSystem;
        _logger = logger;
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
        // People validation performs heavy database writes that contend with an active library scan.
        // Defer it until the scan has finished; the task will run again on its next trigger.
        if (_libraryManager.IsScanRunning)
        {
            _logger.LogInformation("Skipping people validation because a library scan is currently running.");
            return;
        }

        // Phase 1: Deduplicate and remove orphaned people (0-33%)
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            IProgress<double> subProgress = new Progress<double>((val) => progress.Report(val / 3));
            var dupQuery = context.Peoples
                    .GroupBy(e => new { e.Name, e.PersonType })
                    .Where(e => e.Count() > 1)
                    .Select(e => e.Select(f => f.Id).ToArray());

            var total = dupQuery.Count();

            const int PartitionSize = 100;
            var iterator = 0;
            int itemCounter;
            var buffer = ArrayPool<Guid[]>.Shared.Rent(PartitionSize)!;
            try
            {
                do
                {
                    itemCounter = 0;
                    await foreach (var item in dupQuery
                        .Take(PartitionSize)
                        .AsAsyncEnumerable()
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false))
                    {
                        buffer[itemCounter++] = item;
                    }

                    for (int i = 0; i < itemCounter; i++)
                    {
                        var item = buffer[i];
                        var reference = item[0];
                        var dups = item[1..];
                        await context.PeopleBaseItemMap.WhereOneOrMany(dups, e => e.PeopleId)
                            .ExecuteUpdateAsync(e => e.SetProperty(f => f.PeopleId, reference), cancellationToken)
                            .ConfigureAwait(false);
                        await context.Peoples.Where(e => dups.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
                        subProgress.Report(100f / total * ((iterator * PartitionSize) + i));
                    }

                    iterator++;
                } while (itemCounter == PartitionSize && !cancellationToken.IsCancellationRequested);
            }
            finally
            {
                ArrayPool<Guid[]>.Shared.Return(buffer);
            }

            var peopleToDelete = await context.Peoples
                .Where(p => !context.PeopleBaseItemMap.Any(m => m.PeopleId.Equals(p.Id)))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} orphaned people.", peopleToDelete);

            subProgress.Report(100);
        }

        // Phase 2: Validate people (33-66%). Runs after orphaned PeopleBaseItemMap entries are
        // cleaned up above, so dead people are removed in a single pass instead of requiring a second run.
        IProgress<double> validateProgress = new Progress<double>((val) => progress.Report((val / 3) + 33));
        await _libraryManager.ValidatePeopleAsync(validateProgress, cancellationToken).ConfigureAwait(false);

        // Phase 3: Refresh images for people missing them (66-100%)
        IProgress<double> refreshProgress = new Progress<double>((val) => progress.Report((val / 3) + 66));
        await RefreshPeopleImagesAsync(refreshProgress, cancellationToken).ConfigureAwait(false);

        progress.Report(100);
    }

    private async Task RefreshPeopleImagesAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var personTypeName = typeof(Person).FullName!;

        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var people = await context.BaseItems
                .AsNoTracking()
                .Where(b => b.Type == personTypeName)
                .Where(b => b.DateLastRefreshed == null || b.DateLastRefreshed < thirtyDaysAgo)
                .Where(b =>
                    !b.Images!.Any(i => i.ImageType == ImageInfoImageType.Primary) ||
                    string.IsNullOrEmpty(b.Overview))
                .Select(b => new
                {
                    b.Id,
                    HasImage = b.Images!.Any(i => i.ImageType == ImageInfoImageType.Primary),
                    HasOverview = !string.IsNullOrEmpty(b.Overview)
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var numPeople = people.Count;
            var numComplete = 0;
            var numRefreshed = 0;

            _logger.LogDebug("Found {Count} people needing image/overview refresh", numPeople);

            foreach (var entry in people)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (_libraryManager.GetItemById(entry.Id) is not Person item)
                    {
                        continue;
                    }

                    var options = new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                    {
                        ImageRefreshMode = entry.HasImage ? MetadataRefreshMode.ValidationOnly : MetadataRefreshMode.Default,
                        MetadataRefreshMode = entry.HasOverview ? MetadataRefreshMode.ValidationOnly : MetadataRefreshMode.Default
                    };

                    await item.RefreshMetadata(options, cancellationToken).ConfigureAwait(false);
                    numRefreshed++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing images for person {PersonId}", entry.Id);
                }

                numComplete++;
                progress.Report(100.0 * numComplete / numPeople);
            }

            _logger.LogInformation("Refreshed metadata for {Count} people missing images or overview", numRefreshed);
        }
    }
}
