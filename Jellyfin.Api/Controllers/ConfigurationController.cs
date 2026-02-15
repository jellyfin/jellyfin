using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using Jellyfin.Api.Attributes;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Configuration Controller.
/// </summary>
[Route("System")]
[Authorize]
public class ConfigurationController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IMediaEncoder _mediaEncoder;

    private readonly JsonSerializerOptions _serializerOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    public ConfigurationController(
        IServerConfigurationManager configurationManager,
        IMediaEncoder mediaEncoder)
    {
        _configurationManager = configurationManager;
        _mediaEncoder = mediaEncoder;
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
    /// Gets a named configuration.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <response code="200">Configuration returned.</response>
    /// <returns>Configuration.</returns>
    [HttpGet("Configuration/{key}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile(MediaTypeNames.Application.Json)]
    public ActionResult<object> GetNamedConfiguration([FromRoute, Required] string key)
    {
        return _configurationManager.GetConfiguration(key);
    }

    /// <summary>
    /// Updates named configuration.
    /// </summary>
    /// <param name="key">Configuration key.</param>
    /// <param name="configuration">Configuration.</param>
    /// <response code="204">Named configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration/{key}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateNamedConfiguration([FromRoute, Required] string key, [FromBody, Required] JsonDocument configuration)
    {
        var configurationType = _configurationManager.GetConfigurationType(key);
        var deserializedConfiguration = configuration.Deserialize(configurationType, _serializerOptions);

        if (deserializedConfiguration is null)
        {
            throw new ArgumentException("Body doesn't contain a valid configuration");
        }

        _configurationManager.SaveConfiguration(key, deserializedConfiguration);
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
        var currentBranding = (BrandingOptions)_configurationManager.GetConfiguration("branding");

        // Update only the properties from BrandingOptionsDto
        currentBranding.LoginDisclaimer = configuration.LoginDisclaimer;
        currentBranding.CustomCss = configuration.CustomCss;
        currentBranding.SplashscreenEnabled = configuration.SplashscreenEnabled;

        _configurationManager.SaveConfiguration("branding", currentBranding);

        return NoContent();
    }
}
