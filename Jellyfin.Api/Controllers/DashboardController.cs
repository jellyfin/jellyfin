using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Models;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// The dashboard controller.
/// </summary>
[Route("")]
public class DashboardController : BaseJellyfinApiController
{
    private readonly ILogger<DashboardController> _logger;
    private readonly IPluginManager _pluginManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardController"/> class.
    /// </summary>
    /// <param name="logger">Instance of <see cref="ILogger{DashboardController}"/> interface.</param>
    /// <param name="pluginManager">Instance of <see cref="IPluginManager"/> interface.</param>
    public DashboardController(
        ILogger<DashboardController> logger,
        IPluginManager pluginManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;
    }

    /// <summary>
    /// Gets the configuration pages.
    /// </summary>
    /// <param name="enableInMainMenu">Whether to enable in the main menu.</param>
    /// <response code="200">ConfigurationPages returned.</response>
    /// <response code="404">Server still loading.</response>
    /// <returns>An <see cref="IEnumerable{ConfigurationPageInfo}"/> with infos about the plugins.</returns>
    [HttpGet("web/ConfigurationPages")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<ConfigurationPageInfo>> GetConfigurationPages(
        [FromQuery] bool? enableInMainMenu)
    {
        var configPages = _pluginManager.Plugins.SelectMany(GetConfigPages).ToList();

        if (enableInMainMenu.HasValue)
        {
            configPages = configPages.Where(p => p.EnableInMainMenu == enableInMainMenu.Value).ToList();
        }

        return configPages;
    }

    /// <summary>
    /// Gets a dashboard configuration page.
    /// </summary>
    /// <param name="name">The name of the page.</param>
    /// <response code="200">ConfigurationPage returned.</response>
    /// <response code="404">Plugin configuration page not found.</response>
    /// <returns>The configuration page.</returns>
    [HttpGet("web/ConfigurationPage")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesFile(MediaTypeNames.Text.Html, "application/x-javascript")]
    public ActionResult GetDashboardConfigurationPage([FromQuery] string? name)
    {
        var altPage = GetPluginPages().FirstOrDefault(p => string.Equals(p.Item1.Name, name, StringComparison.OrdinalIgnoreCase));
        if (altPage is null)
        {
            return NotFound();
        }

        IPlugin plugin = altPage.Item2;
        string resourcePath = altPage.Item1.EmbeddedResourcePath;
        Stream? stream = plugin.GetType().Assembly.GetManifestResourceStream(resourcePath);
        if (stream is null)
        {
            _logger.LogError("Failed to get resource {Resource} from plugin {Plugin}", resourcePath, plugin.Name);
            return NotFound();
        }

        return File(stream, MimeTypes.GetMimeType(resourcePath));
    }

    private IEnumerable<ConfigurationPageInfo> GetConfigPages(LocalPlugin plugin)
    {
        return GetPluginPages(plugin).Select(i => new ConfigurationPageInfo(plugin.Instance, i.Item1));
    }

    private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages(LocalPlugin plugin)
    {
        if (plugin.Instance is not IHasWebPages hasWebPages)
        {
            return Enumerable.Empty<Tuple<PluginPageInfo, IPlugin>>();
        }

        return hasWebPages.GetPages().Select(i => new Tuple<PluginPageInfo, IPlugin>(i, plugin.Instance));
    }

    private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages()
    {
        return _pluginManager.Plugins.SelectMany(GetPluginPages);
    }
}
