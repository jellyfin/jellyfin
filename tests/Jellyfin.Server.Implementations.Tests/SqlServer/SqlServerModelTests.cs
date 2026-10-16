using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Providers.SqlServer;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

/// <summary>
/// Holds the model of the SQL Server provider, built without a server, to the conventions it needs: ids as 16 bytes in
/// the order of their text, the binary collation on every text column, the longest indexable type for the strings that
/// are indexed without a bound, no bound for text outside any index, no index filter with an <c>OR</c>, and no cascade
/// that the server cannot create.
/// </summary>
public sealed class SqlServerModelTests
{
    private static readonly Lazy<IModel> _model = new(() => DesignTimeModel(OfflineContexts.SqlServer));
    private static readonly Lazy<IModel> _reference = new(() => DesignTimeModel(OfflineContexts.Sqlite));

    // The strings that are indexed without a bound of their own, as the shared configuration marks them.
    private static readonly string[] _textClassColumns =
    [
        "BaseItemProviders.ProviderValue",
        "BaseItems.CleanName",
        "BaseItems.Name",
        "BaseItems.Path",
        "BaseItems.PresentationUniqueKey",
        "BaseItems.SeriesName",
        "BaseItems.SeriesPresentationUniqueKey",
        "BaseItems.SortName",
        "ItemValues.CleanValue",
        "MediaStreamInfos.Language",
        "Peoples.Name",
        "Peoples.NameLower"
    ];

    private static IModel Model => _model.Value;

    private static IModel Reference => _reference.Value;

    [Fact]
    public void GuidColumns_AreSixteenBytesInTheOrderOfTheirText()
    {
        var guids = Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(Guid))
            .ToList();

