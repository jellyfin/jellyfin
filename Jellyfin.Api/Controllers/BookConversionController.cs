using System.ComponentModel.DataAnnotations;

using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ConfigurationDtos;

using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Controller to manage PDF → CBZ conversion settings.
/// </summary>
[Route("System/BookConversion")]
[Authorize]
public class BookConversionController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookConversionController"/> class.
    /// </summary>
    /// <param name="configurationManager">Server configuration manager.</param>
    public BookConversionController(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets current book conversion settings.
    /// </summary>
    /// <response code="200">Settings returned.</response>
    /// <returns>Book conversion settings.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BookConversionSettingsDto> Get()
    {
        var cfg = _configurationManager.Configuration;

        return new BookConversionSettingsDto
        {
            EnablePdfToCbzConversion = cfg.EnablePdfToCbzConversion,
            PdfToCbzDpi = cfg.PdfToCbzDpi,
            PdfToCbzReplaceOriginal = cfg.PdfToCbzReplaceOriginal
        };
    }

    /// <summary>
    /// Updates book conversion settings.
    /// </summary>
    /// <param name="settings">New settings.</param>
    /// <response code="204">Settings updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult Update([FromBody, Required] BookConversionSettingsDto settings)
    {
        var cfg = _configurationManager.Configuration;

        cfg.EnablePdfToCbzConversion = settings.EnablePdfToCbzConversion;
        cfg.PdfToCbzDpi = settings.PdfToCbzDpi;
        cfg.PdfToCbzReplaceOriginal = settings.PdfToCbzReplaceOriginal;

        _configurationManager.ReplaceConfiguration(cfg);

        return NoContent();
    }
}
