using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Persons controller.
/// </summary>
[Authorize]
public class PersonsController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PersonsController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public PersonsController(
        ILibraryManager libraryManager,
        IDtoService dtoService,
        IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _dtoService = dtoService;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets all persons.
    /// </summary>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="filters">Optional. Specify additional filters to apply.</param>
    /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not. userId is required.</param>
    /// <param name="enableUserData">Optional, include user data.</param>
    /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="excludePersonTypes">Optional. If specified results will be filtered to exclude those containing the specified PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="personTypes">Optional. If specified results will be filtered to include only those containing the specified PersonType. Allows multiple, comma-delimited.</param>
    /// <param name="appearsInItemId">Optional. If specified, person results will be filtered on items related to said persons.</param>
    /// <param name="userId">User id.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <param name="parentId">Optional. Specify an item Id to search within a specific library (folder/collection).</param>
    /// <param name="nameStartsWith">Optional. Filter persons whose name starts with this string.</param>
    /// <param name="nameLessThan">Optional. Filter persons whose name is alphabetically less than this string.</param>
    /// <param name="sortBy">Optional. Sort persons by this field (SortName, Random).</param>
    /// <param name="sortOrder">Optional. Sort order (Ascending, Descending).</param>
    /// <param name="startIndex">Optional. The record index to start at.</param>
    /// <response code="200">Persons returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the queryresult of persons.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetPersons(
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ItemFilter[] filters,
        [FromQuery] bool? isFavorite,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] ImageType[] enableImageTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] excludePersonTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] personTypes,
        [FromQuery] Guid? appearsInItemId,
        [FromQuery] Guid? userId,
        [FromQuery] bool? enableImages = true,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string? nameStartsWith = null,
        [FromQuery] string? nameLessThan = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] int? startIndex = null)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        User? user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var isFavoriteInFilters = filters.Any(f => f == ItemFilter.IsFavorite);
        var query = new InternalPeopleQuery(personTypes, excludePersonTypes)
        {
            NameContains = searchTerm,
            User = user,
            IsFavorite = !isFavorite.HasValue && isFavoriteInFilters ? true : isFavorite,
            AppearsInItemId = appearsInItemId ?? Guid.Empty,
            AncestorId = parentId ?? Guid.Empty,
            Limit = limit ?? 0,
            NameStartsWith = nameStartsWith,
            NameLessThan = nameLessThan,
            SortBy = sortBy,
            SortOrder = sortOrder,
            StartIndex = startIndex ?? 0
        };

        var totalCount = _libraryManager.GetPeopleCount(query);
        var peopleItems = _libraryManager.GetPeopleItems(query);
        var dtos = peopleItems
            .Select(person => _dtoService.GetItemByNameDto(person, dtoOptions, null, user))
            .ToArray();

        return new QueryResult<BaseItemDto>(startIndex, totalCount, dtos);
    }

    /// <summary>
    /// Gets crew members (non-actors), one entry per person+role combination.
    /// </summary>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="parentId">Optional. Specify a library Id to scope results to that library.</param>
    /// <param name="nameStartsWith">Optional. Filter crew whose name starts with this string.</param>
    /// <param name="nameLessThan">Optional. Filter crew whose name is alphabetically less than this string.</param>
    /// <param name="sortBy">Optional. Sort by field (SortName, Random).</param>
    /// <param name="sortOrder">Optional. Sort order (Ascending, Descending).</param>
    /// <param name="startIndex">Optional. The record index to start at.</param>
    /// <response code="200">Crew returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the queryresult of crew members.</returns>
    [HttpGet("Crew")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<CrewMemberDto>> GetCrew(
        [FromQuery] int? limit,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string? nameStartsWith = null,
        [FromQuery] string? nameLessThan = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] int? startIndex = null)
    {
        var query = new InternalPeopleQuery([], [])
        {
            AncestorId = parentId ?? Guid.Empty,
            Limit = limit ?? 0,
            NameStartsWith = nameStartsWith,
            NameLessThan = nameLessThan,
            SortBy = sortBy,
            SortOrder = sortOrder,
            StartIndex = startIndex ?? 0
        };

        var (items, totalCount) = _libraryManager.GetCrewMembers(query);
        return new QueryResult<CrewMemberDto>(startIndex, totalCount, items);
    }

    /// <summary>
    /// Get person by name.
    /// </summary>
    /// <param name="name">Person name.</param>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <response code="200">Person returned.</response>
    /// <response code="404">Person not found.</response>
    /// <returns>An <see cref="OkResult"/> containing the person on success,
    /// or a <see cref="NotFoundResult"/> if person not found.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BaseItemDto> GetPerson([FromRoute, Required] string name, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions()
            .AddClientFields(User);

        var item = _libraryManager.GetPerson(name);
        if (item is null)
        {
            return NotFound();
        }

        if (!userId.IsNullOrEmpty())
        {
            var user = _userManager.GetUserById(userId.Value);
            return _dtoService.GetBaseItemDto(item, dtoOptions, user);
        }

        return _dtoService.GetBaseItemDto(item, dtoOptions);
    }
}
