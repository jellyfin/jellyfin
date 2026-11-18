using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Movies controller.
/// </summary>
[Authorize]
[Tags("Movie")]
public class MoviesController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;
    private readonly ISimilarItemsManager _similarItemsManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoviesController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="similarItemsManager">Instance of the <see cref="ISimilarItemsManager"/> interface.</param>
    public MoviesController(
        IUserManager userManager,
        IDtoService dtoService,
        ISimilarItemsManager similarItemsManager)
    {
        _userManager = userManager;
        _dtoService = dtoService;
        _similarItemsManager = similarItemsManager;
    }

    /// <summary>
    /// Gets movie recommendations.
    /// </summary>
    /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
    /// <param name="parentId">Specify this to localize the search to a specific item or folder. Omit to use the root.</param>
    /// <param name="fields">Optional. The fields to return.</param>
    /// <param name="categoryLimit">The max number of categories to return.</param>
    /// <param name="itemLimit">The max number of items to return per category.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <response code="200">Movie recommendations returned.</response>
    /// <returns>The list of movie recommendations.</returns>
    [HttpGet("Recommendations")]
    public async Task<ActionResult<IEnumerable<RecommendationDto>>> GetMovieRecommendations(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? parentId,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
        [FromQuery] int categoryLimit = 5,
        [FromQuery] int itemLimit = 8,
        CancellationToken cancellationToken = default)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = userId.IsNullOrEmpty()
            ? null
            : _userManager.GetUserById(userId.Value);
        var dtoOptions = new DtoOptions { Fields = fields };

        var recommendations = await _similarItemsManager
            .GetMovieRecommendationsAsync(user, parentId ?? Guid.Empty, categoryLimit, itemLimit, dtoOptions, cancellationToken)
            .ConfigureAwait(false);

        return Ok(recommendations.Select(r => new RecommendationDto
        {
            BaselineItemName = r.BaselineItemName,
            CategoryId = r.CategoryId,
            RecommendationType = r.RecommendationType,
            Items = _dtoService.GetBaseItemDtos(r.Items, dtoOptions, user)
        }));
    }
}
