using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.ModelConfiguration;
using Jellyfin.Server.Implementations.Tests.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Every string in a key or an index has to be usable on every database engine: it has a maximum length, so
/// that the index entry has a known size, or it is marked as indexed text without a bound.
/// </summary>
public class IndexedStringBoundsTests
{
    private static readonly Engine[] _engines =
    [
        new("SQL Server", ClusteredKeyBytes: 900, IndexBytes: 1700, BytesPerChar: 2),
        new("MySQL", ClusteredKeyBytes: 3072, IndexBytes: 3072, BytesPerChar: 4),
        new("PostgreSQL", ClusteredKeyBytes: 2704, IndexBytes: 2704, BytesPerChar: 4)
    ];

    private static readonly Dictionary<Type, int> _fixedSizes = new()
    {
        [typeof(bool)] = 1,
        [typeof(byte)] = 1,
        [typeof(sbyte)] = 1,
        [typeof(short)] = 2,
        [typeof(ushort)] = 2,
        [typeof(int)] = 4,
        [typeof(uint)] = 4,
        [typeof(float)] = 4,
        [typeof(DateOnly)] = 4,
        [typeof(long)] = 8,
        [typeof(ulong)] = 8,
        [typeof(double)] = 8,
        [typeof(DateTime)] = 8,
        [typeof(TimeSpan)] = 8,
        [typeof(TimeOnly)] = 8,
        [typeof(DateTimeOffset)] = 10,
        [typeof(Guid)] = 16,
        [typeof(decimal)] = 17
    };

    public static TheoryData<string> Providers => new() { "Sqlite", "Postgres" };

    [Theory]
    [MemberData(nameof(Providers))]
    public void IndexedStrings_AreBoundedOrMarkedUnbounded(string provider)
    {
        using var context = CreateContext(provider);

        Assert.Empty(FindUnclassifiedStrings(context.GetService<IDesignTimeModel>().Model));
    }

    [Theory]
    [MemberData(nameof(Providers))]
    public void Keys_FitTheIndexLimitOfEveryEngine(string provider)
    {
        using var context = CreateContext(provider);

        Assert.Empty(FindOversizedKeys(context.GetService<IDesignTimeModel>().Model));
    }

