using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The music genres controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class MusicGenresController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicGenresController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of <see cref="IUserManager"/> interface.</param>
        /// <param name="dtoService">Instance of <see cref="IDtoService"/> interface.</param>
        public MusicGenresController(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDtoService dtoService)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Gets a music genre, by name.
        /// </summary>
        /// <param name="genreName">The genre name.</param>
        /// <param name="userId">Optional. Filter by user id, and attach user data.</param>
        /// <returns>An <see cref="OkResult"/> containing a <see cref="BaseItemDto"/> with the music genre.</returns>
        [HttpGet("{genreName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<BaseItemDto> GetMusicGenre([FromRoute, Required] string genreName, [FromQuery] Guid? userId)
        {
            var dtoOptions = new DtoOptions().AddClientFields(Request);

            MusicGenre item;

            if (genreName.IndexOf(BaseItem.SlugChar, StringComparison.OrdinalIgnoreCase) != -1)
            {
                item = GetItemFromSlugName<MusicGenre>(_libraryManager, genreName, dtoOptions);
            }
            else
            {
                item = _libraryManager.GetMusicGenre(genreName);
            }

            if (userId.HasValue && !userId.Equals(Guid.Empty))
            {
                var user = _userManager.GetUserById(userId.Value);

                return _dtoService.GetBaseItemDto(item, dtoOptions, user);
            }

            return _dtoService.GetBaseItemDto(item, dtoOptions);
        }

        private T GetItemFromSlugName<T>(ILibraryManager libraryManager, string name, DtoOptions dtoOptions)
            where T : BaseItem, new()
        {
            var result = libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '&'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '/'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            result ??= libraryManager.GetItemList(new InternalItemsQuery
            {
                Name = name.Replace(BaseItem.SlugChar, '?'),
                IncludeItemTypes = new[] { typeof(T).Name },
                DtoOptions = dtoOptions
            }).OfType<T>().FirstOrDefault();

            return result;
        }
    }
}
