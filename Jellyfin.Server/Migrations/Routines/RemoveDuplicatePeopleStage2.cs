using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Migrations.Stages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Stage 2: Remove duplicate People records and enforce unique constraints.
/// </summary>
[JellyfinMigration("2025-09-20T05:21:51", nameof(RemoveDuplicatePeopleStage2), Stage = JellyfinMigrationStageTypes.CoreInitialisation)]
public class RemoveDuplicatePeopleStage2 : IAsyncMigrationRoutine
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;
    private readonly ILogger<RemoveDuplicatePeopleStage2> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveDuplicatePeopleStage2"/> class.
    /// </summary>
    /// <param name="dbProvider">The database context factory.</param>
    /// <param name="logger">The logger.</param>
    public RemoveDuplicatePeopleStage2(IDbContextFactory<JellyfinDbContext> dbProvider, ILogger<RemoveDuplicatePeopleStage2> logger)
    {
        _dbProvider = dbProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        using var dbContext = _dbProvider.CreateDbContext();
        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Starting duplicate People cleanup");

            // Get initial counts for progress tracking
            var totalPeople = await dbContext.Peoples.CountAsync(cancellationToken).ConfigureAwait(false);
            var totalMappings = await dbContext.PeopleBaseItemMap.CountAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Processing {PeopleCount} People records and {MappingCount} PeopleBaseItemMap records", totalPeople, totalMappings);

            // Step 1: Find canonical people and build lookup dictionary
            _logger.LogInformation("Finding canonical people - Time: {Time}", sw.Elapsed);
            var canonicalPeople = await dbContext.Peoples
                .GroupBy(p => new { p.Name, p.PersonType })
                .Select(g => g.OrderBy(p => p.Id).First())
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var canonicalIds = canonicalPeople.Select(p => p.Id).ToHashSet();

            // Build efficient lookup dictionary: (Name, PersonType) -> CanonicalId
            var canonicalLookup = canonicalPeople.ToDictionary(
                p => (p.Name, p.PersonType),
                p => p.Id);

            _logger.LogInformation("Found {Count} canonical people out of {Total} total people - Time: {Time}", canonicalPeople.Count, totalPeople, sw.Elapsed);

            // Step 2: Update PeopleBaseItemMap records to use canonical people IDs
            _logger.LogInformation("Updating PeopleBaseItemMap to use canonical people IDs - Time: {Time}", sw.Elapsed);

            var mappingsUpdated = 0;
            var mappingsToUpdate = dbContext.PeopleBaseItemMap
                .Where(m => !canonicalIds.Contains(m.PeopleId))
                .ToList();

            var mappingsToRemove = new List<PeopleBaseItemMap>();
            var mappingsToCreate = new List<PeopleBaseItemMap>();

            foreach (var mapping in mappingsToUpdate)
            {
                var person = await dbContext.Peoples
                    .Where(p => p.Id.Equals(mapping.PeopleId))
                    .Select(p => new { p.Name, p.PersonType })
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (person != null && canonicalLookup.TryGetValue((person.Name, person.PersonType), out var canonicalId))
                {
                    // Create new mapping pointing to canonical person
                    var newMapping = new PeopleBaseItemMap
                    {
                        ItemId = mapping.ItemId,
                        PeopleId = canonicalId,
                        SortOrder = mapping.SortOrder,
                        ListOrder = mapping.ListOrder,
                        Role = mapping.Role,
                        Item = mapping.Item,
                        People = canonicalPeople.First(p => p.Id.Equals(canonicalId))
                    };
                    mappingsToCreate.Add(newMapping);

                    // Mark old mapping for removal
                    mappingsToRemove.Add(mapping);
                    mappingsUpdated++;
                }
            }

            // Remove old mappings and add new ones pointing to canonical people
            dbContext.PeopleBaseItemMap.RemoveRange(mappingsToRemove);
            dbContext.PeopleBaseItemMap.AddRange(mappingsToCreate);

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Updated {Count} PeopleBaseItemMap records - Time: {Time}", mappingsUpdated, sw.Elapsed);

            // Step 3: Remove duplicate mappings (same ItemId + PeopleId combinations)
            _logger.LogInformation("Removing duplicate PeopleBaseItemMap records - Time: {Time}", sw.Elapsed);
            var duplicateMappingsRemoved = 0;

            var allMappings = dbContext.PeopleBaseItemMap.ToList();

            var mappingGroups = allMappings
                .GroupBy(m => new { m.ItemId, m.PeopleId })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in mappingGroups)
            {
                var duplicates = group.Skip(1).ToList();
                dbContext.PeopleBaseItemMap.RemoveRange(duplicates);
                duplicateMappingsRemoved += duplicates.Count;
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} duplicate PeopleBaseItemMap records - Time: {Time}", duplicateMappingsRemoved, sw.Elapsed);

            // Step 4: Delete duplicate People records (keep only canonical ones)
            _logger.LogInformation("Removing duplicate People records - Time: {Time}", sw.Elapsed);
            var duplicatePeople = await dbContext.Peoples
                .Where(p => !canonicalIds.Contains(p.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            dbContext.Peoples.RemoveRange(duplicatePeople);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} duplicate People records - Time: {Time}", duplicatePeople.Count, sw.Elapsed);

            // Step 5: Remove orphaned People records (those with no BaseItem mappings)
            _logger.LogInformation("Removing orphaned People records with no BaseItem mappings - Time: {Time}", sw.Elapsed);
            var orphanedPeople = await dbContext.Peoples
                .Where(p => !dbContext.PeopleBaseItemMap.Any(m => m.PeopleId.Equals(p.Id)))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            dbContext.Peoples.RemoveRange(orphanedPeople);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Removed {Count} orphaned People records with no BaseItem mappings - Time: {Time}", orphanedPeople.Count, sw.Elapsed);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Duplicate People cleanup completed successfully - Total time: {Time}", sw.Elapsed);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Failed to perform duplicate People cleanup");
            throw;
        }
    }
}