    [Fact]
    public void IndexedString_WithoutBoundOrMark_IsReported()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name);
            entity.HasIndex(e => e.Name);
        });

        var found = Assert.Single(FindUnclassifiedStrings(model));
        Assert.Contains("Sample.Name", found, StringComparison.Ordinal);
    }

    [Fact]
    public void IndexedString_ThatIsBothBoundedAndMarked_IsReported()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(64).IsUnboundedIndexedText();
            entity.HasIndex(e => e.Name);
        });

        Assert.Single(FindUnclassifiedStrings(model));
    }

    [Fact]
    public void IndexedString_WithBoundOrMark_IsAccepted()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(64);
            entity.Property(e => e.Text).IsUnboundedIndexedText();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Text);
        });

        Assert.Empty(FindUnclassifiedStrings(model));
        Assert.Empty(FindOversizedKeys(model));
    }

    [Fact]
    public void NonIndexedString_WithoutBoundOrMark_IsAccepted()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text);
        });

        Assert.Empty(FindUnclassifiedStrings(model));
    }

    [Fact]
    public void PrimaryKey_OverTheClusteredLimit_IsReportedForTheEnginesThatCannotHoldIt()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.Property(e => e.Name).HasMaxLength(500);
            entity.HasKey(e => e.Name);
        });

        var found = Assert.Single(FindOversizedKeys(model));
        Assert.StartsWith("SQL Server", found, StringComparison.Ordinal);
        Assert.Contains("1000 bytes", found, StringComparison.Ordinal);
    }

    [Fact]
    public void Index_OverTheLimit_IsReportedForTheEnginesThatCannotHoldIt()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(400);
            entity.Property(e => e.Other).HasMaxLength(300);
            entity.HasIndex(e => new { e.Name, e.Other });
        });

        // 700 characters take 1400 bytes on SQL Server and 2800 on the others, which is over only the limit of PostgreSQL.
        var found = Assert.Single(FindOversizedKeys(model));
        Assert.StartsWith("PostgreSQL", found, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(850, false)]
    [InlineData(851, true)]
    public void Index_AtAndOverTheSqlServerLimit(int length, bool reported)
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(length);
            entity.HasIndex(e => e.Name);
        });

        Assert.Equal(reported, FindOversizedKeys(model).Any(f => f.StartsWith("SQL Server", StringComparison.Ordinal)));
    }

    [Fact]
    public void UnboundedIndexedText_IsLeftOutOfTheByteBudget()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(64);
            entity.Property(e => e.Text).IsUnboundedIndexedText();
            entity.HasIndex(e => new { e.Name, e.Text, e.Id });
        });

        Assert.Empty(FindOversizedKeys(model));
    }

    [Fact]
    public void KeyOfAnUnknownType_IsReported()
    {
        var model = BuildModel(builder =>
        {
            var entity = builder.Entity<Sample>();
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Blob).HasMaxLength(16);
            entity.HasIndex(e => e.Blob);
        });

        Assert.Equal(_engines.Length, FindOversizedKeys(model).Count);
    }

    private static JellyfinDbContext CreateContext(string provider)
    {
        return provider == "Postgres" ? OfflineContexts.Postgres() : OfflineContexts.Sqlite();
    }

    private static IReadOnlyModel BuildModel(Action<ModelBuilder> configure)
    {
        var builder = new ModelBuilder();
        configure(builder);
        return builder.Model;
    }

    private static List<string> FindUnclassifiedStrings(IReadOnlyModel model)
    {
        var found = new List<string>();
        foreach (var entityType in model.GetEntityTypes())
        {
            var indexed = entityType.GetKeys().Select(k => (Description: "a key", k.Properties))
                .Concat(entityType.GetIndexes().Select(i => (Description: "an index", i.Properties)));
            foreach (var (description, properties) in indexed)
            {
                foreach (var property in properties.Where(p => p.ClrType == typeof(string)))
                {
                    var bounded = property.GetMaxLength() is not null;
                    var marked = property.IsUnboundedIndexedText();
                    if (bounded == marked)
                    {
                        found.Add($"{entityType.DisplayName()}.{property.Name} in {description} is " + (bounded ? "both bounded and marked as unbounded." : "neither bounded nor marked as unbounded."));
                    }
                }
            }
        }

        return found.Distinct(StringComparer.Ordinal).ToList();
    }

    private static List<string> FindOversizedKeys(IReadOnlyModel model)
    {
        var found = new List<string>();
        foreach (var entityType in model.GetEntityTypes())
        {
            var primaryKey = entityType.FindPrimaryKey();
            var keys = entityType.GetKeys().Select(k => (Properties: k.Properties, Clustered: k == primaryKey))
                .Concat(entityType.GetIndexes().Select(i => (Properties: i.Properties, Clustered: false)));
            foreach (var (properties, clustered) in keys)
            {
                foreach (var engine in _engines)
                {
                    var bytes = SizeOf(properties, engine);
                    var limit = clustered ? engine.ClusteredKeyBytes : engine.IndexBytes;
                    if (bytes is null || bytes > limit)
                    {
                        var size = bytes is null ? "an unknown number of bytes" : $"{bytes} bytes";
                        found.Add($"{engine.Name}: {entityType.DisplayName()} ({string.Join(", ", properties.Select(p => p.Name))}) takes {size}, the limit is {limit}.");
                    }
                }
            }
        }

        return found.Distinct(StringComparer.Ordinal).ToList();
    }

    private static int? SizeOf(IReadOnlyList<IReadOnlyProperty> properties, Engine engine)
    {
        var total = 0;
        foreach (var property in properties)
        {
            if (property.ClrType == typeof(string))
            {
                if (property.IsUnboundedIndexedText())
                {
                    continue;
                }

                if (property.GetMaxLength() is not { } length)
                {
                    return null;
                }

                total += length * engine.BytesPerChar;
                continue;
            }

            var type = property.GetProviderClrType() ?? property.ClrType;
            type = Nullable.GetUnderlyingType(type) ?? type;
            type = type.IsEnum ? Enum.GetUnderlyingType(type) : type;
            if (!_fixedSizes.TryGetValue(type, out var size))
            {
                return null;
            }

            total += size;
        }

        return total;
    }

    private sealed record Engine(string Name, int ClusteredKeyBytes, int IndexBytes, int BytesPerChar);

    private sealed class Sample
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Other { get; set; }

        public string? Text { get; set; }

        public byte[]? Blob { get; set; }
    }
}
