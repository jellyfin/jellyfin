using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Plugins controller.
/// </summary>
[Authorize(Policy = Policies.RequiresElevation)]
public class PluginsController : BaseJellyfinApiController
{
    private readonly IInstallationManager _installationManager;
    private readonly IPluginManager _pluginManager;
    private readonly JsonSerializerOptions _serializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginsController"/> class.
    /// </summary>
    /// <param name="installationManager">Instance of the <see cref="IInstallationManager"/> interface.</param>
    /// <param name="pluginManager">Instance of the <see cref="IPluginManager"/> interface.</param>
    public PluginsController(
        IInstallationManager installationManager,
        IPluginManager pluginManager)
    {
        _installationManager = installationManager;
        _pluginManager = pluginManager;
        _serializerOptions = JsonDefaults.Options;
    }

    /// <summary>
    /// Gets a list of currently installed plugins.
    /// </summary>
    /// <response code="200">Installed plugins returned.</response>
    /// <returns>List of currently installed plugins.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PluginInfo>> GetPlugins()
    {
        return Ok(_pluginManager.Plugins
            .OrderBy(p => p.Name)
            .Select(p => p.GetPluginInfo()));
    }

    /// <summary>
    /// Enables a disabled plugin.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <param name="version">Plugin version.</param>
    /// <response code="204">Plugin enabled.</response>
    /// <response code="404">Plugin not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpPost("{pluginId}/{version}/Enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult EnablePlugin([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
    {
        var plugin = _pluginManager.GetPlugin(pluginId, version);
        if (plugin is null)
        {
            return NotFound();
        }

        _pluginManager.EnablePlugin(plugin);
        return NoContent();
    }

    /// <summary>
    /// Disable a plugin.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <param name="version">Plugin version.</param>
    /// <response code="204">Plugin disabled.</response>
    /// <response code="404">Plugin not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpPost("{pluginId}/{version}/Disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DisablePlugin([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
    {
        var plugin = _pluginManager.GetPlugin(pluginId, version);
        if (plugin is null)
        {
            return NotFound();
        }

        _pluginManager.DisablePlugin(plugin);
        return NoContent();
    }

    /// <summary>
    /// Uninstalls a plugin by version.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <param name="version">Plugin version.</param>
    /// <response code="204">Plugin uninstalled.</response>
    /// <response code="404">Plugin not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpDelete("{pluginId}/{version}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult UninstallPluginByVersion([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
    {
        var plugin = _pluginManager.GetPlugin(pluginId, version);
        if (plugin is null)
        {
            return NotFound();
        }

        _installationManager.UninstallPlugin(plugin);
        return NoContent();
    }

    /// <summary>
    /// Uninstalls a plugin.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <response code="204">Plugin uninstalled.</response>
    /// <response code="404">Plugin not found.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpDelete("{pluginId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Please use the UninstallPluginByVersion API.")]
    public ActionResult UninstallPlugin([FromRoute, Required] Guid pluginId)
    {
        // If no version is given, return the current instance.
        var plugins = _pluginManager.Plugins.Where(p => p.Id.Equals(pluginId)).ToList();

        // Select the un-instanced one first.
        var plugin = plugins.FirstOrDefault(p => p.Instance is null) ?? plugins.MinBy(p => p.Manifest.Status);

        if (plugin is not null)
        {
            _installationManager.UninstallPlugin(plugin);
            return NoContent();
        }

        return NotFound();
    }

    /// <summary>
    /// Gets plugin configuration.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <response code="200">Plugin configuration returned.</response>
    /// <response code="404">Plugin not found or plugin configuration not found.</response>
    /// <returns>Plugin configuration.</returns>
    [HttpGet("{pluginId}/Configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BasePluginConfiguration> GetPluginConfiguration([FromRoute, Required] Guid pluginId)
    {
        var plugin = _pluginManager.GetPlugin(pluginId);
        if (plugin?.Instance is IHasPluginConfiguration configPlugin)
        {
            return configPlugin.Configuration;
        }

        return NotFound();
    }

    /// <summary>
    /// Updates plugin configuration.
    /// </summary>
    /// <remarks>
    /// Accepts plugin configuration as JSON body.
    /// </remarks>
    /// <param name="pluginId">Plugin id.</param>
    /// <response code="204">Plugin configuration updated.</response>
    /// <response code="404">Plugin not found or plugin does not have configuration.</response>
    /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpPost("{pluginId}/Configuration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdatePluginConfiguration([FromRoute, Required] Guid pluginId)
    {
        var plugin = _pluginManager.GetPlugin(pluginId);
        if (plugin?.Instance is not IHasPluginConfiguration configPlugin)
        {
            return NotFound();
        }

        var configuration = (BasePluginConfiguration?)await JsonSerializer.DeserializeAsync(Request.Body, configPlugin.ConfigurationType, _serializerOptions)
            .ConfigureAwait(false);

        if (configuration is not null)
        {
            configPlugin.UpdateConfiguration(configuration);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets a plugin's image.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <param name="version">Plugin version.</param>
    /// <response code="200">Plugin image returned.</response>
    /// <returns>Plugin's image.</returns>
    [HttpGet("{pluginId}/{version}/Image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesImageFile]
    [AllowAnonymous]
    public ActionResult GetPluginImage([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
    {
        var plugin = _pluginManager.GetPlugin(pluginId, version);
        if (plugin is null)
        {
            return NotFound();
        }

        var imagePath = Path.Combine(plugin.Path, plugin.Manifest.ImagePath ?? string.Empty);
        if (plugin.Manifest.ImagePath is null || !System.IO.File.Exists(imagePath))
        {
            return NotFound();
        }

        imagePath = Path.Combine(plugin.Path, plugin.Manifest.ImagePath);
        return PhysicalFile(imagePath, MimeTypes.GetMimeType(imagePath));
    }

    /// <summary>
    /// Gets a plugin's manifest.
    /// </summary>
    /// <param name="pluginId">Plugin id.</param>
    /// <response code="204">Plugin manifest returned.</response>
    /// <response code="404">Plugin not found.</response>
    /// <returns>A <see cref="PluginManifest"/> on success, or a <see cref="NotFoundResult"/> if the plugin could not be found.</returns>
    [HttpPost("{pluginId}/Manifest")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PluginManifest> GetPluginManifest([FromRoute, Required] Guid pluginId)
    {
        var plugin = _pluginManager.GetPlugin(pluginId);

        if (plugin is not null)
        {
            return plugin.Manifest;
        }

        return NotFound();
    }
}
