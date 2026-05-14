using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using Jellyfin.Api.Attributes;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Jellyfin.Api.Controllers;

#pragma warning disable CS0618 // IConfigurationManager is kept only for plugin/legacy named configuration fallback.

/// <summary>
/// Configuration Controller.
/// </summary>
[Route("System")]
[Authorize]
[Tags("System")]
public class ConfigurationController : BaseJellyfinApiController
{
    private readonly IConfigurationManager _configurationManager;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IWritableOptions<ServerConfiguration> _serverConfig;
    private readonly IWritableOptions<EncodingOptions> _encodingOptions;
    private readonly IWritableOptions<NetworkConfiguration> _networkOptions;
    private readonly IWritableOptions<BrandingOptions> _brandingOptions;
    private readonly IWritableOptions<LiveTvOptions> _liveTvOptions;
    private readonly IWritableOptions<XbmcMetadataOptions> _xbmcMetadataOptions;

    private readonly JsonSerializerOptions _serializerOptions = JsonDefaults.Options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationController"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IConfigurationManager"/> interface (used for legacy named configs).</param>
    /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
    /// <param name="serverConfig">Instance of the <see cref="IWritableOptions{ServerConfiguration}"/> interface.</param>
    /// <param name="encodingOptions">Instance of the <see cref="IWritableOptions{EncodingOptions}"/> interface.</param>
    /// <param name="networkOptions">Instance of the <see cref="IWritableOptions{NetworkConfiguration}"/> interface.</param>
    /// <param name="brandingOptions">Instance of the <see cref="IWritableOptions{BrandingOptions}"/> interface.</param>
    /// <param name="liveTvOptions">Instance of the <see cref="IWritableOptions{LiveTvOptions}"/> interface.</param>
    /// <param name="xbmcMetadataOptions">Instance of the <see cref="IWritableOptions{XbmcMetadataOptions}"/> interface.</param>
    public ConfigurationController(
        IConfigurationManager configurationManager,
        IMediaEncoder mediaEncoder,
        IWritableOptions<ServerConfiguration> serverConfig,
        IWritableOptions<EncodingOptions> encodingOptions,
        IWritableOptions<NetworkConfiguration> networkOptions,
        IWritableOptions<BrandingOptions> brandingOptions,
        IWritableOptions<LiveTvOptions> liveTvOptions,
        IWritableOptions<XbmcMetadataOptions> xbmcMetadataOptions)
    {
        _configurationManager = configurationManager;
        _mediaEncoder = mediaEncoder;
        _serverConfig = serverConfig;
        _encodingOptions = encodingOptions;
        _networkOptions = networkOptions;
        _brandingOptions = brandingOptions;
        _liveTvOptions = liveTvOptions;
        _xbmcMetadataOptions = xbmcMetadataOptions;
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
        return _serverConfig.Value;
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
        _serverConfig.Update(c =>
        {
            // Replace all mutable properties by copying fields from the incoming configuration.
            var incoming = configuration;
            foreach (var prop in typeof(ServerConfiguration).GetProperties())
            {
                if (prop.CanWrite)
                {
                    prop.SetValue(c, prop.GetValue(incoming));
                }
            }
        });
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
        return key switch
        {
            "encoding" => _encodingOptions.Value,
            "network" => _networkOptions.Value,
            "branding" => _brandingOptions.Value,
            "livetv" => _liveTvOptions.Value,
            "xbmcmetadata" => _xbmcMetadataOptions.Value,
            _ => _configurationManager.GetConfiguration(key)
        };
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
        switch (key)
        {
            case "encoding":
                var encoding = configuration.Deserialize<EncodingOptions>(_serializerOptions)
                    ?? throw new ArgumentException("Body doesn't contain a valid encoding configuration");
                _encodingOptions.Update(c =>
                {
                    foreach (var prop in typeof(EncodingOptions).GetProperties())
                    {
                        if (prop.CanWrite)
                        {
                            prop.SetValue(c, prop.GetValue(encoding));
                        }
                    }
                });
                break;

            case "network":
                var network = configuration.Deserialize<NetworkConfiguration>(_serializerOptions)
                    ?? throw new ArgumentException("Body doesn't contain a valid network configuration");
                _networkOptions.Update(c =>
                {
                    foreach (var prop in typeof(NetworkConfiguration).GetProperties())
                    {
                        if (prop.CanWrite)
                        {
                            prop.SetValue(c, prop.GetValue(network));
                        }
                    }
                });
                break;

            case "branding":
                var branding = configuration.Deserialize<BrandingOptions>(_serializerOptions)
                    ?? throw new ArgumentException("Body doesn't contain a valid branding configuration");
                _brandingOptions.Update(c =>
                {
                    c.LoginDisclaimer = branding.LoginDisclaimer;
                    c.CustomCss = branding.CustomCss;
                    c.SplashscreenEnabled = branding.SplashscreenEnabled;
                    c.SplashscreenLocation = branding.SplashscreenLocation;
                });
                break;

            case "livetv":
                var liveTv = configuration.Deserialize<LiveTvOptions>(_serializerOptions)
                    ?? throw new ArgumentException("Body doesn't contain a valid live tv configuration");
                _liveTvOptions.Update(c =>
                {
                    foreach (var prop in typeof(LiveTvOptions).GetProperties())
                    {
                        if (prop.CanWrite)
                        {
                            prop.SetValue(c, prop.GetValue(liveTv));
                        }
                    }
                });
                break;

            case "xbmcmetadata":
                var xbmc = configuration.Deserialize<XbmcMetadataOptions>(_serializerOptions)
                    ?? throw new ArgumentException("Body doesn't contain a valid xbmc metadata configuration");
                _xbmcMetadataOptions.Update(c =>
                {
                    foreach (var prop in typeof(XbmcMetadataOptions).GetProperties())
                    {
                        if (prop.CanWrite)
                        {
                            prop.SetValue(c, prop.GetValue(xbmc));
                        }
                    }
                });
                break;

            default:
                var configurationType = _configurationManager.GetConfigurationType(key);
                var deserializedConfiguration = configuration.Deserialize(configurationType, _serializerOptions);
                if (deserializedConfiguration is null)
                {
                    throw new ArgumentException("Body doesn't contain a valid configuration");
                }

                _configurationManager.SaveConfiguration(key, deserializedConfiguration);
                break;
        }

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
        _brandingOptions.Update(c =>
        {
            // Preserve SplashscreenLocation; only update the properties exposed by BrandingOptionsDto.
            c.LoginDisclaimer = configuration.LoginDisclaimer;
            c.CustomCss = configuration.CustomCss;
            c.SplashscreenEnabled = configuration.SplashscreenEnabled;
        });

        return NoContent();
    }
}

#pragma warning restore CS0618
