using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Moves the studios out of the item values and into the Companies table.
/// </summary>
/// <remarks>
/// Only the links move; every credit becomes one of kind <see cref="CompanyKind.Studio"/>.
/// Which of them is really a network is not knowable from what was stored, so telling the two
/// apart waits for the next metadata refresh. Reclassifying does not move the company itself,
/// which keeps its id and so its artwork and user data.
/// </remarks>
[JellyfinMigration("2026-09-14T09:40:00", nameof(MigrateStudiosToCompanies))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class MigrateStudiosToCompanies : IAsyncMigrationRoutine
{
    private const string StudioItemType = "MediaBrowser.Controller.Entities.Studio";
    private const string CompanyItemType = "MediaBrowser.Controller.Entities.Company";

    // ItemValueType.Studios, retired along with the rows this migration reads.
    private const ItemValueType StudiosItemValue = (ItemValueType)3;

    private readonly IStartupLogger<MigrateStudiosToCompanies> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;
    private readonly IServerApplicationPaths _applicationPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrateStudiosToCompanies"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    /// <param name="applicationPaths">The application paths.</param>
    public MigrateStudiosToCompanies(
        IStartupLogger<MigrateStudiosToCompanies> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory,
        IServerApplicationPaths applicationPaths)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _applicationPaths = applicationPaths;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            await RewritePackedCompaniesAsync(context, cancellationToken).ConfigureAwait(false);
            await MigrateItemValuesAsync(context, cancellationToken).ConfigureAwait(false);
            await MigrateByNameItemsAsync(context, cancellationToken).ConfigureAwait(false);
        }

        MoveMetadataDirectory();
    }

    private async Task RewritePackedCompaniesAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        // The column held one studio name per entry. Every name it holds was a studio, and the kind
        // is part of each entry now. Packing them back rather than rewriting the separators in SQL
        // keeps the one definition of the format, and drops the empty entries a stray separator left.
        var packed = await context.BaseItems
            .Where(e => e.Companies != null && e.Companies != string.Empty)
            .Select(e => e.Companies!)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var studios in packed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var companies = CompanyInfo.Pack(studios
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(name => new CompanyInfo { Name = name, Type = CompanyKind.Studio }));

            await context.BaseItems
                .Where(e => e.Companies == studios)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.Companies, companies), cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogInformation("Rewrote the companies of {Count} distinct studio lists.", packed.Count);
    }

    private async Task MigrateItemValuesAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var studios = await context.ItemValues
            .Where(e => e.Type == StudiosItemValue)
            .Select(e => new { e.ItemValueId, e.Value })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (studios.Count == 0)
        {
            _logger.LogInformation("No studios to move into the Companies table.");
            return;
        }

        // Names that clean to the same value share one company, so several item values can end up
        // behind a single company id.
        var companyIdsByItemValue = studios.ToDictionary(
            e => e.ItemValueId,
            e => Company.GetCompanyId(e.Value));

        var companies = studios
            .Select(e => new { CompanyId = companyIdsByItemValue[e.ItemValueId], e.Value })
            .GroupBy(e => e.CompanyId)
            // The same name the by-name lists used to pick out of the casing variants.
            .Select(g => (CompanyId: g.Key, Name: g.Min(e => e.Value)!))
            .Select(e => new CompanyEntity
            {
                Id = e.CompanyId,
                Name = e.Name,
                CleanName = e.Name.GetCleanValue()
            })
            .ToList();

        context.Companies.AddRange(companies);

        var maps = await context.ItemValuesMap
            .Where(e => e.ItemValue.Type == StudiosItemValue)
            .Select(e => new { e.ItemId, e.ItemValueId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        context.CompanyBaseItemMap.AddRange(maps
            .Select(e => (e.ItemId, CompanyId: companyIdsByItemValue[e.ItemValueId]))
            .Distinct()
            .Select(e => new CompanyBaseItemMap
            {
                Item = null!,
                ItemId = e.ItemId,
                Company = null!,
                CompanyId = e.CompanyId,
                Type = CompanyKindEntity.Studio
            }));

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // The mappings go with them, on the cascade.
        await context.ItemValues
            .Where(e => e.Type == StudiosItemValue)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Moved {Count} studios into the Companies table.", companies.Count);
    }

    private async Task MigrateByNameItemsAsync(JellyfinDbContext context, CancellationToken cancellationToken)
    {
        var studioItems = await context.BaseItems
            .AsNoTracking()
            .Where(e => e.Type == StudioItemType && e.Name != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (studioItems.Count == 0)
        {
            return;
        }

        var companyRoot = _applicationPaths.CompanyPath;

        // A company's by-name item takes the company's id, which is not the one the studio item was
        // stored under. The new row goes in first, then everything hanging off the old one moves
        // across, so the images and the user data survive the change of id.
        var movedIds = new List<(Guid OldId, Guid NewId)>();
        foreach (var item in studioItems)
        {
            var newId = Company.GetCompanyId(item.Name!);
            if (newId.Equals(item.Id))
            {
                continue;
            }

            var oldId = item.Id;
            item.Id = newId;
            item.Type = CompanyItemType;
            if (!string.IsNullOrEmpty(item.Path))
            {
                item.Path = Path.Combine(companyRoot, Path.GetFileName(item.Path));
            }

            context.BaseItems.Add(item);
            movedIds.Add((oldId, newId));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (oldId, newId) in movedIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await context.BaseItemImageInfos
                .Where(e => e.ItemId == oldId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ItemId, newId), cancellationToken)
                .ConfigureAwait(false);

            await context.BaseItemMetadataFields
                .Where(e => e.ItemId == oldId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ItemId, newId), cancellationToken)
                .ConfigureAwait(false);

            await context.BaseItemProviders
                .Where(e => e.ItemId == oldId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ItemId, newId), cancellationToken)
                .ConfigureAwait(false);

            await context.UserData
                .Where(e => e.ItemId == oldId)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.ItemId, newId), cancellationToken)
                .ConfigureAwait(false);
        }

        await context.BaseItems
            .Where(e => e.Type == StudioItemType)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Moved {Count} studio items to company items.", movedIds.Count);
    }

    private void MoveMetadataDirectory()
    {
        var studioPath = Path.Combine(_applicationPaths.InternalMetadataPath, nameof(CompanyKind.Studio));
        if (!Directory.Exists(studioPath))
        {
            return;
        }

        var companyPath = _applicationPaths.CompanyPath;
        if (Directory.Exists(companyPath))
        {
            return;
        }

        try
        {
            Directory.Move(studioPath, companyPath);
        }
        catch (Exception ex)
        {
            // Only the artwork and the local metadata already on disk are at stake; a refresh
            // fetches them again.
            _logger.LogWarning(ex, "Unable to move {StudioPath} to {CompanyPath}", studioPath, companyPath);
        }
    }
}
