using System.Collections.Generic;
using Jellyfin.Api.Constants;
using MediaBrowser.Common.Api;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Localization controller.
/// </summary>
[Authorize(Policy = Policies.FirstTimeSetupOrDefault)]
public class LocalizationController : BaseJellyfinApiController
{
    private readonly ILocalizationManager _localization;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationController"/> class.
    /// </summary>
    /// <param name="localization">Instance of the <see cref="ILocalizationManager"/> interface.</param>
    public LocalizationController(ILocalizationManager localization)
    {
        _localization = localization;
    }

    /// <summary>
    /// Gets known cultures.
    /// </summary>
    /// <response code="200">Known cultures returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of cultures.</returns>
    [HttpGet("Cultures")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CultureDto>> GetCultures()
    {
        return Ok(_localization.GetCultures());
    }

    /// <summary>
    /// Gets known countries.
    /// </summary>
    /// <response code="200">Known countries returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of countries.</returns>
    [HttpGet("Countries")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<CountryInfo>> GetCountries()
    {
        return Ok(_localization.GetCountries());
    }

    /// <summary>
    /// Gets known parental ratings.
    /// </summary>
    /// <response code="200">Known parental ratings returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of parental ratings.</returns>
    [HttpGet("ParentalRatings")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ParentalRating>> GetParentalRatings()
    {
        return Ok(_localization.GetParentalRatings());
    }

    /// <summary>
    /// Gets localization options.
    /// </summary>
    /// <response code="200">Localization options returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the list of localization options.</returns>
    [HttpGet("Options")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<LocalizationOption>> GetLocalizationOptions()
    {
        return Ok(_localization.GetLocalizationOptions());
    }
}
