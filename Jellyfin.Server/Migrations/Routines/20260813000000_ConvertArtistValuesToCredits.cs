#pragma warning disable RS0030 // Do not use banned APIs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Turns the artist values a release carries into credits, then removes them.
/// </summary>
/// <remarks>
/// Converted rather than dropped: a library whose credits were removed by CleanMusicArtist and not
/// rebuilt by a scan would otherwise lose its artists, and validation would delete them as dead.
/// </remarks>
[JellyfinMigration("2026-08-13T00:00:00", nameof(ConvertArtistValuesToCredits))]
[JellyfinMigrationBackup(JellyfinDb = true)]
public class ConvertArtistValuesToCredits : IAsyncMigrationRoutine
{
    private const int BatchSize = 500;
    private const string MusicArtistItemType = "MediaBrowser.Controller.Entities.Audio.MusicArtist";

    private static readonly ItemValueType[] _artistValueTypes = [ItemValueType.Artist, ItemValueType.AlbumArtist];

    private readonly IStartupLogger<ConvertArtistValuesToCredits> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConvertArtistValuesToCredits"/> class.
    /// </summary>
    /// <param name="logger">The startup logger.</param>
    /// <param name="dbContextFactory">The database context factory.</param>
    public ConvertArtistValuesToCredits(
        IStartupLogger<ConvertArtistValuesToCredits> logger,
        IDbContextFactory<JellyfinDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc/>
    public async Task PerformAsync(CancellationToken cancellationToken)
    {
        var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var artistItems = await context.BaseItems
                .AsNoTracking()
                .Where(b => b.Type == MusicArtistItemType && b.Name != null)
                .Select(b => new { b.Id, b.Name, b.DateCreated })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var itemsByCleanName = new Dictionary<string, Guid>(StringComparer.Ordinal);
            foreach (var artist in artistItems.OrderBy(a => a.DateCreated))
            {
                var key = artist.Name!.GetCleanValue();
                if (!string.IsNullOrEmpty(key))
                {
                    itemsByCleanName.TryAdd(key, artist.Id);
                }
            }

            var values = await context.ItemValues
                .AsNoTracking()
                .Where(v => _artistValueTypes.Contains(v.Type))
                .Select(v => new { v.ItemValueId, v.Type, v.Value, v.CleanValue })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (values.Count == 0)
            {
                _logger.LogInformation("No artist values to convert.");
                return;
            }

            _logger.LogInformation("Converting {Count} artist values into credits.", values.Count);

            // One credit row per (clean name, kind), as the writer keys them.
            var creditsByKey = (await context.Peoples
                    .Where(p => p.PersonType == nameof(PersonKind.Artist) || p.PersonType == nameof(PersonKind.AlbumArtist))
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .GroupBy(p => (p.CleanName, p.PersonType ?? string.Empty), StringTupleComparer.Instance)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Id).First(), StringTupleComparer.Instance);

            // Read once, and only the links already pointing at an artist credit: filtering by item
            // instead walks most of a table with a row per credit on every item, once per batch.
            var existingLinks = (await context.PeopleBaseItemMap
                    .AsNoTracking()
                    .Where(m => m.People.PersonType == nameof(PersonKind.Artist) || m.People.PersonType == nameof(PersonKind.AlbumArtist))
                    .Select(m => new { m.ItemId, m.PeopleId })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(m => (m.ItemId, m.PeopleId))
                .ToHashSet();

            var converted = 0;
            var linked = 0;

            foreach (var batch in values.Chunk(BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var valueIds = batch.Select(v => v.ItemValueId).ToArray();
                var maps = (await context.ItemValuesMap
                        .AsNoTracking()
                        .Where(m => valueIds.Contains(m.ItemValueId))
                        .Select(m => new { m.ItemId, m.ItemValueId })
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false))
                    .ToLookup(m => m.ItemValueId, m => m.ItemId);

                if (maps.Count == 0)
                {
                    continue;
                }

                foreach (var value in batch)
                {
                    var kind = value.Type == ItemValueType.AlbumArtist
                        ? nameof(PersonKind.AlbumArtist)
                        : nameof(PersonKind.Artist);
                    var key = (value.CleanValue, kind);

                    if (!creditsByKey.TryGetValue(key, out var credit))
                    {
                        credit = new People
                        {
                            Id = Guid.NewGuid(),
                            Name = value.Value,
                            CleanName = value.CleanValue,
                            ItemId = itemsByCleanName.GetValueOrDefault(value.CleanValue),
                            PersonType = kind
                        };

                        context.Peoples.Add(credit);
                        creditsByKey[key] = credit;
                        converted++;
                    }
                    else if (credit.ItemId.IsEmpty() && itemsByCleanName.TryGetValue(value.CleanValue, out var artistItemId))
                    {
                        context.Peoples.Attach(credit);
                        credit.ItemId = artistItemId;
                    }

                    foreach (var itemId in maps[value.ItemValueId])
                    {
                        // A release that already credits this artist keeps its role and billing order.
                        if (existingLinks.Add((itemId, credit.Id)))
                        {
                            context.PeopleBaseItemMap.Add(new PeopleBaseItemMap
                            {
                                Item = null!,
                                ItemId = itemId,
                                People = null!,
                                PeopleId = credit.Id,
                                ListOrder = 0,
                                Role = string.Empty
                            });

                            linked++;
                        }
                    }
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Created {Credits} credits and {Links} release links.", converted, linked);

            // Last, so a failure part-way leaves the values to try again from.
            var removedMaps = await context.ItemValuesMap
                .Where(m => _artistValueTypes.Contains(m.ItemValue.Type))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
            var removedValues = await context.ItemValues
                .Where(v => _artistValueTypes.Contains(v.Type))
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Removed {Values} artist values and {Maps} of their release links.", removedValues, removedMaps);
        }
    }

    private sealed class StringTupleComparer : IEqualityComparer<(string, string)>
    {
        public static StringTupleComparer Instance { get; } = new();

        public bool Equals((string, string) x, (string, string) y)
            => string.Equals(x.Item1, y.Item1, StringComparison.Ordinal)
                && string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);

        public int GetHashCode((string, string) obj)
            => HashCode.Combine(obj.Item1, obj.Item2);
    }
}
