using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using Jellyfin.Api.Models.SmartCollectionDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.SmartCollections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using SmartCollectionEntity = Jellyfin.Database.Implementations.Entities.SmartCollections;
using SmartCollectionFilters = Jellyfin.Database.Implementations.Entities.SmartCollectionFilters;

namespace Jellyfin.Api.Tests.Controllers;

public class SmartCollectionsControllerTests
{
    private readonly Mock<ISmartCollectionsManager> _smartCollectionsManager = new();

    [Fact]
    public async Task CreateSmartCollection_ValidRequest_CreatesForCurrentUser()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSmartCollectionDto
        {
            Name = "Romance Movies",
            Filters = JsonSerializer.SerializeToElement(new SmartCollectionFilters
            {
                MinCommunityRating = 7
            }),
            Limit = 25,
            SortOrder = [SortOrder.Descending]
        };

        _smartCollectionsManager
            .Setup(manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), userId.ToString()))
            .ReturnsAsync((SmartCollectionEntity entity, string _) => entity);

        var controller = CreateController(userId);

        var response = await controller.CreateSmartCollection(request);

        var dto = Assert.IsType<SmartCollectionDto>(response.Value);
        Assert.Equal("Romance Movies", dto.Name);
        Assert.Equal(userId, dto.UserId);
        Assert.Equal(25, dto.Limit);
        Assert.Equal(new[] { SortOrder.Descending }, dto.SortOrder);

        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(
                It.Is<SmartCollectionEntity>(entity =>
                    entity.Name == request.Name
                    && entity.UserId.Equals(userId)
                    && entity.Limit == request.Limit
                    && entity.SortOrder == SortOrder.Descending),
                userId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSmartCollection_EmptyName_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());
        var request = new CreateSmartCollectionDto
        {
            Name = " ",
            Filters = JsonSerializer.SerializeToElement(new SmartCollectionFilters())
        };

        var response = await controller.CreateSmartCollection(request);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateSmartCollection_NonObjectFilters_ReturnsBadRequest()
    {
        var controller = CreateController(Guid.NewGuid());
        var request = new CreateSmartCollectionDto
        {
            Name = "Invalid Filters",
            Filters = JsonSerializer.SerializeToElement("not-an-object")
        };

        var response = await controller.CreateSmartCollection(request);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), It.IsAny<string>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CreateSmartCollection_InvalidLimit_ReturnsBadRequest(int limit)
    {
        var controller = CreateController(Guid.NewGuid());
        var request = new CreateSmartCollectionDto
        {
            Name = "Invalid Limit",
            Filters = JsonSerializer.SerializeToElement(new SmartCollectionFilters()),
            Limit = limit
        };

        var response = await controller.CreateSmartCollection(request);

        Assert.IsType<BadRequestObjectResult>(response.Result);
        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateSmartCollection_WithoutLimit_UsesDefaultLimit()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSmartCollectionDto
        {
            Name = "Default Limit",
            Filters = JsonSerializer.SerializeToElement(new SmartCollectionFilters())
        };

        _smartCollectionsManager
            .Setup(manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), userId.ToString()))
            .ReturnsAsync((SmartCollectionEntity entity, string _) => entity);

        var controller = CreateController(userId);

        var response = await controller.CreateSmartCollection(request);

        var dto = Assert.IsType<SmartCollectionDto>(response.Value);
        Assert.Equal(50, dto.Limit);
        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(
                It.Is<SmartCollectionEntity>(entity => entity.Limit == 50),
                userId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task CreateSmartCollection_WithSortByAndSortOrder_PersistsSortOptions()
    {
        var userId = Guid.NewGuid();
        var request = new CreateSmartCollectionDto
        {
            Name = "Sorted Collection",
            Filters = JsonSerializer.SerializeToElement(new SmartCollectionFilters()),
            SortBy = [ItemSortBy.SortName, ItemSortBy.ProductionYear],
            SortOrder = [SortOrder.Descending, SortOrder.Ascending]
        };

        _smartCollectionsManager
            .Setup(manager => manager.CreateAsync(It.IsAny<SmartCollectionEntity>(), userId.ToString()))
            .ReturnsAsync((SmartCollectionEntity entity, string _) => entity);

        var controller = CreateController(userId);

        var response = await controller.CreateSmartCollection(request);

        var dto = Assert.IsType<SmartCollectionDto>(response.Value);
        Assert.Equal(new[] { ItemSortBy.SortName, ItemSortBy.ProductionYear }, dto.SortBy);
        Assert.Equal(new[] { SortOrder.Descending }, dto.SortOrder);
        _smartCollectionsManager.Verify(
            manager => manager.CreateAsync(
                It.Is<SmartCollectionEntity>(entity =>
                    entity.SortBy == "SortName,ProductionYear"
                    && entity.SortOrder == SortOrder.Descending),
                userId.ToString()),
            Times.Once);
    }

    [Fact]
    public async Task GetSmartCollections_ReturnsCurrentUserCollections()
    {
        var userId = Guid.NewGuid();
        var smartCollections = new List<SmartCollectionEntity>
        {
            new("Collection 1", userId, new SmartCollectionFilters()),
            new("Collection 2", userId, new SmartCollectionFilters())
        };

        _smartCollectionsManager
            .Setup(manager => manager.GetAllByUserAsync(userId.ToString()))
            .ReturnsAsync(smartCollections);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollections();

        var dtos = Assert.IsAssignableFrom<IReadOnlyList<SmartCollectionDto>>(response.Value);
        Assert.Equal(2, dtos.Count);
        Assert.All(dtos, dto => Assert.Equal(userId, dto.UserId));
        _smartCollectionsManager.Verify(manager => manager.GetAllByUserAsync(userId.ToString()), Times.Once);
    }

    [Fact]
    public async Task GetSmartCollection_ExistingOwnedCollection_ReturnsSmartCollection()
    {
        var userId = Guid.NewGuid();
        var smartCollection = new SmartCollectionEntity("Collection", userId, new SmartCollectionFilters());

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollection.Id, userId.ToString()))
            .ReturnsAsync(smartCollection);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollection(smartCollection.Id);

        var dto = Assert.IsType<SmartCollectionDto>(response.Value);
        Assert.Equal(smartCollection.Id, dto.Id);
        Assert.Equal(userId, dto.UserId);
    }

    [Fact]
    public async Task GetSmartCollection_MissingCollection_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var smartCollectionId = Guid.NewGuid();

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollectionId, userId.ToString()))
            .ReturnsAsync((SmartCollectionEntity?)null);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollection(smartCollectionId);

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetSmartCollection_DifferentOwner_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var smartCollection = new SmartCollectionEntity("Collection", Guid.NewGuid(), new SmartCollectionFilters());

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollection.Id, userId.ToString()))
            .ReturnsAsync(smartCollection);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollection(smartCollection.Id);

        Assert.IsType<NotFoundObjectResult>(response.Result);
    }

    [Fact]
    public async Task GetSmartCollectionItems_ExistingOwnedCollection_ReturnsItemIds()
    {
        var userId = Guid.NewGuid();
        var itemIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var smartCollection = new SmartCollectionEntity("Collection", userId, new SmartCollectionFilters
        {
            MinCommunityRating = 7
        })
        {
            Limit = 25
        };

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollection.Id, userId.ToString()))
            .ReturnsAsync(smartCollection);
        _smartCollectionsManager
            .Setup(manager => manager.EvaluateAsync(
                It.IsAny<SmartCollectionFilters>(),
                userId.ToString(),
                smartCollection.Limit))
            .ReturnsAsync(itemIds);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollectionItems(smartCollection.Id);

        Assert.NotNull(response.Value);
        var result = response.Value;
        Assert.Equal(itemIds, result.Items);
        Assert.Equal(itemIds.Length, result.TotalRecordCount);
        _smartCollectionsManager.Verify(
            manager => manager.EvaluateAsync(
                It.Is<SmartCollectionFilters>(filters => filters.MinCommunityRating == 7),
                userId.ToString(),
                smartCollection.Limit),
            Times.Once);
    }

    [Fact]
    public async Task GetSmartCollectionItems_MissingCollection_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var smartCollectionId = Guid.NewGuid();

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollectionId, userId.ToString()))
            .ReturnsAsync((SmartCollectionEntity?)null);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollectionItems(smartCollectionId);

        Assert.IsType<NotFoundObjectResult>(response.Result);
        _smartCollectionsManager.Verify(
            manager => manager.EvaluateAsync(It.IsAny<SmartCollectionFilters>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSmartCollectionItems_DifferentOwner_ReturnsNotFound()
    {
        var userId = Guid.NewGuid();
        var smartCollection = new SmartCollectionEntity("Collection", Guid.NewGuid(), new SmartCollectionFilters());

        _smartCollectionsManager
            .Setup(manager => manager.GetByIdAsync(smartCollection.Id, userId.ToString()))
            .ReturnsAsync(smartCollection);

        var controller = CreateController(userId);

        var response = await controller.GetSmartCollectionItems(smartCollection.Id);

        Assert.IsType<NotFoundObjectResult>(response.Result);
        _smartCollectionsManager.Verify(
            manager => manager.EvaluateAsync(It.IsAny<SmartCollectionFilters>(), It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    private SmartCollectionsController CreateController(Guid userId)
    {
        var claims = new[]
        {
            new Claim(InternalClaimTypes.UserId, userId.ToString("N", CultureInfo.InvariantCulture))
        };

        return new SmartCollectionsController(_smartCollectionsManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims))
                }
            }
        };
    }
}