        Assert.NotEmpty(guids);
        Assert.All(guids, property =>
        {
            Assert.Equal("binary(16)", property.GetColumnType());
            Assert.IsType<BigEndianGuidConverter>(property.GetTypeMapping().Converter);
        });
    }

    [Fact]
    public void BigEndianGuidConverter_WritesTheBytesInTheOrderOfTheText()
    {
        var id = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

        Assert.Equal(Convert.FromHexString("00112233445566778899AABBCCDDEEFF"), BigEndianGuidConverter.ToBytes(id));
        Assert.Equal(id, BigEndianGuidConverter.FromBytes(BigEndianGuidConverter.ToBytes(id)));
    }

    [Fact]
    public void BigEndianGuidConverter_BytesSortAsTheText()
    {
        // The first id is the smaller text but the larger uniqueidentifier, which orders the last six bytes first.
        Guid[] ids = [Guid.Parse("0fffffff-ffff-ffff-ffff-ffffffffffff"), Guid.Parse("10000000-0000-0000-0000-000000000000")];

        var byBytes = ids.OrderBy(id => Convert.ToHexString(BigEndianGuidConverter.ToBytes(id)), StringComparer.Ordinal);

        Assert.Equal(ids.OrderBy(id => id.ToString(), StringComparer.Ordinal), byBytes);
    }

    [Fact]
    public void TextColumns_AllHaveTheBinaryCollation()
    {
        var text = Model.GetRelationalModel().Tables
            .SelectMany(t => t.Columns)
            .Where(c => TextCollationConvention.IsText(c.StoreType))
            .ToList();

        Assert.True(text.Count > _textClassColumns.Length, "The model has few text columns, so the check sees too little.");
        Assert.All(text, column => Assert.Equal(SqlServerDatabaseProvider.Collation, column.Collation));
    }

    [Theory]
    [InlineData("nvarchar(4000)", true)]
    [InlineData("nvarchar(max)", true)]
    [InlineData("varchar(10)", true)]
    [InlineData("nchar(3)", true)]
    [InlineData("char", true)]
    [InlineData("ntext", true)]
    [InlineData("text", true)]
    [InlineData("binary(16)", false)]
    [InlineData("int", false)]
    [InlineData("datetime2", false)]
    [InlineData("varbinary(max)", false)]
    [InlineData(null, false)]
    public void IsText_IsTrueForTheCharacterTypes(string? storeType, bool expected)
    {
        Assert.Equal(expected, TextCollationConvention.IsText(storeType));
    }

    [Fact]
    public void HistoryTable_HasTheBinaryCollation()
    {
        using var context = OfflineContexts.SqlServer();

        var script = context.GetService<IHistoryRepository>().GetCreateScript();

        Assert.Equal(2, script.Split("COLLATE " + SqlServerDatabaseProvider.Collation).Length - 1);
    }

    [Fact]
    public void TextClassColumns_AreTheLongestTypeThatAnIndexTakes()
    {
        var marked = Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties().Where(p => p.GetColumnType() == IndexedTextConvention.ColumnType).Select(p => Name(e, p)))
            .Order(StringComparer.Ordinal);

        Assert.Equal(_textClassColumns, marked);
    }

    [Fact]
    public void BoundedTextOutsideEveryIndexAndKey_HasNoBound()
    {
        var properties = Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties().Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is not null).Select(p => (Entity: e, Property: p)))
            .ToList();
        var outside = properties.Where(x => !x.Property.IsKey() && !x.Property.IsForeignKey() && !x.Property.GetContainingIndexes().Any()).ToList();
        var inside = properties.Except(outside).ToList();

        Assert.Contains(outside, x => Name(x.Entity, x.Property) == "ActivityLogs.Overview");
        Assert.Contains(outside, x => Name(x.Entity, x.Property) == "ImageInfos.Path");
        Assert.All(outside, x => Assert.Equal(UnindexedTextConvention.ColumnType, x.Property.GetColumnType()));

        // Text in an index or a key keeps the bound it is sized for.
        Assert.NotEmpty(inside);
        Assert.All(inside, x => Assert.Equal($"nvarchar({x.Property.GetMaxLength()})", x.Property.GetColumnType()));
    }

    [Fact]
    public void IndexFilters_OnlyTheOneWithAnOrIsDropped()
    {
        var filters = Reference.GetEntityTypes()
            .SelectMany(e => e.GetIndexes().Where(i => i.GetFilter() is not null).Select(i => (Entity: e, Index: i)))
            .ToList();

        Assert.Equal(4, filters.Count);
        Assert.All(filters, x =>
        {
            var index = Model.FindEntityType(x.Entity.Name)!.GetIndexes().Single(i => i.GetDatabaseName() == x.Index.GetDatabaseName());
            var filter = x.Index.GetFilter()!;
            Assert.Equal(filter.Contains(" OR ", StringComparison.Ordinal) ? null : filter, index.GetFilter());
            Assert.False(index.IsUnique);
        });
    }

    [Theory]
    [InlineData("\"PrimaryVersionId\" IS NULL AND (\"OwnerId\" IS NULL OR \"ExtraType\" IS NOT NULL)", false)]
    [InlineData("[A] = 1 or [B] = 2", false)]
    [InlineData("\"OwnerId\" IS NOT NULL", true)]
    [InlineData("\"ORDER\" > 0 AND \"Color\" IS NULL", true)]
    public void CanExpress_IsFalseForAFilterWithAnOr(string filter, bool expected)
    {
        Assert.Equal(expected, IndexFilterConvention.CanExpress(filter));
    }

    [Fact]
    public void UniqueIndexOnANullableColumn_IsFilteredToTheRowsWithAValue()
    {
        // A unique index of SQL Server takes one NULL; EF Core filters it so that it takes any number, as SQLite does.
        var index = Model.GetEntityTypes().SelectMany(e => e.GetIndexes()).Single(i => i.GetDatabaseName() == "IX_ImageInfos_UserId");

        Assert.True(index.IsUnique);
        Assert.Equal("[UserId] IS NOT NULL", index.GetFilter());
    }

    [Fact]
    public void ForeignKeys_DeleteAsInTheReferenceButTheTwoTheServerCannotCascade()
    {
        string[] noAction = ["AncestorIds (ParentItemId)", "BaseItems (ParentId)"];
        var keys = Reference.GetEntityTypes().SelectMany(e => e.GetForeignKeys()).ToList();

        Assert.All(keys, key =>
        {
            var name = $"{key.DeclaringEntityType.GetTableName()} ({string.Join(", ", key.Properties.Select(p => p.GetColumnName()))})";
            var actual = Model.FindEntityType(key.DeclaringEntityType.Name)!.GetForeignKeys().Single(k => k.Properties.Select(p => p.Name).SequenceEqual(key.Properties.Select(p => p.Name)));
            if (noAction.Contains(name))
            {
                Assert.Equal(DeleteBehavior.Cascade, key.DeleteBehavior);
                Assert.Equal(DeleteBehavior.NoAction, actual.DeleteBehavior);
            }
            else
            {
                Assert.Equal(key.DeleteBehavior, actual.DeleteBehavior);
            }
        });
    }

    [Fact]
    public void Identifiers_AreAtMost128Characters()
    {
        var names = Model.GetRelationalModel().Tables
            .SelectMany(t => new[] { t.Name }
                .Concat(t.Columns.Select(c => c.Name))
                .Concat(t.UniqueConstraints.Select(c => c.Name))
                .Concat(t.ForeignKeyConstraints.Select(c => c.Name))
                .Concat(t.Indexes.Select(i => i.Name)))
            .ToList();

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.InRange(name.Length, 1, 128));
    }

    private static string Name(IReadOnlyEntityType entityType, IReadOnlyProperty property) => $"{entityType.GetTableName()}.{property.GetColumnName()}";

    private static IModel DesignTimeModel(Func<Jellyfin.Database.Implementations.JellyfinDbContext> create)
    {
        using var context = create();
        return context.GetService<IDesignTimeModel>().Model;
    }
}
