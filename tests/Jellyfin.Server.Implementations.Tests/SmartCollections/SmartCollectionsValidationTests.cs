using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.SmartCollections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Entities = Jellyfin.Database.Implementations.Entities;
using User = Jellyfin.Database.Implementations.Entities.User;

namespace Jellyfin.Server.Implementations.Tests.SmartCollections;

public class SmartCollectionsValidationTests : IDisposable
{
    private Mock<ISmartCollectionsRepository> _mockRepository;
    private Mock<IItemRepository> _mockItemRepository;
    private Mock<IUserManager> _mockUserManager;
    private Mock<ILogger<SmartCollectionsManager>> _mockLogger;
    private IMemoryCache _cache;
    private SmartCollectionsManager _manager;

    public SmartCollectionsValidationTests()
    {
        _mockRepository = new Mock<ISmartCollectionsRepository>();
        _mockItemRepository = new Mock<IItemRepository>();
        _mockUserManager = new Mock<IUserManager>();
        _mockLogger = new Mock<ILogger<SmartCollectionsManager>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _manager = new SmartCollectionsManager(
            _mockRepository.Object,
            _mockUserManager.Object,
            _mockLogger.Object,
            _mockItemRepository.Object,
            _cache);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesCollection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test Collection", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId,
            Limit = 50
        };

