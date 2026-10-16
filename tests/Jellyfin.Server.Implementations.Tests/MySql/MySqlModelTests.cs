using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Jellyfin.Server.Implementations.Tests.EfMigrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microting.EntityFrameworkCore.MySql.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlModelTests
{
    public static TheoryData<string> MySqlFamilyRows => new(MigrationProvider.All.Where(p => p.ServerVersion is not null).Select(p => p.Name));

    [Theory]
    [MemberData(nameof(MySqlFamilyRows))]
    public void PathIndex_IndexesAPrefixOf512Characters(string providerName)
    {
        using var context = MigrationProvider.Get(providerName).CreateContext();
        var index = context.GetService<IDesignTimeModel>().Model
            .FindEntityType(typeof(BaseItemEntity))!
            .GetIndexes()
            .Single(i => i.GetDatabaseName() == "IX_BaseItems_Path");

        // MySQL and MariaDB index the first 512 characters of a path, so that a path of any length is saved.
        Assert.Equal([512], index.PrefixLength());
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.13-mariadb")]
    public void StringProperties_HaveTheBinaryCollation(string server)
    {
        using var context = OfflineContexts.MySql(server);

        var strings = Properties(context).Where(p => p.ClrType == typeof(string)).ToList();

        Assert.NotEmpty(strings);
        Assert.All(strings, p => Assert.Equal("utf8mb4_0900_bin", p.GetCollation()));
    }

    [Fact]
    public void DateTimeProperties_AreClampedAndHaveMicrosecondPrecision()
    {
        using var context = OfflineContexts.MySql();

        var dates = Properties(context).Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(DateTime)).ToList();

        Assert.NotEmpty(dates);
        Assert.All(dates, p => Assert.IsType<ClampedUtcDateTimeConverter>(p.GetValueConverter()));
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.13-mariadb")]
    public void CreateScript_UsesTheColumnTypesOfTheModel(string server)
    {
        using var context = OfflineContexts.MySql(server);

        var script = context.Database.GenerateCreateScript();

        Assert.Contains("datetime(6)", script, StringComparison.Ordinal);
        Assert.Contains("binary(16)", script, StringComparison.Ordinal);
        Assert.Contains("longtext", script, StringComparison.Ordinal);
        Assert.Contains("`Path` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_bin", script, StringComparison.Ordinal);
        Assert.Contains("ON `BaseItems` (`Path`(512))", script, StringComparison.Ordinal);
        Assert.DoesNotContain("char(36)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("datetime(0)", script, StringComparison.Ordinal);
        Assert.DoesNotContain("WHERE", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Indexes_HaveNoFilter()
    {
        using var mySql = OfflineContexts.MySql();
        using var sqlite = OfflineContexts.Sqlite();

        Assert.Contains(Indexes(sqlite), i => i.GetFilter() is not null);
        Assert.DoesNotContain(Indexes(mySql), i => i.GetFilter() is not null);
    }

    [Fact]
    public void Identifiers_FitTheLimitOfTheServer()
    {
        using var context = OfflineContexts.MySql();
        var model = context.GetService<IDesignTimeModel>().Model;

        var names = new List<string>();
        foreach (var entity in model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            names.Add(entity.GetTableName()!);
            names.AddRange(entity.GetProperties().Select(p => p.GetColumnName()));
            names.AddRange(entity.GetIndexes().Select(i => i.GetDatabaseName()!));
            names.AddRange(entity.GetKeys().Select(k => k.GetName()!));
            names.AddRange(entity.GetForeignKeys().Select(f => f.GetConstraintName()!));
        }

        Assert.NotEmpty(names);
        Assert.All(names, n => Assert.True(n.Length <= 64, $"{n} has {n.Length} characters."));
    }

    [Fact]
    public void UnboundedIndexedText_IsLongTextWithAPrefixOnEveryIndex()
    {
        using var context = OfflineContexts.MySql();

        var unbounded = Properties(context).Where(p => p.IsUnboundedIndexedText()).ToList();

        Assert.NotEmpty(unbounded);
        Assert.All(unbounded, p => Assert.Equal("longtext", p.GetColumnType()));
        foreach (var index in Indexes(context).Where(i => i.Properties.Any(p => p.IsUnboundedIndexedText())))
        {
            var prefix = index.PrefixLength();
            Assert.NotNull(prefix);
            Assert.Equal(index.Properties.Count, prefix.Length);
            for (var i = 0; i < prefix.Length; i++)
            {
                if (index.Properties[i].IsUnboundedIndexedText())
                {
                    Assert.InRange(prefix[i], 1, IndexPrefixLengthConvention.MaxPrefixLength);
                }
                else
                {
                    Assert.Equal(0, prefix[i]);
                }
            }
        }
    }

    [Fact]
    public void BoundedText_IsLongTextUnlessItIsKeyedOrIndexed()
    {
        using var context = OfflineContexts.MySql();

        var bounded = Properties(context).Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is not null).ToList();

        Assert.NotEmpty(bounded);
        Assert.Contains(bounded, p => IsIndexedOrKeyed(p));
        Assert.Contains(bounded, p => !IsIndexedOrKeyed(p));
        foreach (var property in bounded)
        {
            var name = $"{property.DeclaringType.DisplayName()}.{property.Name}";
            if (IsIndexedOrKeyed(property))
            {
                Assert.True(property.GetColumnType().StartsWith("varchar(", StringComparison.Ordinal), $"{name} is {property.GetColumnType()}.");
            }
            else
            {
                Assert.Equal("longtext", property.GetColumnType());
            }
        }
    }

    [Fact]
    public void IndexesOverBoundedText_HaveNoPrefix()
    {
        using var context = OfflineContexts.MySql();

        var bounded = Indexes(context).Where(i => !i.Properties.Any(p => p.IsUnboundedIndexedText())).ToList();

        Assert.NotEmpty(bounded);
        Assert.All(bounded, i => Assert.True(i.PrefixLength() is null or { Length: 0 }, $"{i.DeclaringEntityType.DisplayName()} {i.GetDatabaseName()}"));
    }

    [Fact]
    public void UniqueIndexesAndKeys_NeverRelyOnAPrefix()
    {
        using var context = OfflineContexts.MySql();
        var model = context.GetService<IDesignTimeModel>().Model;

        var unique = Indexes(context).Where(i => i.IsUnique).ToList();
        Assert.NotEmpty(unique);
        Assert.All(unique, i => Assert.True(i.PrefixLength() is null or { Length: 0 } || i.PrefixLength()!.All(p => p == 0), i.GetDatabaseName()));
        Assert.All(unique, i => Assert.DoesNotContain(i.Properties, p => p.IsUnboundedIndexedText()));
        Assert.All(
            model.GetEntityTypes().SelectMany(e => e.GetKeys()),
            k => Assert.DoesNotContain(k.Properties, p => p.IsUnboundedIndexedText()));
    }

    [Fact]
    public void EveryIndex_FitsTheSizeOfAnIndexEntryIncludingThePrefixes()
    {
        using var context = OfflineContexts.MySql();

        foreach (var index in Indexes(context))
        {
            var prefix = index.PrefixLength();
            var bytes = 0;
            for (var i = 0; i < index.Properties.Count; i++)
            {
                var property = index.Properties[i];
                if (property.IsUnboundedIndexedText())
                {
                    bytes += prefix![i] * IndexPrefixLengthConvention.BytesPerChar;
                }
                else if (property.ClrType == typeof(string))
                {
                    bytes += property.GetMaxLength()!.Value * IndexPrefixLengthConvention.BytesPerChar;
                }
            }

            Assert.True(bytes <= IndexPrefixLengthConvention.MaxKeyBytes, $"{index.DeclaringEntityType.DisplayName()} {index.GetDatabaseName()} takes {bytes} bytes of text.");
        }
    }

    private static bool IsIndexedOrKeyed(IProperty property)
    {
        return property.IsKey() || property.IsForeignKey() || property.GetContainingIndexes().Any();
    }

    private static IEnumerable<IProperty> Properties(JellyfinDbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model.GetEntityTypes().Where(e => e.GetTableName() is not null).SelectMany(e => e.GetProperties());
    }

    private static IEnumerable<IIndex> Indexes(JellyfinDbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model.GetEntityTypes().SelectMany(e => e.GetIndexes());
    }
}
