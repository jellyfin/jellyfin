using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Api.Models.HomeSectionDto;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Home Section controller.
    /// </summary>
    [Route("Users/{userId}/HomeSections")]
    [Authorize]
    public class HomeSectionController : BaseJellyfinApiController
    {
        private readonly IHomeSectionManager _homeSectionManager;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDtoService _dtoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeSectionController"/> class.
        /// </summary>
        /// <param name="homeSectionManager">Instance of the <see cref="IHomeSectionManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
        public HomeSectionController(
            IHomeSectionManager homeSectionManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IDtoService dtoService)
        {
            _homeSectionManager = homeSectionManager;
            _userManager = userManager;
            _libraryManager = libraryManager;
            _dtoService = dtoService;
        }

        /// <summary>
        /// Get all home sections.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <response code="200">Home sections retrieved.</response>
        /// <returns>An <see cref="IEnumerable{EnrichedHomeSectionDto}"/> containing the home sections.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<List<EnrichedHomeSectionDto>> GetHomeSections([FromRoute, Required] Guid userId)
        {
            var sections = _homeSectionManager.GetHomeSections(userId);
            var result = new List<EnrichedHomeSectionDto>();
            var user = _userManager.GetUserById(userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            foreach (var section in sections)
            {
                var enrichedSection = new EnrichedHomeSectionDto
                {
                    Id = null, // We'll need to retrieve the ID from the database
                    SectionOptions = section,
                    Items = GetItemsForSection(userId, section)
                };

                result.Add(enrichedSection);
            }

            return Ok(result);
        }

        /// <summary>
        /// Get home section.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="sectionId">Section id.</param>
        /// <response code="200">Home section retrieved.</response>
        /// <response code="404">Home section not found.</response>
        /// <returns>An <see cref="EnrichedHomeSectionDto"/> containing the home section.</returns>
        [HttpGet("{sectionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<EnrichedHomeSectionDto> GetHomeSection([FromRoute, Required] Guid userId, [FromRoute, Required] Guid sectionId)
        {
            var section = _homeSectionManager.GetHomeSection(userId, sectionId);
            if (section == null)
            {
                return NotFound();
            }

            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var result = new EnrichedHomeSectionDto
            {
                Id = sectionId,
                SectionOptions = section,
                Items = GetItemsForSection(userId, section)
            };

            return Ok(result);
        }

        /// <summary>
        /// Create a new home section.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="dto">The home section dto.</param>
        /// <response code="201">Home section created.</response>
        /// <returns>An <see cref="HomeSectionDto"/> containing the new home section.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public ActionResult<HomeSectionDto> CreateHomeSection([FromRoute, Required] Guid userId, [FromBody, Required] HomeSectionDto dto)
        {
            var sectionId = _homeSectionManager.CreateHomeSection(userId, dto.SectionOptions);
            _homeSectionManager.SaveChanges();

            dto.Id = sectionId;
            return CreatedAtAction(nameof(GetHomeSection), new { userId, sectionId }, dto);
        }

        /// <summary>
        /// Update a home section.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="sectionId">Section id.</param>
        /// <param name="dto">The home section dto.</param>
        /// <response code="204">Home section updated.</response>
        /// <response code="404">Home section not found.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpPut("{sectionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UpdateHomeSection(
            [FromRoute, Required] Guid userId,
            [FromRoute, Required] Guid sectionId,
            [FromBody, Required] HomeSectionDto dto)
        {
            var success = _homeSectionManager.UpdateHomeSection(userId, sectionId, dto.SectionOptions);
            if (!success)
            {
                return NotFound();
            }

            _homeSectionManager.SaveChanges();
            return NoContent();
        }

        /// <summary>
        /// Delete a home section.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="sectionId">Section id.</param>
        /// <response code="204">Home section deleted.</response>
        /// <response code="404">Home section not found.</response>
        /// <returns>A <see cref="NoContentResult"/>.</returns>
        [HttpDelete("{sectionId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteHomeSection([FromRoute, Required] Guid userId, [FromRoute, Required] Guid sectionId)
        {
            var success = _homeSectionManager.DeleteHomeSection(userId, sectionId);
            if (!success)
            {
                return NotFound();
            }

            _homeSectionManager.SaveChanges();
            return NoContent();
        }

        private IEnumerable<BaseItemDto> GetItemsForSection(Guid userId, HomeSectionOptions options)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return Array.Empty<BaseItemDto>();
            }

            switch (options.SectionType)
            {
                case HomeSectionType.None:
                    return Array.Empty<BaseItemDto>();
                case HomeSectionType.SmallLibraryTiles:
                    return GetLibraryTilesHomeSectionItems(userId, true);
                case HomeSectionType.LibraryButtons:
                    return GetLibraryTilesHomeSectionItems(userId, false);
                // TODO: Implement GetActiveRecordingsHomeSectionItems
                case HomeSectionType.ActiveRecordings:
                    return Array.Empty<BaseItemDto>();
                // TODO: Implement GetResumeItemsHomeSectionItems
                case HomeSectionType.Resume:
                    return Array.Empty<BaseItemDto>();
                // TODO: Implement GetResumeAudioHomeSectionItems
                case HomeSectionType.ResumeAudio:
                    return Array.Empty<BaseItemDto>();
                case HomeSectionType.LatestMedia:
                    return GetLatestMediaHomeSectionItems(userId, options.MaxItems);
                // TODO: Implement GetNextUpHomeSectionItems
                case HomeSectionType.NextUp:
                    return Array.Empty<BaseItemDto>();
                // TODO: Implement GetLiveTvHomeSectionItems
                case HomeSectionType.LiveTv:
                    return Array.Empty<BaseItemDto>();
                // TODO: Implement ResumeBookHomeSectionItems
                case HomeSectionType.ResumeBook:
                    return Array.Empty<BaseItemDto>();
                // Major TODO: Implement GetPinnedCollectionHomeSectionItems and add HomeSectionType.PinnedCollection
                // See example at https://github.com/johnpc/jellyfin-plugin-home-sections/blob/main/Jellyfin.Plugin.HomeSections/Api/HomeSectionsController.cs
                // Question: what should I do in the case of an unexpected HomeSectionType? Throw an exception?
                default:
                    return Array.Empty<BaseItemDto>();
            }
        }

        private IEnumerable<BaseItemDto> GetLatestMediaHomeSectionItems(Guid userId, int maxItems)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return Array.Empty<BaseItemDto>();
            }

            var query = new InternalItemsQuery(user)
            {
                Recursive = true,
                Limit = maxItems,
                IsVirtualItem = false,
                OrderBy = new[] { (ItemSortBy.DateCreated, SortOrder.Descending) }
            };

            var items = _libraryManager.GetItemsResult(query);

            return items.Items
                .Where(i => i != null && (i is Movie || i is Series || i is Episode))
                .Select(i =>
                {
                    try
                    {
                        return _dtoService.GetBaseItemDto(i, new DtoOptions(), user);
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't crash
                        System.Diagnostics.Debug.WriteLine($"Error converting item {i.Id} to DTO: {ex.Message}");
                        return null;
                    }
                })
                .Where(dto => dto != null)
                .Cast<BaseItemDto>();
        }

        private IEnumerable<BaseItemDto> GetLibraryTilesHomeSectionItems(Guid userId, bool smallTiles = false)
        {
            var user = _userManager.GetUserById(userId);
            if (user == null)
            {
                return Array.Empty<BaseItemDto>();
            }

            // Get the user's view items (libraries)
            var folders = _libraryManager.GetUserRootFolder()
                .GetChildren(user, true)
                .Where(i => i.IsFolder && !i.IsHidden)
                .OrderBy(i => i.SortName)
                .ToList();

            // Convert to DTOs with appropriate options
            var options = new DtoOptions
            {
                // For small tiles, we might want to limit the fields returned
                // to make the response smaller
                Fields = smallTiles
                    ? new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.DisplayPreferencesId }
                    : new[]
                    {
                        ItemFields.PrimaryImageAspectRatio,
                        ItemFields.DisplayPreferencesId,
                        ItemFields.Overview,
                        ItemFields.ChildCount
                    }
            };

            return folders
                .Select(i =>
                {
                    try
                    {
                        return _dtoService.GetBaseItemDto(i, options, user);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                })
                .Where(dto => dto != null)
                .Cast<BaseItemDto>();
        }
    }
}
