using System;
using System.ComponentModel.DataAnnotations;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
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
/// Studios controller.
/// </summary>
[Authorize]
public class StudiosController : BaseJellyfinApiController
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="StudiosController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    public StudiosController(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IDtoService dtoService)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Gets all studios from a given item, folder, or the entire library.
    /// </summary>
    /// <param name="startIndex">Optional. The record index to start at. All items with a lower index will be dropped from the results.</param>
    /// <param name="limit">Optional. The maximum number of records to return.</param>
    /// <param name="searchTerm">Optional. Search term.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. Specify additional fields of information to return in the output.</param>
    /// <param name="excludeItemTypes">Optional. If specified, results will be filtered out based on item type. This allows multiple, comma delimited.</param>
    /// <param name="includeItemTypes">Optional. If specified, results will be filtered based on item type. This allows multiple, comma delimited.</param>
    /// <param name="isFavorite">Optional filter by items that are marked as favorite, or not.</param>
    /// <param name="enableUserData">Optional, include user data.</param>
    /// <param name="imageTypeLimit">Optional, the max number of images to return, per image type.</param>
    /// <param name="enableImageTypes">Optional. The image types to include in the output.</param>
    /// <param name="userId">User id.</param>
    /// <param name="nameStartsWithOrGreater">Optional filter by items whose name is sorted equally or greater than a given input string.</param>
    /// <param name="nameStartsWith">Optional filter by items whose name is sorted equally than a given input string.</param>
    /// <param name="nameLessThan">Optional filter by items whose name is equally or lesser than a given input string.</param>
    /// <param name="enableImages">Optional, include image information in output.</param>
    /// <param name="enableTotalRecordCount">Total record count.</param>
    /// <response code="200">Studios returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the studios.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<QueryResult<BaseItemDto>> GetStudios(
        [FromQuery] int? startIndex,
        [FromQuery] int? limit,
        [FromQuery] string? searchTerm,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] excludeItemTypes,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] BaseItemKind[] includeItemTypes,
        [FromQuery] bool? isFavorite,
        [FromQuery] bool? enableUserData,
        [FromQuery] int? imageTypeLimit,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ImageType[] enableImageTypes,
        [FromQuery] Guid? userId,
        [FromQuery] string? nameStartsWithOrGreater,
        [FromQuery] string? nameStartsWith,
        [FromQuery] string? nameLessThan,
        [FromQuery] bool? enableImages = true,
        [FromQuery] bool enableTotalRecordCount = true)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions { Fields = fields }
            .AddClientFields(User)
            .AddAdditionalDtoOptions(enableImages, enableUserData, imageTypeLimit, enableImageTypes);

        User? user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);

        var parentItem = _libraryManager.GetParentItem(parentId, userId);

        var query = new InternalItemsQuery(user)
        {
            ExcludeItemTypes = excludeItemTypes,
            IncludeItemTypes = includeItemTypes,
            StartIndex = startIndex,
            Limit = limit,
            IsFavorite = isFavorite,
            NameLessThan = nameLessThan,
            NameStartsWith = nameStartsWith,
            NameStartsWithOrGreater = nameStartsWithOrGreater,
            DtoOptions = dtoOptions,
            SearchTerm = searchTerm,
            EnableTotalRecordCount = enableTotalRecordCount
        };

        if (parentId.HasValue)
        {
            if (parentItem is Folder)
            {
                query.AncestorIds = new[] { parentId.Value };
            }
            else
            {
                query.ItemIds = new[] { parentId.Value };
            }
        }

        var result = _libraryManager.GetStudios(query);
        var shouldIncludeItemTypes = includeItemTypes.Length != 0;
        return RequestHelpers.CreateQueryResult(result, dtoOptions, _dtoService, shouldIncludeItemTypes, user);
    }

    /// <summary>
    /// Gets a studio by name.
    /// </summary>
    /// <param name="name">Studio name.</param>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <response code="200">Studio returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the studio.</returns>
    [HttpGet("{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BaseItemDto> GetStudio([FromRoute, Required] string name, [FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var dtoOptions = new DtoOptions().AddClientFields(User);

        var item = _libraryManager.GetStudio(name);
        if (!userId.IsNullOrEmpty())
        {
            var user = _userManager.GetUserById(userId.Value);

            return _dtoService.GetBaseItemDto(item, dtoOptions, user);
        }

        return _dtoService.GetBaseItemDto(item, dtoOptions);
    }
}
