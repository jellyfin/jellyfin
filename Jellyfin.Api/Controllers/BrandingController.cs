using MediaBrowser.Model.Branding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Branding controller.
/// </summary>
public class BrandingController : BaseJellyfinApiController
{
    private readonly IOptions<BrandingOptions> _brandingOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrandingController"/> class.
    /// </summary>
    /// <param name="brandingOptions">Instance of the <see cref="IOptions{BrandingOptions}"/> interface.</param>
    public BrandingController(IOptions<BrandingOptions> brandingOptions)
    {
        _brandingOptions = brandingOptions;
    }

    /// <summary>
    /// Gets branding configuration.
    /// </summary>
    /// <response code="200">Branding configuration returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the branding configuration.</returns>
    [HttpGet("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<BrandingOptionsDto> GetBrandingOptions()
    {
        var opts = _brandingOptions.Value;

        var brandingOptionsDto = new BrandingOptionsDto
        {
            LoginDisclaimer = opts.LoginDisclaimer,
            CustomCss = opts.CustomCss,
            SplashscreenEnabled = opts.SplashscreenEnabled
        };

        return brandingOptionsDto;
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
        return _brandingOptions.Value.CustomCss ?? string.Empty;
    }
}
