using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines;

/// <summary>
/// Drops keys that no current property reads or writes from the serialized <c>BaseItems.Data</c> blob.
/// </summary>
[JellyfinMigration("2026-09-11T12:00:00", nameof(StripEmbeddedLinkedChildren))]
internal class StripEmbeddedLinkedChildren : IDatabaseMigrationRoutine
{
    private const int BatchSize = 1000;

    private static readonly string[] _deadKeys = ["LinkedChildren", "ExtraIds", "SupportsExternalTransfer"];

    private static readonly JsonDocumentOptions _documentOptions = new() { AllowDuplicateProperties = false };

    private readonly ILogger<StripEmbeddedLinkedChildren> _logger;
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider;

    public StripEmbeddedLinkedChildren(
        ILoggerFactory loggerFactory,
        IDbContextFactory<JellyfinDbContext> dbProvider)
    {
        _logger = loggerFactory.CreateLogger<StripEmbeddedLinkedChildren>();
        _dbProvider = dbProvider;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        using var context = _dbProvider.CreateDbContext();

        var updated = 0;
        Guid? lastId = null;
        while (true)
        {
            // The text match only narrows the rows down, whether a key is a top-level property is decided on the parsed blob.
            // Paged by Id rather than by offset: the rows written back drop out of the match, the rows left alone stay in it.
            var batch = context.BaseItems
                .Where(e => !lastId.HasValue || e.Id > lastId)
                .Where(e => e.Data != null
                    && (e.Data.Contains("\"LinkedChildren\"")
                        || e.Data.Contains("\"ExtraIds\"")
                        || e.Data.Contains("\"SupportsExternalTransfer\"")))
                .OrderBy(e => e.Id)
                .Select(e => new { e.Id, e.Data })
                .Take(BatchSize)
                .ToList();
            if (batch.Count == 0)
            {
                break;
            }

            lastId = batch[^1].Id;

            using var transaction = context.Database.BeginTransaction();
            foreach (var item in batch)
            {
                var data = item.Data is null ? null : RemoveDeadKeys(item.Data);
                if (data is null)
                {
                    continue;
                }

                context.BaseItems
                    .Where(e => e.Id.Equals(item.Id))
                    .ExecuteUpdate(s => s.SetProperty(e => e.Data, data));
                updated++;
            }

            transaction.Commit();
        }

        _logger.LogInformation("Dropped dead keys from the serialized data of {Count} items", updated);
    }

    private static string? RemoveDeadKeys(string data)
    {
        try
        {
            if (JsonNode.Parse(data, documentOptions: _documentOptions) is not JsonObject item)
            {
                return null;
            }

            var removed = false;
            foreach (var key in _deadKeys)
            {
                removed |= item.Remove(key);
            }

            // Written back with the options the blob was written with, so everything else stays as it was.
            return removed ? item.ToJsonString(JsonDefaults.Options) : null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // A blob that cannot be read back as it was written (malformed, with a duplicate key or an unpaired surrogate)
            // is left as it is: one bad row must not cost every other row the fix.
            return null;
        }
    }
}
