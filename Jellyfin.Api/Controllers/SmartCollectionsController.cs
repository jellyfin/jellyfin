using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.SmartCollectionDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.SmartCollections;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartCollectionEntity = Jellyfin.Database.Implementations.Entities.SmartCollections;
using SmartCollectionFilters = Jellyfin.Database.Implementations.Entities.SmartCollectionFilters;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Smart collections controller.
/// </summary>
[Route("SmartCollections")]
[Authorize]
[Tags("SmartCollection")]
public class SmartCollectionsController : BaseJellyfinApiController
{
    private const int DefaultLimit = 50;
    private readonly ISmartCollectionsManager _smartCollectionsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartCollectionsController"/> class.
    /// </summary>
    /// <param name="smartCollectionsManager">Instance of the <see cref="ISmartCollectionsManager"/> interface.</param>
    public SmartCollectionsController(ISmartCollectionsManager smartCollectionsManager)
    {
        _smartCollectionsManager = smartCollectionsManager;
    }

    /// <summary>
    /// Creates a new smart collection.
    /// </summary>
    /// <param name="createSmartCollectionRequest">The create smart collection payload.</param>
    /// <response code="200">Smart collection created.</response>
    /// <response code="400">Invalid smart collection payload.</response>
    /// <returns>The created smart collection.</returns>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmartCollectionDto>> CreateSmartCollection(
        [FromBody, Required] CreateSmartCollectionDto createSmartCollectionRequest)
    {
        if (createSmartCollectionRequest is null)
        {
            return BadRequest("Smart collection payload is required.");
        }

        if (string.IsNullOrWhiteSpace(createSmartCollectionRequest.Name))
        {
            return BadRequest("Smart collection name is required.");
        }

        if (createSmartCollectionRequest.Filters.ValueKind is not JsonValueKind.Object)
        {
            return BadRequest("Smart collection filters must be a JSON object.");
        }

        if (createSmartCollectionRequest.Limit <= 0)
        {
            return BadRequest("Smart collection limit must be greater than zero.");
        }

        var filters = createSmartCollectionRequest.Filters.Deserialize<SmartCollectionFilters>();
        if (filters is null)
        {
            return BadRequest("Smart collection filters are invalid.");
        }

        var userId = User.GetUserId();
        var smartCollection = new SmartCollectionEntity(createSmartCollectionRequest.Name, userId, filters)
        {
            Limit = createSmartCollectionRequest.Limit ?? DefaultLimit,
            SortBy = ToSortByString(createSmartCollectionRequest.SortBy),
            SortOrder = ToSortOrder(createSmartCollectionRequest.SortOrder)
        };

        var created = await _smartCollectionsManager.CreateAsync(
            smartCollection,
            userId.ToString()).ConfigureAwait(false);

        return ToDto(created);
    }

    /// <summary>
    /// Gets all smart collections for the current user.
    /// </summary>
    /// <response code="200">Smart collections returned.</response>
    /// <returns>The current user's smart collections.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SmartCollectionDto>>> GetSmartCollections()
    {
        var userId = User.GetUserId();
        var smartCollections = await _smartCollectionsManager.GetAllByUserAsync(
            userId.ToString()).ConfigureAwait(false);

        return smartCollections.Select(ToDto).ToList();
    }

    /// <summary>
    /// Gets a smart collection.
    /// </summary>
    /// <param name="smartCollectionId">The smart collection id.</param>
    /// <response code="200">Smart collection returned.</response>
    /// <response code="404">Smart collection not found.</response>
    /// <returns>The requested smart collection.</returns>
    [HttpGet("{smartCollectionId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmartCollectionDto>> GetSmartCollection(
        [FromRoute, Required] Guid smartCollectionId)
    {
        var userId = User.GetUserId();
        var smartCollection = await _smartCollectionsManager.GetByIdAsync(
            smartCollectionId,
            userId.ToString()).ConfigureAwait(false);

        if (smartCollection is null || !smartCollection.UserId.Equals(userId))
        {
            return NotFound("Smart collection not found.");
        }

        return ToDto(smartCollection);
    }

    /// <summary>
    /// Gets the item ids for a smart collection.
    /// </summary>
    /// <param name="smartCollectionId">The smart collection id.</param>
    /// <response code="200">Smart collection items returned.</response>
    /// <response code="404">Smart collection not found.</response>
    /// <returns>The item ids matching the smart collection filters.</returns>
    [HttpGet("{smartCollectionId}/Items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueryResult<Guid>>> GetSmartCollectionItems(
        [FromRoute, Required] Guid smartCollectionId)
    {
        var userId = User.GetUserId();
        var smartCollection = await _smartCollectionsManager.GetByIdAsync(
            smartCollectionId,
            userId.ToString()).ConfigureAwait(false);

        if (smartCollection is null || !smartCollection.UserId.Equals(userId))
        {
            return NotFound("Smart collection not found.");
        }

        var itemIds = await _smartCollectionsManager.EvaluateAsync(
            smartCollection.GetFilters(),
            userId.ToString(),
            smartCollection.Limit).ConfigureAwait(false);

        return new QueryResult<Guid>(itemIds.ToArray());
    }

    private static SmartCollectionDto ToDto(SmartCollectionEntity smartCollection)
    {
        return new SmartCollectionDto
        {
            Id = smartCollection.Id,
            Name = smartCollection.Name,
            UserId = smartCollection.UserId,
            Filters = JsonSerializer.SerializeToElement(smartCollection.GetFilters()),
            SortBy = ToSortByList(smartCollection.SortBy),
            SortOrder = smartCollection.SortOrder.HasValue ? [smartCollection.SortOrder.Value] : null,
            Limit = smartCollection.Limit
        };
    }

    private static string? ToSortByString(IReadOnlyList<ItemSortBy>? sortBy)
    {
        return sortBy is null || sortBy.Count == 0
            ? null
            : string.Join(',', sortBy);
    }

    private static IReadOnlyList<ItemSortBy>? ToSortByList(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return null;
        }

        var values = new List<ItemSortBy>();
        foreach (var value in sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<ItemSortBy>(value, true, out var sortField))
            {
                values.Add(sortField);
            }
        }

        return values.Count == 0 ? null : values;
    }

    private static SortOrder? ToSortOrder(IReadOnlyList<SortOrder>? sortOrder)
    {
        return sortOrder is null || sortOrder.Count == 0
            ? null
            : sortOrder[0];
    }
}
