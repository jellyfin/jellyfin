using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.PluginDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Plugins controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class PluginsController : BaseJellyfinApiController
    {
        private readonly IInstallationManager _installationManager;
        private readonly IPluginManager _pluginManager;
        private readonly IConfigurationManager _config;
        private readonly JsonSerializerOptions _serializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginsController"/> class.
        /// </summary>
        /// <param name="installationManager">Instance of the <see cref="IInstallationManager"/> interface.</param>
        /// <param name="pluginManager">Instance of the <see cref="IPluginManager"/> interface.</param>
        /// <param name="config">Instance of the <see cref="IConfigurationManager"/> interface.</param>
        public PluginsController(
            IInstallationManager installationManager,
            IPluginManager pluginManager,
            IConfigurationManager config)
        {
            _installationManager = installationManager;
            _pluginManager = pluginManager;
            _serializerOptions = JsonDefaults.GetOptions();
            _config = config;
        }

        /// <summary>
        /// Get plugin security info.
        /// </summary>
        /// <response code="200">Plugin security info returned.</response>
        /// <returns>Plugin security info.</returns>
        [Obsolete("This endpoint should not be used.")]
        [HttpGet("SecurityInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public static ActionResult<PluginSecurityInfo> GetPluginSecurityInfo()
        {
            return new PluginSecurityInfo
            {
                IsMbSupporter = true,
                SupporterKey = "IAmTotallyLegit"
            };
        }

        /// <summary>
        /// Gets registration status for a feature.
        /// </summary>
        /// <param name="name">Feature name.</param>
        /// <response code="200">Registration status returned.</response>
        /// <returns>Mb registration record.</returns>
        [Obsolete("This endpoint should not be used.")]
        [HttpPost("RegistrationRecords/{name}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public static ActionResult<MBRegistrationRecord> GetRegistrationStatus([FromRoute, Required] string name)
        {
            return new MBRegistrationRecord
            {
                IsRegistered = true,
                RegChecked = true,
                TrialVersion = false,
                IsValid = true,
                RegError = false
            };
        }

        /// <summary>
        /// Gets registration status for a feature.
        /// </summary>
        /// <param name="name">Feature name.</param>
        /// <response code="501">Not implemented.</response>
        /// <returns>Not Implemented.</returns>
        /// <exception cref="NotImplementedException">This endpoint is not implemented.</exception>
        [Obsolete("Paid plugins are not supported")]
        [HttpGet("Registrations/{name}")]
        [ProducesResponseType(StatusCodes.Status501NotImplemented)]
        public static ActionResult GetRegistration([FromRoute, Required] string name)
        {
            // TODO Once we have proper apps and plugins and decide to break compatibility with paid plugins,
            // delete all these registration endpoints. They are only kept for compatibility.
            throw new NotImplementedException();
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
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult EnablePlugin([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
        {
            var plugin = _pluginManager.GetPlugin(pluginId, version);
            if (plugin == null)
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
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DisablePlugin([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
        {
            var plugin = _pluginManager.GetPlugin(pluginId, version);
            if (plugin == null)
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
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult UninstallPluginByVersion([FromRoute, Required] Guid pluginId, [FromRoute, Required] Version version)
        {
            var plugin = _pluginManager.GetPlugin(pluginId, version);
            if (plugin == null)
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
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Obsolete("Please use the UninstallPluginByVersion API.")]
        public ActionResult UninstallPlugin([FromRoute, Required] Guid pluginId)
        {
            // If no version is given, return the current instance.
            var plugins = _pluginManager.Plugins.Where(p => p.Id.Equals(pluginId));

            // Select the un-instanced one first.
            var plugin = plugins.FirstOrDefault(p => p.Instance == null);
            if (plugin == null)
            {
                // Then by the status.
                plugin = plugins.OrderBy(p => p.Manifest.Status).FirstOrDefault();
            }

            if (plugin != null)
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

            if (configuration != null)
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
            if (plugin == null)
            {
                return NotFound();
            }

            var imagePath = Path.Combine(plugin.Path, plugin.Manifest.ImagePath ?? string.Empty);
            if (plugin.Manifest.ImagePath == null || !System.IO.File.Exists(imagePath))
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

            if (plugin != null)
            {
                return plugin.Manifest;
            }

            return NotFound();
        }

        /// <summary>
        /// Updates plugin security info.
        /// </summary>
        /// <param name="pluginSecurityInfo">Plugin security info.</param>
        /// <response code="204">Plugin security info updated.</response>
        /// <returns>An <see cref="NoContentResult"/>.</returns>
        [Obsolete("This endpoint should not be used.")]
        [HttpPost("SecurityInfo")]
        [Authorize(Policy = Policies.RequiresElevation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult UpdatePluginSecurityInfo([FromBody, Required] PluginSecurityInfo pluginSecurityInfo)
        {
            return NoContent();
        }
    }
}
