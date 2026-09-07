using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class PeopleValidator.
/// </summary>
public class PeopleValidator
{
    /// <summary>
    /// The library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<PeopleValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeopleValidator" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public PeopleValidator(ILibraryManager libraryManager, ILogger<PeopleValidator> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Validates the people.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        // Before the refresh below walks them: a credit no item maps to any more stands for nothing,
        // and while it is there the person it names cannot reach the dead-person sweep either.
        var numOrphaned = _libraryManager.DeleteOrphanedCredits();
        if (numOrphaned > 0)
        {
            _logger.LogInformation("Deleted {Amount} credits no item maps to", numOrphaned);
        }

        var names = _libraryManager.GetPeopleNames(new InternalPeopleQuery());
        var existingPersonIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person]
        }).ToHashSet();

        var (newNames, deadIds) = PartitionCreditsByPersonId(names, _libraryManager.GetPersonId, existingPersonIds);

        var numComplete = 0;
        var count = names.Count;
        var refreshed = 0;

        foreach (var name in names)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var item = _libraryManager.GetOrCreatePerson(name);
                var isNew = !existingPersonIds.Contains(item.Id);
                var neverRefreshed = item.DateLastRefreshed == default;

                if (isNew || neverRefreshed)
                {
                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                    refreshed++;
                }
            }
            catch (OperationCanceledException)
            {
                // Don't clutter the log
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing {PersonName}", name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= count;
            percent *= 100;

            progress.Report(percent);
        }

        _logger.LogInformation(
            "Refreshed metadata for {RefreshedCount} people out of {TotalCount} total, {NewCount} of which had no item yet",
            refreshed,
            count,
            newNames.Count);

        // A person somebody locked is theirs, not ours, however little the library still credits them.
        var deadEntities = deadIds
            .Select(_libraryManager.GetItemById)
            .OfType<Person>()
            .Where(item => !item.IsLocked)
            .ToList();

        foreach (var item in deadEntities)
        {
            _logger.LogInformation("Deleting dead {ItemType} {ItemId} {ItemName}", item.GetType().Name, item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name);
        }

        _libraryManager.DeleteItemsUnsafeFast(deadEntities, deleteSourceFiles: true);

        progress.Report(100);
    }

    /// <summary>
    /// Splits the person items into the ones a credit still calls for and the ones nothing does.
    /// </summary>
    /// <param name="creditNames">Every name credited on an item, from the people table.</param>
    /// <param name="getPersonId">Maps a credit name to the id its person item has.</param>
    /// <param name="existingPersonIds">The ids of the person items that exist.</param>
    /// <returns>The credits needing an item, and the ids of the items nothing credits.</returns>
    internal static (List<string> NewNames, List<Guid> DeadIds) PartitionCreditsByPersonId(
        IReadOnlyList<string> creditNames,
        Func<string, Guid> getPersonId,
        IReadOnlySet<Guid> existingPersonIds)
    {
        ArgumentNullException.ThrowIfNull(creditNames);
        ArgumentNullException.ThrowIfNull(getPersonId);
        ArgumentNullException.ThrowIfNull(existingPersonIds);

        var newNames = new List<string>();
        var liveIds = new HashSet<Guid>();

        foreach (var name in creditNames)
        {
            var personId = getPersonId(name);

            // Distinct credit names can normalize onto one id; only the first of them needs an item.
            if (liveIds.Add(personId) && !existingPersonIds.Contains(personId))
            {
                newNames.Add(name);
            }
        }

        var deadIds = existingPersonIds.Where(id => !liveIds.Contains(id)).ToList();

        return (newNames, deadIds);
    }
}
