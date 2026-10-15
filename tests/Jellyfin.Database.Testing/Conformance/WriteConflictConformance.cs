using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: a write that loses a race with another writer, by saving a key or a unique value that the other writer
/// saved first, fails with an error that the provider classifies as a retryable write conflict, which the server then
/// retries; and a write that would fail every time is not classified as one.
/// </summary>
public abstract class WriteConflictConformance : ConformanceTestBase
{
    [ConformanceFact]
    public void SaveChanges_SecondWriterOfTheSameKey_IsARetryableWriteConflict()
    {
        var items = Table<BaseItemEntity>();
        var id = Guid.NewGuid();
        using var first = CreateDbContext();
        using var second = CreateDbContext();
        Rows.Add(first, items, new Dictionary<string, object?> { [nameof(BaseItemEntity.Id)] = id });
        Rows.Add(second, items, new Dictionary<string, object?> { [nameof(BaseItemEntity.Id)] = id });
        first.SaveChanges();

        var exception = Assert.ThrowsAny<DbUpdateException>(() => second.SaveChanges());

        Assert.True(DatabaseProvider.IsRetryableWriteConflict(exception), exception.ToString());
    }

    [ConformanceFact]
    public void SaveChanges_SecondWriterOfAUniqueValue_IsARetryableWriteConflictForEveryUniqueIndex()
    {
        var wrong = new List<string>();
        var tested = 0;
        foreach (var table in Tables())
        {
            foreach (var index in table.GetIndexes().Where(index => index.IsUnique))
            {
                var first = Rows.Save(table);
                using var readContext = CreateDbContext();
                var stored = ModelRowFactory.Find(readContext, table, first.Key)!;
                var values = index.Properties.ToDictionary(property => property.Name, property => stored.Property(property.Name).CurrentValue, StringComparer.Ordinal);
                if (values.Values.Any(value => value is null))
                {
                    // A null is never equal to another null in a unique index, so the row cannot conflict.
                    continue;
                }

                tested++;
                using var context = CreateDbContext();
                Rows.Add(context, table, values);
                try
                {
                    context.SaveChanges();
                    wrong.Add($"{Describe(index)}: the second row was saved");
                }
                catch (DbUpdateException ex) when (!DatabaseProvider.IsRetryableWriteConflict(ex))
                {
                    wrong.Add($"{Describe(index)}: not retryable: {ex.InnerException?.Message ?? ex.Message}");
                }
                catch (DbUpdateException)
                {
                }
            }
        }

        AssertNone(wrong);
        Assert.True(tested > 0, "The model has no unique index to test.");
    }

    [ConformanceFact]
    public void SaveChanges_RequiredValueMissing_IsNotARetryableWriteConflict()
    {
        using var context = CreateDbContext();
        Rows.Add(context, Table<BaseItemEntity>(), new Dictionary<string, object?> { [nameof(BaseItemEntity.Type)] = null });

        var exception = Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());

        Assert.False(DatabaseProvider.IsRetryableWriteConflict(exception), exception.ToString());
    }

    [ConformanceFact]
    public void SaveChanges_ForeignKeyToNoRow_IsNotARetryableWriteConflict()
    {
        using var context = CreateDbContext();
        Rows.Add(context, Table<UserData>(), new Dictionary<string, object?> { [nameof(UserData.ItemId)] = Guid.NewGuid() });

        var exception = Assert.ThrowsAny<DbUpdateException>(() => context.SaveChanges());

        Assert.False(DatabaseProvider.IsRetryableWriteConflict(exception), exception.ToString());
    }

    private static string Describe(IIndex index)
        => $"{index.DeclaringEntityType.GetTableName()} ({string.Join(", ", index.Properties.Select(property => property.Name))})";
}
