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
        _mockRepository.Verify(x => x.CreateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Arrange
        var collection = new Entities.SmartCollections("Test", Guid.NewGuid(), new Entities.SmartCollectionFilters());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateAsync(collection, "not-a-guid"));
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
    }

    [Fact]
    public async Task GetByIdAsync_InvalidUserId_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.GetByIdAsync(Guid.NewGuid(), "invalid-guid"));
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
        _mockRepository.Verify(x => x.UpdateSmartCollectionAsync(It.IsAny<Entities.SmartCollections>()), Times.Once);
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
        _mockRepository.Verify(x => x.DeleteSmartCollectionAsync(collectionId), Times.Once);
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
    public async Task EvaluateAsync_WithGenreFilter_MapsToQuery()
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
            .Callback<InternalItemsQuery>(q =>
            {
                Assert.NotNull(q.Genres);
                Assert.Contains("Action", q.Genres);
            })
            .Returns(expectedIds);

        var filters = new Entities.SmartCollectionFilters();
        filters.Genres.Add("Action");

        // Act
        await _manager.EvaluateAsync(filters, userId.ToString(), 50);

        // Assert
        _mockItemRepository.Verify(x => x.GetItemIdsList(It.IsAny<InternalItemsQuery>()), Times.Once);
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
}
