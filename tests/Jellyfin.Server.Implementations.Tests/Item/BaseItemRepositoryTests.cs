using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public class BaseItemRepositoryTests
{
    private static readonly string VideoTypeName = typeof(Video).FullName!;

    [Fact]
    public void DeserializeBaseItem_WithUnknownType_ReturnsNull()
    {
        // Arrange
        var entity = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "NonExistent.Plugin.CustomItemType"
        };

        // Act
        var result = BaseItemRepository.DeserializeBaseItem(entity, NullLogger.Instance, null, false);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeBaseItem_WithUnknownType_LogsWarning()
    {
        // Arrange
        var entity = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "NonExistent.Plugin.CustomItemType"
        };
        var loggerMock = new Mock<ILogger>();

        // Act
        BaseItemRepository.DeserializeBaseItem(entity, loggerMock.Object, null, false);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unknown type", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void DeserializeBaseItem_WithKnownType_ReturnsItem()
    {
        // Arrange
        var entity = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.Movies.Movie"
        };

        // Act
        var result = BaseItemRepository.DeserializeBaseItem(entity, NullLogger.Instance, null, false);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void TranslateQuery_ResumableOwnedAdditionalPart_IncludesOwnedVideoWithoutPrimaryVersion()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = CreateDbContext(dbOptions);
        context.Database.EnsureCreated();

        var user = new User("test", "Default", "Default");
        var owner = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = VideoTypeName,
            PresentationUniqueKey = "movie"
        };
        var additionalPart = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = VideoTypeName,
            OwnerId = owner.Id,
            PresentationUniqueKey = "movie-part-2"
        };
        var alternateVersion = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = VideoTypeName,
            OwnerId = owner.Id,
            PrimaryVersionId = owner.Id,
            PresentationUniqueKey = "movie-alt"
        };

        context.Users.Add(user);
        context.BaseItems.AddRange(owner, additionalPart, alternateVersion);
        context.UserData.AddRange(
            CreateUserData(user, additionalPart),
            CreateUserData(user, alternateVersion));
        context.SaveChanges();

        var query = new InternalItemsQuery(user)
        {
            IsResumable = true
        };
        var repository = CreateRepository(context);
        repository.PrepareFilterQuery(query);

        // Act
        var itemIds = repository
            .TranslateQuery(context.BaseItems.AsNoTracking(), context, query)
            .Select(e => e.Id)
            .ToArray();

        // Assert
        Assert.Contains(additionalPart.Id, itemIds);
        Assert.DoesNotContain(alternateVersion.Id, itemIds);
    }

    private static JellyfinDbContext CreateDbContext(DbContextOptions<JellyfinDbContext> dbOptions)
    {
        return new JellyfinDbContext(
            dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private static BaseItemRepository CreateRepository(JellyfinDbContext context)
    {
        var dbProvider = new Mock<IDbContextFactory<JellyfinDbContext>>();
        dbProvider.Setup(f => f.CreateDbContext()).Returns(context);

        var itemTypeLookup = new Mock<IItemTypeLookup>();
        itemTypeLookup.SetupGet(i => i.BaseItemKindNames).Returns(new Dictionary<BaseItemKind, string>());
        itemTypeLookup.SetupGet(i => i.MusicGenreTypes).Returns(Array.Empty<string>());

        return new BaseItemRepository(
            dbProvider.Object,
            Mock.Of<IServerApplicationHost>(),
            itemTypeLookup.Object,
            Mock.Of<IServerConfigurationManager>(),
            NullLogger<BaseItemRepository>.Instance);
    }

    private static UserData CreateUserData(User user, BaseItemEntity item)
    {
        return new UserData
        {
            CustomDataKey = item.Id.ToString("N"),
            Item = item,
            ItemId = item.Id,
            PlaybackPositionTicks = 1,
            User = user,
            UserId = user.Id
        };
    }
}
