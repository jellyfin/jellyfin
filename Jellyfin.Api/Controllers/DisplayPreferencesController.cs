using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Display Preferences Controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class DisplayPreferencesController : BaseJellyfinApiController
    {
        private readonly IDisplayPreferencesManager _displayPreferencesManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayPreferencesController"/> class.
        /// </summary>
        /// <param name="displayPreferencesManager">Instance of <see cref="IDisplayPreferencesManager"/> interface.</param>
        public DisplayPreferencesController(IDisplayPreferencesManager displayPreferencesManager)
        {
            _displayPreferencesManager = displayPreferencesManager;
        }

        /// <summary>
        /// Get Display Preferences.
        /// </summary>
        /// <param name="displayPreferencesId">Display preferences id.</param>
        /// <param name="userId">User id.</param>
        /// <param name="client">Client.</param>
        /// <response code="200">Display preferences retrieved.</response>
        /// <returns>An <see cref="OkResult"/> containing the display preferences on success, or a <see cref="NotFoundResult"/> if the display preferences could not be found.</returns>
        [HttpGet("{displayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "displayPreferencesId", Justification = "Imported from ServiceStack")]
        public ActionResult<DisplayPreferencesDto> GetDisplayPreferences(
            [FromRoute, Required] string displayPreferencesId,
            [FromQuery, Required] Guid userId,
            [FromQuery, Required] string client)
        {
            var displayPreferences = _displayPreferencesManager.GetDisplayPreferences(userId, client);
            var itemPreferences = _displayPreferencesManager.GetItemDisplayPreferences(displayPreferences.UserId, Guid.Empty, displayPreferences.Client);

            var dto = new DisplayPreferencesDto
            {
                Client = displayPreferences.Client,
                Id = displayPreferences.UserId.ToString(),
                ViewType = itemPreferences.ViewType.ToString(),
                SortBy = itemPreferences.SortBy,
                SortOrder = itemPreferences.SortOrder,
                IndexBy = displayPreferences.IndexBy?.ToString(),
                RememberIndexing = itemPreferences.RememberIndexing,
                RememberSorting = itemPreferences.RememberSorting,
                ScrollDirection = displayPreferences.ScrollDirection,
                ShowBackdrop = displayPreferences.ShowBackdrop,
                ShowSidebar = displayPreferences.ShowSidebar
            };

            foreach (var homeSection in displayPreferences.HomeSections)
            {
                dto.CustomPrefs["homesection" + homeSection.Order] = homeSection.Type.ToString().ToLowerInvariant();
            }

            foreach (var itemDisplayPreferences in _displayPreferencesManager.ListItemDisplayPreferences(displayPreferences.UserId, displayPreferences.Client))
            {
                dto.CustomPrefs["landing-" + itemDisplayPreferences.ItemId] = itemDisplayPreferences.ViewType.ToString().ToLowerInvariant();
            }

            dto.CustomPrefs["chromecastVersion"] = displayPreferences.ChromecastVersion.ToString().ToLowerInvariant();
            dto.CustomPrefs["skipForwardLength"] = displayPreferences.SkipForwardLength.ToString(CultureInfo.InvariantCulture);
            dto.CustomPrefs["skipBackLength"] = displayPreferences.SkipBackwardLength.ToString(CultureInfo.InvariantCulture);
            dto.CustomPrefs["enableNextVideoInfoOverlay"] = displayPreferences.EnableNextVideoInfoOverlay.ToString(CultureInfo.InvariantCulture);
            dto.CustomPrefs["tvhome"] = displayPreferences.TvHome;

            return dto;
        }

        /// <summary>
        /// Update Display Preferences.
        /// </summary>
        /// <param name="displayPreferencesId">Display preferences id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="client">Client.</param>
        /// <param name="displayPreferences">New Display Preferences object.</param>
        /// <response code="204">Display preferences updated.</response>
        /// <returns>An <see cref="NoContentResult"/> on success.</returns>
        [HttpPost("{displayPreferencesId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "displayPreferencesId", Justification = "Imported from ServiceStack")]
        public ActionResult UpdateDisplayPreferences(
            [FromRoute, Required] string displayPreferencesId,
            [FromQuery, Required] Guid userId,
            [FromQuery, Required] string client,
            [FromBody, Required] DisplayPreferencesDto displayPreferences)
        {
            HomeSectionType[] defaults =
            {
                HomeSectionType.SmallLibraryTiles,
                HomeSectionType.Resume,
                HomeSectionType.ResumeAudio,
                HomeSectionType.LiveTv,
                HomeSectionType.NextUp,
                HomeSectionType.LatestMedia, HomeSectionType.None,
            };

            var existingDisplayPreferences = _displayPreferencesManager.GetDisplayPreferences(userId, client);
            existingDisplayPreferences.IndexBy = Enum.TryParse<IndexingKind>(displayPreferences.IndexBy, true, out var indexBy) ? indexBy : (IndexingKind?)null;
            existingDisplayPreferences.ShowBackdrop = displayPreferences.ShowBackdrop;
            existingDisplayPreferences.ShowSidebar = displayPreferences.ShowSidebar;

            existingDisplayPreferences.ScrollDirection = displayPreferences.ScrollDirection;
            existingDisplayPreferences.ChromecastVersion = displayPreferences.CustomPrefs.TryGetValue("chromecastVersion", out var chromecastVersion)
                ? Enum.Parse<ChromecastVersion>(chromecastVersion, true)
                : ChromecastVersion.Stable;
            existingDisplayPreferences.EnableNextVideoInfoOverlay = displayPreferences.CustomPrefs.TryGetValue("enableNextVideoInfoOverlay", out var enableNextVideoInfoOverlay)
                ? bool.Parse(enableNextVideoInfoOverlay)
                : true;
            existingDisplayPreferences.SkipBackwardLength = displayPreferences.CustomPrefs.TryGetValue("skipBackLength", out var skipBackLength)
                ? int.Parse(skipBackLength, CultureInfo.InvariantCulture)
                : 10000;
            existingDisplayPreferences.SkipForwardLength = displayPreferences.CustomPrefs.TryGetValue("skipForwardLength", out var skipForwardLength)
                ? int.Parse(skipForwardLength, CultureInfo.InvariantCulture)
                : 30000;
            existingDisplayPreferences.DashboardTheme = displayPreferences.CustomPrefs.TryGetValue("dashboardTheme", out var theme)
                ? theme
                : string.Empty;
            existingDisplayPreferences.TvHome = displayPreferences.CustomPrefs.TryGetValue("tvhome", out var home)
                ? home
                : string.Empty;
            existingDisplayPreferences.HomeSections.Clear();

            foreach (var key in displayPreferences.CustomPrefs.Keys.Where(key => key.StartsWith("homesection", StringComparison.OrdinalIgnoreCase)))
            {
                var order = int.Parse(key.AsSpan().Slice("homesection".Length));
                if (!Enum.TryParse<HomeSectionType>(displayPreferences.CustomPrefs[key], true, out var type))
                {
                    type = order < 7 ? defaults[order] : HomeSectionType.None;
                }

                existingDisplayPreferences.HomeSections.Add(new HomeSection { Order = order, Type = type });
            }

            foreach (var key in displayPreferences.CustomPrefs.Keys.Where(key => key.StartsWith("landing-", StringComparison.OrdinalIgnoreCase)))
            {
                var itemPreferences = _displayPreferencesManager.GetItemDisplayPreferences(existingDisplayPreferences.UserId, Guid.Parse(key.Substring("landing-".Length)), existingDisplayPreferences.Client);
                itemPreferences.ViewType = Enum.Parse<ViewType>(displayPreferences.ViewType);
            }

            var itemPrefs = _displayPreferencesManager.GetItemDisplayPreferences(existingDisplayPreferences.UserId, Guid.Empty, existingDisplayPreferences.Client);
            itemPrefs.SortBy = displayPreferences.SortBy;
            itemPrefs.SortOrder = displayPreferences.SortOrder;
            itemPrefs.RememberIndexing = displayPreferences.RememberIndexing;
            itemPrefs.RememberSorting = displayPreferences.RememberSorting;

            if (Enum.TryParse<ViewType>(displayPreferences.ViewType, true, out var viewType))
            {
                itemPrefs.ViewType = viewType;
            }

            _displayPreferencesManager.SaveChanges();

            return NoContent();
        }
    }
}
