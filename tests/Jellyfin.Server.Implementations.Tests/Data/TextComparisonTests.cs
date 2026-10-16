using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Holds every engine to comparing text as SQLite does: two values that differ only by trailing spaces are different, and
/// text orders by code point. SQL Server pads with spaces to compare, and orders by UTF-16 code unit; both are known gaps.
/// </summary>
public sealed class TextComparisonTests : DatabaseTestFixture
{
    [Fact]
    public void ItemValues_DifferingOnlyByTrailingSpaces_AreBothStoredAndFoundApart()
    {
        KnownGap.On(KnownGaps.TrailingSpacesOnSqlServer, () =>
        {
            using (var context = CreateDbContext())
            {
                context.ItemValues.Add(new ItemValue { ItemValueId = Guid.NewGuid(), Type = ItemValueType.Genre, Value = "Rock", CleanValue = "rock" });
                context.ItemValues.Add(new ItemValue { ItemValueId = Guid.NewGuid(), Type = ItemValueType.Genre, Value = "Rock ", CleanValue = "rock " });
                context.SaveChanges();
            }

            using var check = CreateDbContext();
            Assert.Equal(2, check.ItemValues.Count(e => e.Type == ItemValueType.Genre));
            Assert.Equal(["Rock"], check.ItemValues.Where(e => e.Value == "Rock").Select(e => e.Value).ToList());
        });
    }

    [Fact]
    public void SortNames_OutsideTheBasicMultilingualPlane_OrderByCodePoint()
    {
        string[] byCodePoint = ["z", "", "�", "\U0001F600"];
        const string Type = "TextComparisonProbe";
        using (var context = CreateDbContext())
        {
            foreach (var name in byCodePoint.Reverse())
            {
                context.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = Type, SortName = name });
            }

            context.SaveChanges();
        }

        KnownGap.On(KnownGaps.Utf16OrderOnSqlServer, () =>
        {
            using var context = CreateDbContext();
            var ordered = context.BaseItems.Where(e => e.Type == Type).OrderBy(e => e.SortName).Select(e => e.SortName!).ToList();
            if (ordered.SequenceEqual(["z", "\U0001F600", "", "�"]))
            {
                throw new InvalidOperationException(KnownGaps.Utf16OrderMessage);
            }

            Assert.Equal(byCodePoint, ordered);
        });
    }
}
