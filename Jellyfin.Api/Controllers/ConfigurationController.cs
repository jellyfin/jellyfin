using System.ComponentModel.DataAnnotations;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Configuration Controller.
/// </summary>
[Route("System")]
[Authorize]
[Tags("System")]
public class ConfigurationController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    public ConfigurationController(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets application configuration.
    /// </summary>
    /// <response code="200">Application configuration returned.</response>
    /// <returns>Application configuration.</returns>
    [HttpGet("Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ServerConfiguration> GetConfiguration()
    {
        return _configurationManager.Configuration;
    }

    /// <summary>
    /// Updates application configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <response code="204">Configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateConfiguration([FromBody, Required] ServerConfiguration configuration)
    {
        _configurationManager.ReplaceConfiguration(configuration);
        return NoContent();
    }

    /// <summary>
    /// Gets a default MetadataOptions object.
    /// </summary>
    /// <response code="200">Metadata options returned.</response>
    /// <returns>Default MetadataOptions.</returns>
    [HttpGet("Configuration/MetadataOptions/Default")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<MetadataOptions> GetDefaultMetadataOptions()
    {
        return new MetadataOptions();
    }

    /// <summary>
    /// Gets encoding configuration.
    /// </summary>
    /// <response code="200">Encoding configuration returned.</response>
    /// <returns>Encoding configuration.</returns>
    [HttpGet("Configuration/Encoding")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<EncodingOptions> GetEncodingConfiguration()
    {
        return _configurationManager.GetConfiguration<EncodingOptions>("encoding");
    }

    /// <summary>
    /// Updates encoding configuration.
    /// </summary>
    /// <param name="configuration">Encoding configuration.</param>
    /// <response code="204">Encoding configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Encoding")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateEncodingConfiguration([FromBody, Required] EncodingOptions configuration)
    {
        _configurationManager.SaveConfiguration("encoding", configuration);
        return NoContent();
    }

    /// <summary>
    /// Gets network configuration.
    /// </summary>
    /// <response code="200">Network configuration returned.</response>
    /// <returns>Network configuration.</returns>
    [HttpGet("Configuration/Network")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<NetworkConfiguration> GetNetworkConfiguration()
    {
        return _configurationManager.GetConfiguration<NetworkConfiguration>("network");
    }

    /// <summary>
    /// Updates network configuration.
    /// </summary>
    /// <param name="configuration">Network configuration.</param>
    /// <response code="204">Network configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Network")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateNetworkConfiguration([FromBody, Required] NetworkConfiguration configuration)
    {
        _configurationManager.SaveConfiguration("network", configuration);
        return NoContent();
    }

    /// <summary>
    /// Gets metadata configuration.
    /// </summary>
    /// <response code="200">Metadata configuration returned.</response>
    /// <returns>Metadata configuration.</returns>
    [HttpGet("Configuration/Metadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<MetadataConfiguration> GetMetadataConfiguration()
    {
        return _configurationManager.GetConfiguration<MetadataConfiguration>("metadata");
    }

    /// <summary>
    /// Updates metadata configuration.
    /// </summary>
    /// <param name="configuration">Metadata configuration.</param>
    /// <response code="204">Metadata configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Metadata")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateMetadataConfiguration([FromBody, Required] MetadataConfiguration configuration)
    {
        _configurationManager.SaveConfiguration("metadata", configuration);
        return NoContent();
    }

    /// <summary>
    /// Gets XbmcMetadata configuration.
    /// </summary>
    /// <response code="200">XbmcMetadata configuration returned.</response>
    /// <returns>XbmcMetadata configuration.</returns>
    [HttpGet("Configuration/XbmcMetadata")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<XbmcMetadataOptions> GetXbmcMetadataConfiguration()
    {
        return _configurationManager.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");
    }

    /// <summary>
    /// Updates XbmcMetadata configuration.
    /// </summary>
    /// <param name="configuration">XbmcMetadata configuration.</param>
    /// <response code="204">XbmcMetadata configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/XbmcMetadata")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateXbmcMetadataConfiguration([FromBody, Required] XbmcMetadataOptions configuration)
    {
        _configurationManager.SaveConfiguration("xbmcmetadata", configuration);
        return NoContent();
    }

    /// <summary>
    /// Gets LiveTv configuration.
    /// </summary>
    /// <response code="200">LiveTv configuration returned.</response>
    /// <returns>LiveTv configuration.</returns>
    [HttpGet("Configuration/LiveTv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<LiveTvOptions> GetLiveTvConfiguration()
    {
        return _configurationManager.GetConfiguration<LiveTvOptions>("livetv");
    }

    /// <summary>
    /// Updates LiveTv configuration.
    /// </summary>
    /// <param name="configuration">LiveTv configuration.</param>
    /// <response code="204">LiveTv configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/LiveTv")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateLiveTvConfiguration([FromBody, Required] LiveTvOptions configuration)
    {
        _configurationManager.SaveConfiguration("livetv", configuration);
        return NoContent();
    }

    /// <summary>
    /// Updates branding configuration.
    /// </summary>
    /// <param name="configuration">Branding configuration.</param>
    /// <response code="204">Branding configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/Branding")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateBrandingConfiguration([FromBody, Required] BrandingOptionsDto configuration)
    {
        // Get the current branding configuration to preserve SplashscreenLocation
        var currentBranding = _configurationManager.GetConfiguration<BrandingOptions>("branding");

        // Update only the properties from BrandingOptionsDto
        currentBranding.LoginDisclaimer = configuration.LoginDisclaimer;
        currentBranding.CustomCss = configuration.CustomCss;
        currentBranding.SplashscreenEnabled = configuration.SplashscreenEnabled;

        _configurationManager.SaveConfiguration("branding", currentBranding);

        return NoContent();
    }
}
