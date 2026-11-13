using System;
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

        // Only credits with no live item: one already linked may be credited under another name than
        // its item's, and getting its item by name would create a phantom the sweep then deletes.
        // Every kind: an Artist credit belongs to a MusicArtist, not a Person.
        var unlinked = _libraryManager.GetUnlinkedCredits();
        var numLinked = 0;

        IProgress<double> linkProgress = new Progress<double>(val => progress.Report(val / 2));
        for (var i = 0; i < unlinked.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var credit = unlinked[i];
            try
            {
                var item = _libraryManager.GetOrCreateCreditItem(credit.Name, credit.Type);
                if (item is null)
                {
                    _logger.LogWarning("Failed to get or create the item for {Kind} credit {Name}", credit.Type, credit.Name);
                    continue;
                }

                numLinked += _libraryManager.LinkCreditsToItem(credit.Name, credit.Type, item.Id);
            }
            catch (OperationCanceledException)
            {
                // Don't clutter the log
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking {Kind} credit {Name}", credit.Type, credit.Name);
            }

            linkProgress.Report(100d * (i + 1) / unlinked.Count);
        }

        // A person somebody locked is theirs, not ours, however little the library still credits them.
        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            IsDeadPerson = true,
            IsLocked = false
        });

        foreach (var item in deadEntities)
        {
            _logger.LogInformation("Deleting dead {ItemType} {ItemId} {ItemName}", item.GetType().Name, item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name);
        }

        _libraryManager.DeleteItemsUnsafeFast(deadEntities, deleteSourceFiles: true);

        // Created while saving an item's cast, a person never had a refresh of its own.
        var personIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person]
        });

        IProgress<double> refreshProgress = new Progress<double>(val => progress.Report(50 + (val / 2)));
        var refreshed = 0;
        for (var i = 0; i < personIds.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (_libraryManager.GetItemById(personIds[i]) is Person item && item.DateLastRefreshed == default)
                {
                    await item.RefreshMetadata(cancellationToken).ConfigureAwait(false);
                    refreshed++;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing person {PersonId}", personIds[i]);
            }

            refreshProgress.Report(100d * (i + 1) / personIds.Count);
        }

        _logger.LogInformation(
            "Linked {Linked} credits, deleted {Dead} dead people and refreshed {Refreshed} out of {Total}",
            numLinked,
            deadEntities.Count,
            refreshed,
            personIds.Count);

        progress.Report(100);
    }
}
