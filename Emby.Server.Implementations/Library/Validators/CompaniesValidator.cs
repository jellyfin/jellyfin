using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Validators;

/// <summary>
/// Class CompaniesValidator.
/// </summary>
public class CompaniesValidator
{
    /// <summary>
    /// The library manager.
    /// </summary>
    private readonly ILibraryManager _libraryManager;

    private readonly IItemRepository _itemRepo;

    /// <summary>
    /// The logger.
    /// </summary>
    private readonly ILogger<CompaniesValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompaniesValidator" /> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="itemRepo">The item repository.</param>
    public CompaniesValidator(ILibraryManager libraryManager, ILogger<CompaniesValidator> logger, IItemRepository itemRepo)
    {
        _libraryManager = libraryManager;
        _logger = logger;
        _itemRepo = itemRepo;
    }

    /// <summary>
    /// Runs the specified progress.
    /// </summary>
    /// <param name="progress">The progress.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    public async Task Run(IProgress<double> progress, CancellationToken cancellationToken)
    {
        // A company left without a single credit would otherwise keep its by-name item off the
        // sweep at the end of this task, which only sees companies that are gone.
        var orphaned = _itemRepo.DeleteOrphanedCompanies();
        if (orphaned > 0)
        {
            _logger.LogDebug("Deleted {Count} companies nothing is credited to any more", orphaned);
        }

        var companies = _itemRepo.GetAllCompanies();
        var existingCompanyIds = _libraryManager.GetItemIds(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Company]
        }).ToHashSet();

        var numComplete = 0;
        var count = companies.Count;
        var refreshed = 0;

        foreach (var (id, name) in companies)
        {
            try
            {
                // A company and its by-name item share an id, so one missing from the item ids is
                // exactly one this run has to create.
                if (!existingCompanyIds.Contains(id))
                {
                    var item = _libraryManager.GetCompany(name);
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
                _logger.LogError(ex, "Error refreshing {CompanyName}", name);
            }

            numComplete++;
            double percent = numComplete;
            percent /= count;
            percent *= 100;

            progress.Report(percent);
        }

        _logger.LogInformation("Refreshed metadata for {RefreshedCount} new companies out of {TotalCount} total", refreshed, count);

        var deadEntities = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Company],
            IsDeadCompany = true,
            IsLocked = false
        });

        foreach (var item in deadEntities)
        {
            _logger.LogInformation("Deleting dead {ItemType} {ItemId} {ItemName}", item.GetType().Name, item.Id.ToString("N", CultureInfo.InvariantCulture), item.Name);
        }

        _libraryManager.DeleteItemsUnsafeFast(deadEntities, deleteSourceFiles: true);

        progress.Report(100);
    }
}
