using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.UserViewDtos;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// User views controller.
    /// </summary>
    [Route("")]
    public class UserViewsController : BaseJellyfinApiController
    {
        private readonly IUserManager _userManager;
        private readonly IUserViewManager _userViewManager;
        private readonly IDtoService _dtoService;
        private readonly IAuthorizationContext _authContext;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserViewsController"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public UserViewsController(
            IUserManager userManager,
            IUserViewManager userViewManager,
            IDtoService dtoService,
            IAuthorizationContext authContext,
            ILibraryManager libraryManager)
        {
            _userManager = userManager;
            _userViewManager = userViewManager;
            _dtoService = dtoService;
            _authContext = authContext;
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
        [HttpGet("Users/{userId}/Views")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<QueryResult<BaseItemDto>> GetUserViews(
            [FromRoute, Required] Guid userId,
            [FromQuery] bool? includeExternalContent,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] presetViews,
            [FromQuery] bool includeHidden = false)
        {
            var query = new UserViewQuery
            {
                UserId = userId,
                IncludeHidden = includeHidden
            };

            if (includeExternalContent.HasValue)
            {
                query.IncludeExternalContent = includeExternalContent.Value;
            }

            if (presetViews.Length != 0)
            {
                query.PresetViews = presetViews;
            }

            var app = _authContext.GetAuthorizationInfo(Request).Client ?? string.Empty;
            if (app.IndexOf("emby rt", StringComparison.OrdinalIgnoreCase) != -1)
            {
                query.PresetViews = new[] { CollectionType.Movies, CollectionType.TvShows };
            }

            var folders = _userViewManager.GetUserViews(query);

            var dtoOptions = new DtoOptions().AddClientFields(Request);
            var fields = dtoOptions.Fields.ToList();

            fields.Add(ItemFields.PrimaryImageAspectRatio);
            fields.Add(ItemFields.DisplayPreferencesId);
            fields.Remove(ItemFields.BasicSyncInfo);
            dtoOptions.Fields = fields.ToArray();

            var user = _userManager.GetUserById(userId);

            var dtos = folders.Select(i => _dtoService.GetBaseItemDto(i, dtoOptions, user))
                .ToArray();

            return new QueryResult<BaseItemDto>
            {
                Items = dtos,
                TotalRecordCount = dtos.Length
            };
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
        public ActionResult<IEnumerable<SpecialViewOptionDto>> GetGroupingOptions([FromRoute, Required] Guid userId)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
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
                .OrderBy(i => i.Name));
        }
    }
}
