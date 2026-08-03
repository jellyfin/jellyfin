using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryLegacyFilterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly CommandRecordingInterceptor _interceptor = new();
    private readonly BaseItemRepository _repository;
    private readonly string _audioTypeName;
    private readonly string _movieTypeName;

    public BaseItemRepositoryLegacyFilterTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_interceptor)
            .Options;

        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var itemTypeLookup = new ItemTypeLookup();
        _audioTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Audio];
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    // Verifies eligible tags and genres retain normalized result semantics with one direct ItemValues join.
    [Fact]
    public void GetQueryFiltersLegacy_ItemValues_GroupByCleanValueWithOneItemValuesJoin()
    {
        var firstItem = CreateMovieEntity(Guid.NewGuid(), "First");
        var secondItem = CreateMovieEntity(Guid.NewGuid(), "Second");
        var excludedItem = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = _audioTypeName,
            Name = "Excluded Audio",
            MediaType = "Audio",
            IsMovie = false,
            IsFolder = false,
            IsVirtualItem = false
        };
        var firstTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Alpha",
            CleanValue = "alpha"
        };
        var duplicateTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "alpha",
            CleanValue = "alpha"
        };
        var secondTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Beta",
            CleanValue = "beta"
        };
        var genre = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Genre Leak",
            CleanValue = "genre leak"
        };
        var excludedTag = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = "Excluded Tag",
            CleanValue = "excluded tag"
        };
        var excludedGenre = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Genre,
            Value = "Excluded Genre",
            CleanValue = "excluded genre"
        };

        using (var context = CreateDbContext())
        {
            context.BaseItems.AddRange(firstItem, secondItem, excludedItem);
            context.ItemValues.AddRange(firstTag, duplicateTag, secondTag, genre, excludedTag, excludedGenre);
            context.ItemValuesMap.AddRange(
                CreateMap(firstItem, firstTag),
                CreateMap(firstItem, duplicateTag),
                CreateMap(secondItem, secondTag),
                CreateMap(firstItem, genre),
                CreateMap(excludedItem, excludedTag),
                CreateMap(excludedItem, excludedGenre));
            context.SaveChanges();
        }

        var result = _repository.GetQueryFiltersLegacy(new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Equal(["Alpha", "Beta"], result.Tags);
        Assert.Equal(["Genre Leak"], result.Genres);
        var tagsCommand = Assert.Single(
            _interceptor.Commands,
            command => command.Contains("GROUP BY", StringComparison.Ordinal)
                && command.Contains("\"Type\" = 4", StringComparison.Ordinal));
        var genresCommand = Assert.Single(
            _interceptor.Commands,
            command => command.Contains("GROUP BY", StringComparison.Ordinal)
                && command.Contains("\"Type\" = 2", StringComparison.Ordinal));
        Assert.Equal(1, CountOccurrences(tagsCommand, "INNER JOIN \"ItemValues\""));
        Assert.DoesNotContain("SELECT (", tagsCommand, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(genresCommand, "INNER JOIN \"ItemValues\""));
        Assert.DoesNotContain("SELECT (", genresCommand, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string value, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = value.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private static ItemValueMap CreateMap(BaseItemEntity item, ItemValue itemValue)
    {
        return new ItemValueMap
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue
        };
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private sealed class CommandRecordingInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Commands.Add(command.CommandText);
            return result;
        }
    }
}
