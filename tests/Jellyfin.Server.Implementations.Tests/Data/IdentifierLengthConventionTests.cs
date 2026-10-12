using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Conventions;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

public class IdentifierLengthConventionTests
{
    private const int Limit = 30;
    private const int MaxLength = 63;

    [Fact]
    public void Names_WithinTheLimit_AreKept()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains("key:Orders.PK_Orders", names);
        Assert.Contains("index:Orders.IX_Orders_CustomerIdentifier", names);
        Assert.Contains("column:Orders.CustomerId", names);
    }

    [Fact]
    public void ForeignKey_ExactlyAtTheLimit_IsKept()
    {
        var name = "FK_Orders_Customers_CustomerId";
        Assert.Equal(Limit, name.Length);

        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"fk:Orders.{name}", names);
    }

    [Fact]
    public void Index_OverTheLimit_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"index:Orders.{Shorten("IX_Orders_CustomerId_ShippingReferenceNumber")}", names);
    }

    [Fact]
    public void ForeignKey_OverTheLimit_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"fk:Orders.{Shorten("FK_Orders_Customers_CustomerIdentifier")}", names);
    }

    [Fact]
    public void Key_OverTheLimit_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"key:{Shorten("TheTableNameIsLongerThanTheLimit")}.{Shorten("PK_TheTableNameIsLongerThanTheLimit")}", names);
    }

    [Fact]
    public void Column_OverTheLimit_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"column:Orders.{Shorten("ColumnNameThatIsLongerThanTheLimit")}", names);
    }

    [Fact]
    public void Table_OverTheLimit_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"table:{Shorten("TheTableNameIsLongerThanTheLimit")}", names);
    }

    [Fact]
    public void Index_ExplicitlyNamedOverTheLimit_IsShortenedFromThatName()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        Assert.Contains($"index:Orders.{Shorten("IX_ExplicitlyChosenNameOverTheLimit")}", names);
    }

    [Fact]
    public void Names_SharingTheirLeadingCharacters_StayDistinctWithoutTildes()
    {
        var names = Describe(BuildModel(Limit, ConfigureOrders));

        var indexNames = names.Where(n => n.StartsWith("index:Orders.", StringComparison.Ordinal)).ToList();
        Assert.Equal(indexNames.Count, indexNames.Distinct(StringComparer.Ordinal).Count());
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
        Assert.Contains($"index:Orders.{Shorten("IX_Orders_SharedLeadingCharacters_First")}", names);
        Assert.Contains($"index:Orders.{Shorten("IX_Orders_SharedLeadingCharacters_Second")}", names);
    }

    [Fact]
    public void Names_DoNotDependOnTheOrderTheModelIsBuilt()
    {
        var forward = Describe(BuildModel(Limit, ConfigureOrders));
        var reversed = Describe(BuildModel(Limit, builder => ConfigureOrders(builder, reverse: true)));

        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void RealModel_WithALimitAboveEveryName_IsUnchanged()
    {
        var baseline = Describe(GetModel<JellyfinDbContext>());
        var withConvention = Describe(GetModel<Limit128Context>());

        Assert.Equal(baseline, withConvention);
    }

    [Fact]
    public void RealModel_WithTheLimitOfPostgres_RenamesOnlyTheIndexesOverTheLimit()
    {
        var baseline = Describe(GetModel<JellyfinDbContext>());
        var withConvention = Describe(GetModel<Limit63Context>());

        var tooLong = baseline.Where(n => Encoding.UTF8.GetByteCount(NameOf(n)) > 63).ToList();
        Assert.NotEmpty(tooLong);
        Assert.All(tooLong, n => Assert.StartsWith("index:BaseItems.", n, StringComparison.Ordinal));

        var removed = baseline.Except(withConvention).ToList();
        var added = withConvention.Except(baseline).ToList();
        Assert.Equal(tooLong.Order(StringComparer.Ordinal), removed.Order(StringComparer.Ordinal));
        Assert.Equal(tooLong.Count, added.Count);
        Assert.All(added, n => Assert.True(Encoding.UTF8.GetByteCount(NameOf(n)) <= 63));
        Assert.All(added, n => Assert.Matches("_[0-9a-f]{8}$", n));
    }

    [Fact]
    public void Constructor_LimitTooSmallForTheSuffix_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdentifierLengthConvention(9));
    }

    [Fact]
    public void Indexes_SharingTheirFirst62Characters_GetDistinctHashedNamesWithoutTildes()
    {
        var (first, second) = CollidingIndexNames();
        Assert.Equal(first[..62], second[..62]);
        Assert.NotEqual(first[..63], second[..63]);

        var names = Describe(BuildModel(MaxLength, builder => ConfigureCollidingIndexes(builder, reverse: false)));

        var indexNames = names.Where(n => n.StartsWith("index:Parcels.", StringComparison.Ordinal)).ToList();
        Assert.Contains($"index:Parcels.{Shorten(first, MaxLength)}", indexNames);
        Assert.Contains($"index:Parcels.{Shorten(second, MaxLength)}", indexNames);
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
        Assert.Equal(indexNames.Count, indexNames.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Indexes_SharingTheirFirst62Characters_DoNotDependOnTheOrderTheModelIsBuilt()
    {
        var forward = Describe(BuildModel(MaxLength, builder => ConfigureCollidingIndexes(builder, reverse: false)));
        var reversed = Describe(BuildModel(MaxLength, builder => ConfigureCollidingIndexes(builder, reverse: true)));

        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void Index_ShortenedNameEqualToAnotherIndex_ThrowsNamingBoth()
    {
        var (first, _) = CollidingIndexNames();
        var shortened = Shorten(first, MaxLength);

        var exception = Assert.Throws<InvalidOperationException>(() => BuildModel(
            MaxLength,
            builder =>
            {
                var parcels = builder.Entity<Parcel>();
                parcels.ToTable("Parcels");
                parcels.Property<int>(ParcelColumns.Shared);
                parcels.Property<int>(ParcelColumns.Alpha);
                parcels.Property<int>(ParcelColumns.Beta);
                parcels.HasIndex(ParcelColumns.Shared, ParcelColumns.Alpha);
                parcels.HasIndex(ParcelColumns.Beta).HasDatabaseName(shortened);
            }));

        Assert.Contains(first, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"'{shortened}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Parcels", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Column_ShortenedNameEqualToAnotherColumn_Throws()
    {
        var longName = new string('c', MaxLength + 5);
        var shortened = Shorten(longName, MaxLength);

        var exception = Assert.Throws<InvalidOperationException>(() => BuildModel(
            MaxLength,
            builder =>
            {
                var parcels = builder.Entity<Parcel>();
                parcels.ToTable("Parcels");
                parcels.Property<int>("A").HasColumnName(longName);
                parcels.Property<int>("B").HasColumnName(shortened);
            }));

        Assert.Contains(longName, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"'{shortened}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Index_NameOf54CharactersAnd75Bytes_IsShortenedToFitInBytes()
    {
        var name = new string('a', 33) + new string('\u00e9', 21);
        Assert.Equal(54, name.Length);
        Assert.Equal(75, Encoding.UTF8.GetByteCount(name));

        var shortened = IndexNameAfterBuilding(name);

        Assert.Equal(name[..43] + "_" + HashOf(name), shortened);
        Assert.Equal(62, Encoding.UTF8.GetByteCount(shortened));
    }

    [Fact]
    public void Index_NameCutInsideAMultiByteCharacter_IsCutBeforeThatCharacter()
    {
        // 34 ASCII bytes and ten three-byte characters: the 54 byte budget ends after two bytes of the seventh one.
        var name = new string('a', 34) + new string('\u20ac', 10);
        Assert.Equal(44, name.Length);
        Assert.Equal(64, Encoding.UTF8.GetByteCount(name));

        var shortened = IndexNameAfterBuilding(name);

        Assert.Equal(name[..40] + "_" + HashOf(name), shortened);
        Assert.Equal(61, Encoding.UTF8.GetByteCount(shortened));
        Assert.DoesNotContain('\uFFFD', shortened);
    }

    [Fact]
    public void Index_NameCutInsideASurrogatePair_KeepsThePairWhole()
    {
        // 52 ASCII bytes and three four-byte characters: only two bytes of the budget are left for the first of them.
        var name = new string('a', 52) + string.Concat(Enumerable.Repeat("\U0001F600", 3));
        Assert.Equal(58, name.Length);
        Assert.Equal(64, Encoding.UTF8.GetByteCount(name));

        var shortened = IndexNameAfterBuilding(name);

        Assert.Equal(name[..52] + "_" + HashOf(name), shortened);
        Assert.DoesNotContain(shortened, char.IsSurrogate);
    }

    [Fact]
    public void Index_NameOfExactlyTheLimitInBytes_IsKept()
    {
        var name = new string('\u20ac', 21);
        Assert.Equal(63, Encoding.UTF8.GetByteCount(name));

        Assert.Equal(name, IndexNameAfterBuilding(name));
    }

    [Fact]
    public void Index_NameOneByteOverTheLimit_IsShortened()
    {
        var name = new string('\u20ac', 21) + "a";
        Assert.Equal(64, Encoding.UTF8.GetByteCount(name));

        var shortened = IndexNameAfterBuilding(name);

        Assert.Equal(name[..18] + "_" + HashOf(name), shortened);
    }

    [Fact]
    public void Index_NameFromADataAnnotationOverTheLimit_IsShortenedFromThatName()
    {
        var names = Describe(BuildModel(Limit, builder => builder.Entity<Annotated>().ToTable("Annotated")));

        Assert.Contains($"index:Annotated.{Shorten("IX_NamedByADataAnnotationOverTheLimit")}", names);
    }

    [Fact]
    public void Table_OfADerivedTypeInTablePerType_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, builder =>
        {
            builder.Entity<Vehicle>().ToTable("Vehicles").UseTptMappingStrategy();
            builder.Entity<VehicleWithAnUnusuallyLongName>().ToTable("TheDerivedTableNameIsLongerThanTheLimit");
        }));

        var table = Shorten("TheDerivedTableNameIsLongerThanTheLimit");
        Assert.Contains($"table:{table}", names);
        Assert.DoesNotContain("table:TheDerivedTableNameIsLongerThanTheLimit", names);
    }

    [Fact]
    public void Table_OfADerivedTypeInTablePerConcreteType_BecomesPrefixUnderscoreHash()
    {
        var names = Describe(BuildModel(Limit, builder =>
        {
            builder.Entity<Vehicle>().ToTable("Vehicles").UseTpcMappingStrategy();
            builder.Entity<VehicleWithAnUnusuallyLongName>().ToTable("TheDerivedTableNameIsLongerThanTheLimit");
        }));

        var table = Shorten("TheDerivedTableNameIsLongerThanTheLimit");
        Assert.Contains($"table:{table}", names);
        Assert.DoesNotContain("table:TheDerivedTableNameIsLongerThanTheLimit", names);
    }

    [Fact]
    public void Table_SharedByTwoEntityTypes_IsShortenedOnceForBoth()
    {
        var model = BuildModel(Limit, builder =>
        {
            builder.Entity<Customer>().ToTable("TheTableNameIsLongerThanTheLimit");
            builder.Entity<Customer>().OwnsOne(c => c.Address, a => a.ToTable("TheTableNameIsLongerThanTheLimit"));
        });

        var tables = model.GetEntityTypes().Select(e => e.GetTableName()).Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal([Shorten("TheTableNameIsLongerThanTheLimit")], tables);
    }

    [Fact]
    public void DefaultNames_OverTheLimitOfThePostgresProvider_AreShortenedFromTheFullNames()
    {
        var table = nameof(ShipmentReturnAuthorizationsAwaitingWarehouseInspectionAndRefundApproval);
        var column = nameof(ShipmentReturnAuthorizationsAwaitingWarehouseInspectionAndRefundApproval.ReferenceToTheOriginalPurchaseOrderInTheAccountingSystemOfTheRegion);
        var shortTable = Shorten(table, MaxLength);

        var names = Describe(BuildModel(MaxLength, ConfigureLongDefaultNames));

        Assert.True(Encoding.UTF8.GetByteCount(table) > MaxLength);
        Assert.True(Encoding.UTF8.GetByteCount(column) > MaxLength);
        Assert.Contains($"table:{shortTable}", names);
        Assert.Contains($"column:{shortTable}.{Shorten(column, MaxLength)}", names);
        Assert.Contains($"key:{shortTable}.{Shorten("PK_" + table, MaxLength)}", names);
        Assert.Contains($"key:{shortTable}.{Shorten($"AK_{table}_Code", MaxLength)}", names);
        Assert.Contains($"index:{shortTable}.{Shorten($"IX_{table}_{column}", MaxLength)}", names);
        Assert.Contains($"fk:{shortTable}.{Shorten($"FK_{table}_Suppliers_SupplierId", MaxLength)}", names);
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
        Assert.All(names, n => Assert.True(Encoding.UTF8.GetByteCount(NameOf(n)) <= MaxLength, n));
    }

    [Fact]
    public void DefaultColumns_SharingTheirFirst70Characters_AreShortenedFromTheirOwnFullNames()
    {
        var shared = new string('S', 70);
        var first = shared + "First";
        var second = shared + "Second";

        var names = Describe(BuildModel(MaxLength, builder =>
        {
            var parcels = builder.Entity<Parcel>();
            parcels.ToTable("Parcels");
            parcels.Property<int>(first);
            parcels.Property<int>(second);
            parcels.HasIndex(first, second);
        }));

        Assert.Contains($"column:Parcels.{Shorten(first, MaxLength)}", names);
        Assert.Contains($"column:Parcels.{Shorten(second, MaxLength)}", names);
        Assert.Contains($"index:Parcels.{Shorten($"IX_Parcels_{first}_{second}", MaxLength)}", names);
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultNames_WithMultiByteCharacters_AreShortenedFromTheFullNamesInBytes()
    {
        var column = new string('\u00e9', 70);
        var table = new string('\u00e9', 70) + "Table";
        var index = $"IX_{table}_{column}";
        Assert.Equal(140, Encoding.UTF8.GetByteCount(column));

        var names = Describe(BuildModel(MaxLength, builder =>
        {
            var entity = builder.Entity(table);
            entity.Property<int>("Id");
            entity.HasKey("Id");
            entity.Property<int>(column);
            entity.HasIndex(column);
        }));

        // The budget of 54 bytes holds 27 two-byte characters, and 3 bytes of the prefix and 25 characters of the table.
        var shortTable = table[..27] + "_" + HashOf(table);
        Assert.Contains($"table:{shortTable}", names);
        Assert.Contains($"column:{shortTable}.{column[..27]}_{HashOf(column)}", names);
        Assert.Contains($"key:{shortTable}.PK_{table[..25]}_{HashOf("PK_" + table)}", names);
        Assert.Contains($"index:{shortTable}.IX_{table[..25]}_{HashOf(index)}", names);
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
        Assert.All(names, n => Assert.True(Encoding.UTF8.GetByteCount(NameOf(n)) <= MaxLength, n));
    }

    [Fact]
    public void DefaultColumn_OfAnOwnedTypeSharingTheTable_IsShortenedFromTheNameWithTheNavigation()
    {
        var column = new string('L', 70);

        var names = Describe(BuildModel(MaxLength, builder =>
        {
            builder.Entity<Customer>().ToTable("Customers");
            builder.Entity<Customer>().OwnsOne(c => c.Address, a => a.Property<int>(column));
        }));

        Assert.Contains($"column:Customers.{Shorten($"Address_{column}", MaxLength)}", names);
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultNames_OverTheLimitOfThePostgresProvider_DoNotDependOnTheOrderTheModelIsBuilt()
    {
        var forward = Describe(BuildModel(MaxLength, ConfigureLongDefaultNames));
        var reversed = Describe(BuildModel(MaxLength, builder => ConfigureLongDefaultNames(builder, reverse: true)));
        var again = Describe(BuildModel(MaxLength, ConfigureLongDefaultNames));

        Assert.Equal(forward, reversed);
        Assert.Equal(forward, again);
    }

    [Fact]
    public void ProviderModel_HasNoNameOverTheLimitOrWithATilde()
    {
        var model = GetNpgsqlModel();
        var names = Describe(model).Select(NameOf).ToList();

        Assert.NotEmpty(names);
        Assert.All(names, n => Assert.True(Encoding.UTF8.GetByteCount(n) <= 63, n));
        Assert.DoesNotContain(names, n => n.Contains('~', StringComparison.Ordinal));
    }

    [Fact]
    public void ProviderModel_NamesThatWereOverTheLimit_AreTheHashedNamesOfTheirFullNames()
    {
        var full = Describe(GetModel<JellyfinDbContext>()).Where(n => Encoding.UTF8.GetByteCount(NameOf(n)) > 63).ToList();
        var provider = Describe(GetNpgsqlModel());

        Assert.NotEmpty(full);
        foreach (var description in full)
        {
            var scope = description[..(description.LastIndexOf('.') + 1)];
            Assert.Contains(scope + Shorten(NameOf(description), 63), provider);
        }
    }

    internal static string HashOf(string name) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];

    private static string IndexNameAfterBuilding(string name)
    {
        var model = BuildModel(MaxLength, builder =>
        {
            var parcels = builder.Entity<Parcel>();
            parcels.ToTable("Parcels");
            parcels.Property<int>(ParcelColumns.Beta);
            parcels.HasIndex(ParcelColumns.Beta).HasDatabaseName(name);
        });

        return model.FindEntityType(typeof(Parcel))!.GetIndexes().Single().GetDatabaseName()!;
    }

    private static (string First, string Second) CollidingIndexNames() =>
        ($"IX_Parcels_{ParcelColumns.Shared}_{ParcelColumns.Alpha}", $"IX_Parcels_{ParcelColumns.Shared}_{ParcelColumns.Beta}");

    private static void ConfigureLongDefaultNames(ModelBuilder builder) => ConfigureLongDefaultNames(builder, reverse: false);

    private static void ConfigureLongDefaultNames(ModelBuilder builder, bool reverse)
    {
        var shipments = builder.Entity<ShipmentReturnAuthorizationsAwaitingWarehouseInspectionAndRefundApproval>();
        var steps = new Action[]
        {
            () => builder.Entity<Supplier>().ToTable("Suppliers"),
            () => shipments.HasAlternateKey(s => s.Code),
            () => shipments.HasIndex(s => s.ReferenceToTheOriginalPurchaseOrderInTheAccountingSystemOfTheRegion),
            () => shipments.HasOne<Supplier>().WithMany().HasForeignKey(s => s.SupplierId)
        };
        foreach (var step in reverse ? steps.Reverse() : steps)
        {
            step();
        }
    }

    private static void ConfigureCollidingIndexes(ModelBuilder builder, bool reverse)
    {
        var parcels = builder.Entity<Parcel>();
        parcels.ToTable("Parcels");
        parcels.Property<int>(ParcelColumns.Shared);
        parcels.Property<int>(ParcelColumns.Alpha);
        parcels.Property<int>(ParcelColumns.Beta);

        var indexes = new Action[]
        {
            () => parcels.HasIndex(ParcelColumns.Shared, ParcelColumns.Alpha),
            () => parcels.HasIndex(ParcelColumns.Shared, ParcelColumns.Beta)
        };
        foreach (var index in reverse ? indexes.Reverse() : indexes)
        {
            index();
        }
    }

    private static IModel GetNpgsqlModel()
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseNpgsql("Host=localhost")
            .Options;
        var provider = new PostgresDatabaseProvider(NullLogger<PostgresDatabaseProvider>.Instance);
        var locking = new NoLockBehavior(NullLogger<NoLockBehavior>.Instance);
        using var context = new JellyfinDbContext(options, NullLogger<JellyfinDbContext>.Instance, provider, locking);
        return context.GetService<IDesignTimeModel>().Model;
    }

    internal static string Shorten(string name, int limit = Limit)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];
        return name[..(limit - 9)] + "_" + hash;
    }

    internal static List<string> Describe(IModel model)
    {
        var names = new List<string>();
        foreach (var entityType in model.GetEntityTypes().Where(e => e.GetTableName() is not null))
        {
            var table = entityType.GetTableName()!;
            var store = StoreObjectIdentifier.Table(table, entityType.GetSchema());
            names.Add($"table:{table}");

            // The provider names a column that collides with another one for its table only.
            names.AddRange(entityType.GetProperties().Select(p => $"column:{table}.{p.GetColumnName(store) ?? p.GetColumnName()}"));
            names.AddRange(entityType.GetKeys().Select(k => $"key:{table}.{k.GetName()}"));
            names.AddRange(entityType.GetForeignKeys().Select(f => $"fk:{table}.{f.GetConstraintName()}"));
            names.AddRange(entityType.GetIndexes().Select(i => $"index:{table}.{i.GetDatabaseName()}"));
        }

        return names.Order(StringComparer.Ordinal).ToList();
    }

    private static string NameOf(string description)
    {
        // A table is described as "table:name", every other object as "kind:table.name".
        var separator = description.Contains('.', StringComparison.Ordinal)
            ? description.LastIndexOf('.')
            : description.IndexOf(':', StringComparison.Ordinal);
        return description[(separator + 1)..];
    }

    private static IModel BuildModel(int limit, Action<ModelBuilder> configure)
    {
        using var context = new DbContext(new DbContextOptionsBuilder().UseNpgsql("Host=localhost").Options);
        var conventions = context.GetService<IConventionSetBuilder>().CreateConventionSet();
        conventions.ModelFinalizingConventions.Add(new IdentifierLengthConvention(limit));
        var builder = new ModelBuilder(conventions);
        configure(builder);
        return builder.FinalizeModel();
    }

    private static void ConfigureOrders(ModelBuilder builder) => ConfigureOrders(builder, reverse: false);

    private static void ConfigureOrders(ModelBuilder builder, bool reverse)
    {
        void Customers() => builder.Entity<Customer>().ToTable("Customers");

        void LongTable() => builder.Entity<TheTableNameIsLongerThanTheLimit>().ToTable("TheTableNameIsLongerThanTheLimit");

        void Orders()
        {
            var orders = builder.Entity<Order>();
            orders.ToTable("Orders");
            orders.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId);
            orders.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerIdentifier);
            orders.Property(o => o.ColumnNameThatIsLongerThanTheLimit);

            var indexes = new Action[]
            {
                () => orders.HasIndex(o => new { o.CustomerId, o.ShippingReferenceNumber }),
                () => orders.HasIndex(o => new { o.SharedLeadingCharacters, o.First }),
                () => orders.HasIndex(o => new { o.SharedLeadingCharacters, o.Second }),
                () => orders.HasIndex(o => o.Reference).HasDatabaseName("IX_ExplicitlyChosenNameOverTheLimit")
            };
            foreach (var index in reverse ? indexes.Reverse() : indexes)
            {
                index();
            }
        }

        var steps = new Action[] { Customers, Orders, LongTable };
        foreach (var step in reverse ? steps.Reverse() : steps)
        {
            step();
        }
    }

    private static IModel GetModel<TContext>()
        where TContext : JellyfinDbContext
    {
        var options = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var provider = new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance);
        var locking = new NoLockBehavior(NullLogger<NoLockBehavior>.Instance);
        using var context = (TContext)Activator.CreateInstance(typeof(TContext), options, NullLogger<JellyfinDbContext>.Instance, provider, locking)!;
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static class ParcelColumns
    {
        public const string Alpha = "AlphaColumnThatEndsTheSharedPrefix";

        public const string Beta = "BetaColumnThatEndsTheSharedPrefix";

        // "IX_Parcels_", this name and the underscore after it are 62 characters.
        public static readonly string Shared = new('S', 62 - "IX_Parcels_".Length - 1);
    }

    private sealed class Customer
    {
        public int Id { get; set; }

        public Address? Address { get; set; }
    }

    private sealed class Address
    {
        public string? Street { get; set; }
    }

    private sealed class Supplier
    {
        public int Id { get; set; }
    }

    private sealed class ShipmentReturnAuthorizationsAwaitingWarehouseInspectionAndRefundApproval
    {
        public int Id { get; set; }

        public int Code { get; set; }

        public int SupplierId { get; set; }

        public int ReferenceToTheOriginalPurchaseOrderInTheAccountingSystemOfTheRegion { get; set; }
    }

    private sealed class Parcel
    {
        public int Id { get; set; }
    }

    [Index(nameof(Name), Name = "IX_NamedByADataAnnotationOverTheLimit")]
    private sealed class Annotated
    {
        public int Id { get; set; }

        public int Name { get; set; }
    }

    private class Vehicle
    {
        public int Id { get; set; }
    }

    private sealed class VehicleWithAnUnusuallyLongName : Vehicle
    {
        public int Wheels { get; set; }
    }

    private sealed class TheTableNameIsLongerThanTheLimit
    {
        public int Id { get; set; }
    }

    private sealed class Order
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }

        public int CustomerIdentifier { get; set; }

        public int ShippingReferenceNumber { get; set; }

        public int ColumnNameThatIsLongerThanTheLimit { get; set; }

        public int SharedLeadingCharacters { get; set; }

        public int First { get; set; }

        public int Second { get; set; }

        public int Reference { get; set; }
    }

    private sealed class Limit128Context(
        DbContextOptions<JellyfinDbContext> options,
        ILogger<JellyfinDbContext> logger,
        IJellyfinDatabaseProvider provider,
        IEntityFrameworkCoreLockingBehavior locking)
        : JellyfinDbContext(options, logger, provider, locking)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Conventions.Add(_ => new IdentifierLengthConvention(128));
        }
    }

    private sealed class Limit63Context(
        DbContextOptions<JellyfinDbContext> options,
        ILogger<JellyfinDbContext> logger,
        IJellyfinDatabaseProvider provider,
        IEntityFrameworkCoreLockingBehavior locking)
        : JellyfinDbContext(options, logger, provider, locking)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);
            configurationBuilder.Conventions.Add(_ => new IdentifierLengthConvention(63));
        }
    }
}
