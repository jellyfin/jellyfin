using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.ConfigurationDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
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
    /// Updates the path to the media encoder.
    /// </summary>
    /// <param name="mediaEncoderPath">Media encoder path form body.</param>
    /// <response code="204">Media encoder path updated.</response>
    /// <returns>Status.</returns>
    [Obsolete("This endpoint is obsolete.")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("MediaEncoder/Path")]
    [Authorize(Policy = Policies.FirstTimeSetupOrElevated)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateMediaEncoderPath([FromBody, Required] MediaEncoderPathDto mediaEncoderPath)
    {
        // API ENDPOINT DISABLED (NOOP) FOR SECURITY PURPOSES
        // _mediaEncoder.UpdateEncoderPath(mediaEncoderPath.Path, mediaEncoderPath.PathType);
        return NoContent();
    }
}
