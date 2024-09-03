using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Model.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Display Preferences Controller.
/// </summary>
[Authorize]
public class DisplayPreferencesController : BaseJellyfinApiController
{
    private readonly IDisplayPreferencesManager _displayPreferencesManager;
    private readonly ILogger<DisplayPreferencesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayPreferencesController"/> class.
    /// </summary>
    /// <param name="displayPreferencesManager">Instance of <see cref="IDisplayPreferencesManager"/> interface.</param>
    /// <param name="logger">Instance of <see cref="ILogger{DisplayPreferencesController}"/> interface.</param>
    public DisplayPreferencesController(IDisplayPreferencesManager displayPreferencesManager, ILogger<DisplayPreferencesController> logger)
    {
        _displayPreferencesManager = displayPreferencesManager;
        _logger = logger;
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
        [FromQuery] Guid? userId,
        [FromQuery, Required] string client)
    {
        userId = RequestHelpers.GetUserId(User, userId);

        if (!Guid.TryParse(displayPreferencesId, out var itemId))
        {
            itemId = displayPreferencesId.GetMD5();
        }

        var displayPreferences = _displayPreferencesManager.GetDisplayPreferences(userId.Value, itemId, client);
        var itemPreferences = _displayPreferencesManager.GetItemDisplayPreferences(displayPreferences.UserId, itemId, displayPreferences.Client);
        itemPreferences.ItemId = itemId;

        var dto = new DisplayPreferencesDto
        {
            Client = displayPreferences.Client,
            Id = displayPreferences.ItemId.ToString(),
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

        dto.CustomPrefs["chromecastVersion"] = displayPreferences.ChromecastVersion.ToString().ToLowerInvariant();
        dto.CustomPrefs["skipForwardLength"] = displayPreferences.SkipForwardLength.ToString(CultureInfo.InvariantCulture);
        dto.CustomPrefs["skipBackLength"] = displayPreferences.SkipBackwardLength.ToString(CultureInfo.InvariantCulture);
        dto.CustomPrefs["enableNextVideoInfoOverlay"] = displayPreferences.EnableNextVideoInfoOverlay.ToString(CultureInfo.InvariantCulture);
        dto.CustomPrefs["tvhome"] = displayPreferences.TvHome;
        dto.CustomPrefs["dashboardTheme"] = displayPreferences.DashboardTheme;

        // Load all custom display preferences
        var customDisplayPreferences = _displayPreferencesManager.ListCustomItemDisplayPreferences(displayPreferences.UserId, itemId, displayPreferences.Client);
        foreach (var (key, value) in customDisplayPreferences)
        {
            dto.CustomPrefs.TryAdd(key, value);
        }

        // This will essentially be a noop if no changes have been made, but new prefs must be saved at least.
        _displayPreferencesManager.SaveChanges();

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
        [FromQuery] Guid? userId,
        [FromQuery, Required] string client,
        [FromBody, Required] DisplayPreferencesDto displayPreferences)
    {
        userId = RequestHelpers.GetUserId(User, userId);

        HomeSectionType[] defaults =
        {
            HomeSectionType.SmallLibraryTiles,
            HomeSectionType.Resume,
            HomeSectionType.ResumeAudio,
            HomeSectionType.ResumeBook,
            HomeSectionType.LiveTv,
            HomeSectionType.NextUp,
            HomeSectionType.LatestMedia,
            HomeSectionType.None,
        };

        if (!Guid.TryParse(displayPreferencesId, out var itemId))
        {
            itemId = displayPreferencesId.GetMD5();
        }

        var existingDisplayPreferences = _displayPreferencesManager.GetDisplayPreferences(userId.Value, itemId, client);
        existingDisplayPreferences.IndexBy = Enum.TryParse<IndexingKind>(displayPreferences.IndexBy, true, out var indexBy) ? indexBy : null;
        existingDisplayPreferences.ShowBackdrop = displayPreferences.ShowBackdrop;
        existingDisplayPreferences.ShowSidebar = displayPreferences.ShowSidebar;

        existingDisplayPreferences.ScrollDirection = displayPreferences.ScrollDirection;
        existingDisplayPreferences.ChromecastVersion = displayPreferences.CustomPrefs.TryGetValue("chromecastVersion", out var chromecastVersion)
                                                       && !string.IsNullOrEmpty(chromecastVersion)
            ? Enum.Parse<ChromecastVersion>(chromecastVersion, true)
            : ChromecastVersion.Stable;
        displayPreferences.CustomPrefs.Remove("chromecastVersion");

        existingDisplayPreferences.EnableNextVideoInfoOverlay = !displayPreferences.CustomPrefs.TryGetValue("enableNextVideoInfoOverlay", out var enableNextVideoInfoOverlay)
                                                                || string.IsNullOrEmpty(enableNextVideoInfoOverlay)
                                                                || bool.Parse(enableNextVideoInfoOverlay);
        displayPreferences.CustomPrefs.Remove("enableNextVideoInfoOverlay");

        existingDisplayPreferences.SkipBackwardLength = displayPreferences.CustomPrefs.TryGetValue("skipBackLength", out var skipBackLength)
                                                        && !string.IsNullOrEmpty(skipBackLength)
            ? int.Parse(skipBackLength, CultureInfo.InvariantCulture)
            : 10000;
        displayPreferences.CustomPrefs.Remove("skipBackLength");

        existingDisplayPreferences.SkipForwardLength = displayPreferences.CustomPrefs.TryGetValue("skipForwardLength", out var skipForwardLength)
                                                       && !string.IsNullOrEmpty(skipForwardLength)
            ? int.Parse(skipForwardLength, CultureInfo.InvariantCulture)
            : 30000;
        displayPreferences.CustomPrefs.Remove("skipForwardLength");

        existingDisplayPreferences.DashboardTheme = displayPreferences.CustomPrefs.TryGetValue("dashboardTheme", out var theme)
            ? theme
            : string.Empty;
        displayPreferences.CustomPrefs.Remove("dashboardTheme");

        existingDisplayPreferences.TvHome = displayPreferences.CustomPrefs.TryGetValue("tvhome", out var home)
            ? home
            : string.Empty;
        displayPreferences.CustomPrefs.Remove("tvhome");

        existingDisplayPreferences.HomeSections.Clear();

        foreach (var key in displayPreferences.CustomPrefs.Keys.Where(key => key.StartsWith("homesection", StringComparison.OrdinalIgnoreCase)))
        {
            var order = int.Parse(key.AsSpan().Slice("homesection".Length), CultureInfo.InvariantCulture);
            if (!Enum.TryParse<HomeSectionType>(displayPreferences.CustomPrefs[key], true, out var type))
            {
                type = order < 8 ? defaults[order] : HomeSectionType.None;
            }

            displayPreferences.CustomPrefs.Remove(key);
            existingDisplayPreferences.HomeSections.Add(new HomeSection { Order = order, Type = type });
        }

        foreach (var key in displayPreferences.CustomPrefs.Keys.Where(key => key.StartsWith("landing-", StringComparison.OrdinalIgnoreCase)))
        {
            if (!Enum.TryParse<ViewType>(displayPreferences.CustomPrefs[key], true, out _))
            {
                _logger.LogError("Invalid ViewType: {LandingScreenOption}", displayPreferences.CustomPrefs[key]);
                displayPreferences.CustomPrefs.Remove(key);
            }
        }

        var itemPrefs = _displayPreferencesManager.GetItemDisplayPreferences(existingDisplayPreferences.UserId, itemId, existingDisplayPreferences.Client);
        itemPrefs.SortBy = displayPreferences.SortBy ?? "SortName";
        itemPrefs.SortOrder = displayPreferences.SortOrder;
        itemPrefs.RememberIndexing = displayPreferences.RememberIndexing;
        itemPrefs.RememberSorting = displayPreferences.RememberSorting;
        itemPrefs.ItemId = itemId;

        // Set all remaining custom preferences.
        _displayPreferencesManager.SetCustomItemDisplayPreferences(userId.Value, itemId, existingDisplayPreferences.Client, displayPreferences.CustomPrefs);
        _displayPreferencesManager.SaveChanges();

        return NoContent();
    }
}
