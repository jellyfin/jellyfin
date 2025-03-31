using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.UserViewDtos;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// User views controller.
/// </summary>
[Route("")]
[Authorize]
public class UserViewsController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly IDtoService _dtoService;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserViewsController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    public UserViewsController(
        IUserManager userManager,
        IUserViewManager userViewManager,
        IDtoService dtoService,
        ILibraryManager libraryManager)
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _dtoService = dtoService;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Get user views.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="includeExternalContent">Whether or not to include external views such as channels or live tv.</param>
    /// <param name="presetViews">Preset views.</param>
    /// <param name="includeHidden">Whether or not to include hidden content.</param>
    /// <response code="200">User views returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the user views.</returns>
    [HttpGet("UserViews")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public QueryResult<BaseItemDto> GetUserViews(
        [FromQuery] Guid? userId,
        [FromQuery] bool? includeExternalContent,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] CollectionType?[] presetViews,
        [FromQuery] bool includeHidden = false)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        var query = new UserViewQuery { User = user, IncludeHidden = includeHidden };

        if (includeExternalContent.HasValue)
        {
            query.IncludeExternalContent = includeExternalContent.Value;
        }

        if (presetViews.Length != 0)
        {
            query.PresetViews = presetViews;
        }

        var folders = _userViewManager.GetUserViews(query);

        var dtoOptions = new DtoOptions().AddClientFields(User);
        dtoOptions.Fields = [..dtoOptions.Fields, ItemFields.PrimaryImageAspectRatio, ItemFields.DisplayPreferencesId];

        var dtos = Array.ConvertAll(folders, i => _dtoService.GetBaseItemDto(i, dtoOptions, user));

        return new QueryResult<BaseItemDto>(dtos);
    }

    /// <summary>
    /// Get user views.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="includeExternalContent">Whether or not to include external views such as channels or live tv.</param>
    /// <param name="presetViews">Preset views.</param>
    /// <param name="includeHidden">Whether or not to include hidden content.</param>
    /// <response code="200">User views returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the user views.</returns>
    [HttpGet("Users/{userId}/Views")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public QueryResult<BaseItemDto> GetUserViewsLegacy(
        [FromRoute, Required] Guid userId,
        [FromQuery] bool? includeExternalContent,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] CollectionType?[] presetViews,
        [FromQuery] bool includeHidden = false)
        => GetUserViews(userId, includeExternalContent, presetViews, includeHidden);

    /// <summary>
    /// Get user view grouping options.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <response code="200">User view grouping options returned.</response>
    /// <response code="404">User not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the user view grouping options
    /// or a <see cref="NotFoundResult"/> if user not found.
    /// </returns>
    [HttpGet("UserViews/GroupingOptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<SpecialViewOptionDto>> GetGroupingOptions([FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(_libraryManager.GetUserRootFolder()
            .GetChildren(user, true)
            .OfType<Folder>()
            .Where(UserView.IsEligibleForGrouping)
            .Select(i => new SpecialViewOptionDto
            {
                Name = i.Name,
                Id = i.Id.ToString("N", CultureInfo.InvariantCulture)
            })
            .OrderBy(i => i.Name)
            .AsEnumerable());
    }

    /// <summary>
    /// Get user view grouping options.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <response code="200">User view grouping options returned.</response>
    /// <response code="404">User not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the user view grouping options
    /// or a <see cref="NotFoundResult"/> if user not found.
    /// </returns>
    [HttpGet("Users/{userId}/GroupingOptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<IEnumerable<SpecialViewOptionDto>> GetGroupingOptionsLegacy(
        [FromRoute, Required] Guid userId)
        => GetGroupingOptions(userId);
}
