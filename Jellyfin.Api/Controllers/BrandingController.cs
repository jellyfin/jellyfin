using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Branding controller.
/// </summary>
public class BrandingController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _serverConfigurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrandingController"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public BrandingController(IServerConfigurationManager serverConfigurationManager)
    {
        _serverConfigurationManager = serverConfigurationManager;
    }

    /// <summary>
    /// Gets branding configuration.
    /// </summary>
    /// <response code="200">Branding configuration returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the branding configuration.</returns>
    [HttpGet("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BrandingOptions> GetBrandingOptions()
    {
        return _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
    }

    /// <summary>
    /// Gets branding css.
    /// </summary>
    /// <response code="200">Branding css returned.</response>
    /// <response code="204">No branding css configured.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the branding css if exist,
    /// or a <see cref="NoContentResult"/> if the css is not configured.
    /// </returns>
    [HttpGet("Css")]
    [HttpGet("Css.css", Name = "GetBrandingCss_2")]
    [Produces("text/css")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult<string> GetBrandingCss()
    {
        var options = _serverConfigurationManager.GetConfiguration<BrandingOptions>("branding");
        return options.CustomCss ?? string.Empty;
    }
}