        _mockRepository
            .Setup(x => x.CreateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()))
            .ReturnsAsync(collection);

        // Act
        var result = await _manager.CreateAsync(collection, userId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collectionId, result.Id);
        Assert.Equal("Test Collection", result.Name);
        _mockRepository.Verify(
            x => x.CreateSmartCollectionAsync(It.Is<Entities.SmartCollections>(created =>
                created.Name == collection.Name
                && created.UserId.Equals(userId)
                && created.Limit == collection.Limit)),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var collection = new Entities.SmartCollections("Test", Guid.NewGuid(), new Entities.SmartCollectionFilters());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateAsync(collection, "not-a-guid"));
        _mockRepository.Verify(
            x => x.CreateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCollection()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act
        var result = await _manager.GetByIdAsync(collectionId, userId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(collectionId, result.Id);
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidUserId_ThrowsArgumentException()
    {
        var collectionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.GetByIdAsync(collectionId, "invalid-guid"));
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_MissingCollection_ReturnsNull()
    {
        // Arrange
        var collectionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync((Entities.SmartCollections?)null);

        // Act
        var result = await _manager.GetByIdAsync(collectionId, userId.ToString());

        // Assert
        Assert.Null(result);
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
    }

    [Fact]
    public async Task GetAllByUserAsync_ValidUserId_ReturnsCollections()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collections = new List<Entities.SmartCollections>
        {
            new Entities.SmartCollections("Collection 1", userId, new Entities.SmartCollectionFilters()),
            new Entities.SmartCollections("Collection 2", userId, new Entities.SmartCollectionFilters())
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionsForUserAsync(userId))
            .ReturnsAsync(collections);

        // Act
        var result = await _manager.GetAllByUserAsync(userId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockRepository.Verify(x => x.GetSmartCollectionsForUserAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllByUserAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.GetAllByUserAsync("invalid-guid"));
        _mockRepository.Verify(
            x => x.GetSmartCollectionsForUserAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidCollection_UpdatesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var existingCollection = new Entities.SmartCollections("Old Name", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };
        var updatedCollection = new Entities.SmartCollections("New Name", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId,
            Limit = 100
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync(existingCollection);
        _mockRepository
            .Setup(x => x.UpdateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _manager.UpdateAsync(updatedCollection, userId.ToString());

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal(100, result.Limit);
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(
            x => x.UpdateSmartCollectionAsync(It.Is<Entities.SmartCollections>(updated =>
                updated.Id.Equals(collectionId)
                && updated.UserId.Equals(userId)
                && updated.Name == "New Name"
                && updated.Limit == 100)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var collection = new Entities.SmartCollections("Test", Guid.NewGuid(), new Entities.SmartCollectionFilters());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.UpdateAsync(collection, "invalid-guid"));
        _mockRepository.Verify(
            x => x.GetSmartCollectionByIdAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockRepository.Verify(
            x => x.UpdateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MissingCollection_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync((Entities.SmartCollections?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.UpdateAsync(collection, userId.ToString()));
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(
            x => x.UpdateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WrongOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test", ownerId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _manager.UpdateAsync(collection, otherUserId.ToString()));
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(
            x => x.UpdateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ValidCollection_DeletesSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test", userId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync(collection);
        _mockRepository
            .Setup(x => x.DeleteSmartCollectionAsync(collectionId))
            .Returns(Task.CompletedTask);

        // Act
        await _manager.DeleteAsync(collectionId, userId.ToString());

        // Assert
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(x => x.DeleteSmartCollectionAsync(collectionId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.DeleteAsync(Guid.NewGuid(), "invalid-guid"));
        _mockRepository.Verify(
            x => x.GetSmartCollectionByIdAsync(It.IsAny<Guid>()),
            Times.Never);
        _mockRepository.Verify(
            x => x.DeleteSmartCollectionAsync(It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_MissingCollection_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync((Entities.SmartCollections?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.DeleteAsync(collectionId, userId.ToString()));
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(x => x.DeleteSmartCollectionAsync(collectionId), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WrongOwner_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var collectionId = Guid.NewGuid();
        var collection = new Entities.SmartCollections("Test", ownerId, new Entities.SmartCollectionFilters())
        {
            Id = collectionId
        };

        _mockRepository
            .Setup(x => x.GetSmartCollectionByIdAsync(collectionId))
            .ReturnsAsync(collection);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _manager.DeleteAsync(collectionId, otherUserId.ToString()));
        _mockRepository.Verify(x => x.GetSmartCollectionByIdAsync(collectionId), Times.Once);
        _mockRepository.Verify(x => x.DeleteSmartCollectionAsync(collectionId), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_WithValidFilters_ReturnsItemIds()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockUser = new User("testuser", "passwordHash", "passwordSalt")
        {
            Id = userId
        };
        var expectedIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(mockUser);
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns(expectedIds);

        var filters = new Entities.SmartCollectionFilters { MinCommunityRating = 7 };

        // Act
        var result = await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedIds, result);
    }

    [Fact]
    public async Task EvaluateAsync_CacheMiss_CallsItemRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockUser = new User("testuser", "passwordHash", "passwordSalt")
        {
            Id = userId
        };
        var expectedIds = new[] { Guid.NewGuid() };

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(mockUser);
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns(expectedIds);

        // Act
        var result = await _manager.EvaluateAsync(new Entities.SmartCollectionFilters(), userId.ToString(), 50);

        // Assert
        Assert.Equal(expectedIds, result);
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_CacheHit_SkipsItemRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockUser = new User("testuser", "passwordHash", "passwordSalt")
        {
            Id = userId
        };
        var expectedIds = new[] { Guid.NewGuid() };

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(mockUser);
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns(expectedIds);

        var filters = new Entities.SmartCollectionFilters();

        // First call — warms cache
        await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // Reset the mock to verify second call doesn't invoke it
        _mockItemRepository.Reset();

        // Act — second call should hit cache
        var result = await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // Assert
        Assert.Equal(expectedIds, result);
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_CacheUsesUserIdInCacheKey()
    {
        // Arrange
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var filters = new Entities.SmartCollectionFilters();

        _mockUserManager
            .Setup(x => x.GetUserById(firstUserId))
            .Returns(new User("firstuser", "passwordHash", "passwordSalt") { Id = firstUserId });
        _mockUserManager
            .Setup(x => x.GetUserById(secondUserId))
            .Returns(new User("seconduser", "passwordHash", "passwordSalt") { Id = secondUserId });
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns([Guid.NewGuid()]);

        // Act
        await _manager.EvaluateAsync(filters, firstUserId.ToString(), 50);
        await _manager.EvaluateAsync(filters, secondUserId.ToString(), 50);

        // Assert
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateAsync_CacheUsesLimitInCacheKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new Entities.SmartCollectionFilters();

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(new User("testuser", "passwordHash", "passwordSalt") { Id = userId });
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns([Guid.NewGuid()]);

        // Act
        await _manager.EvaluateAsync(filters, userId.ToString(), 25);
        await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // Assert
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateAsync_CacheUsesFiltersInCacheKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var firstFilters = new Entities.SmartCollectionFilters { MinCommunityRating = 7 };
        var secondFilters = new Entities.SmartCollectionFilters { MinCommunityRating = 8 };

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(new User("testuser", "passwordHash", "passwordSalt") { Id = userId });
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Returns([Guid.NewGuid()]);

        // Act
        await _manager.EvaluateAsync(firstFilters, userId.ToString(), 50);
        await _manager.EvaluateAsync(secondFilters, userId.ToString(), 50);

        // Assert
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EvaluateAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.EvaluateAsync(filters, "invalid-guid", 50));
    }

    [Fact]
    public async Task EvaluateAsync_UserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns((User)null!);

        var filters = new Entities.SmartCollectionFilters();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.EvaluateAsync(filters, userId.ToString(), 50));
    }

    [Fact]
    public async Task EvaluateAsync_UserNotFound_DoesNotCacheFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var filters = new Entities.SmartCollectionFilters();

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns((User)null!);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.EvaluateAsync(filters, userId.ToString(), 50));
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.EvaluateAsync(filters, userId.ToString(), 50));

        _mockUserManager.Verify(x => x.GetUserById(userId), Times.Exactly(2));
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateAsync_WithGenreFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters();
        filters.Genres.Add("Action");
        filters.Genres.Add("Drama");

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(new[] { "Action", "Drama" }, query.Genres);
    }

    [Fact]
    public async Task EvaluateAsync_WithTagFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters();
        filters.Tags.Add("4k");
        filters.Tags.Add("Favorite");

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(new[] { "4k", "Favorite" }, query.Tags);
    }

    [Fact]
    public async Task EvaluateAsync_WithYearRangeFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters
        {
            YearFrom = 2018,
            YearTo = 2020
        };

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(new[] { 2018, 2019, 2020 }, query.Years);
    }

    [Fact]
    public async Task EvaluateAsync_WithMinCommunityRatingFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters { MinCommunityRating = 7.5f };

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(7.5f, query.MinCommunityRating);
    }

    [Fact]
    public async Task EvaluateAsync_WithMinCriticRatingFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters { MinCriticRating = 8.2f };

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(8.2f, query.MinCriticRating);
    }

    [Fact]
    public async Task EvaluateAsync_WithOfficialRatingsFilter_MapsToQuery()
    {
        // Arrange
        var filters = new Entities.SmartCollectionFilters();
        filters.OfficialRatings.Add("PG-13");
        filters.OfficialRatings.Add("R");

        // Act
        var query = await EvaluateAndCaptureQuery(filters);

        // Assert
        Assert.Equal(new[] { "PG-13", "R" }, query.OfficialRatings);
    }

    [Fact]
    public async Task EvaluateAsync_AppliesLimitToInternalItemsQuery()
    {
        // Arrange
        var limit = 25;

        // Act
        var query = await EvaluateAndCaptureQuery(new Entities.SmartCollectionFilters(), limit);

        // Assert
        Assert.Equal(limit, query.Limit);
    }

    [Fact]
    public async Task EvaluateAsync_WithEmptyFilters_UsesUnfilteredQuery()
    {
        // Act
        var query = await EvaluateAndCaptureQuery(new Entities.SmartCollectionFilters());

        // Assert
        Assert.Empty(query.Genres);
        Assert.Empty(query.Tags);
        Assert.Empty(query.Years);
        Assert.Empty(query.OfficialRatings);
        Assert.Null(query.MinCommunityRating);
        Assert.Null(query.MinCriticRating);
        Assert.Equal(50, query.Limit);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache?.Dispose();
        }
    }

    private async Task<InternalItemsQuery> EvaluateAndCaptureQuery(Entities.SmartCollectionFilters filters, int limit = 50)
    {
        var userId = Guid.NewGuid();
        var mockUser = new User("testuser", "passwordHash", "passwordSalt")
        {
            Id = userId
        };
        var expectedIds = new[] { Guid.NewGuid() };
        InternalItemsQuery? capturedQuery = null;

        _mockUserManager
            .Setup(x => x.GetUserById(userId))
            .Returns(mockUser);
        _mockItemRepository
            .Setup(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(query => capturedQuery = query)
            .Returns(expectedIds);

        await _manager.EvaluateAsync(filters, userId.ToString(), limit);

        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Once);
        return Assert.IsType<InternalItemsQuery>(capturedQuery);
    }
}
