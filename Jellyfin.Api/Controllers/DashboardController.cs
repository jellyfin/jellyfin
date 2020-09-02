using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Models;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The dashboard controller.
    /// </summary>
    [Route("")]
    public class DashboardController : BaseJellyfinApiController
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _appConfig;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IResourceFileManager _resourceFileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardController"/> class.
        /// </summary>
        /// <param name="logger">Instance of <see cref="ILogger{DashboardController}"/> interface.</param>
        /// <param name="appHost">Instance of <see cref="IServerApplicationHost"/> interface.</param>
        /// <param name="appConfig">Instance of <see cref="IConfiguration"/> interface.</param>
        /// <param name="resourceFileManager">Instance of <see cref="IResourceFileManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/> interface.</param>
        public DashboardController(
            ILogger<DashboardController> logger,
            IServerApplicationHost appHost,
            IConfiguration appConfig,
            IResourceFileManager resourceFileManager,
            IServerConfigurationManager serverConfigurationManager)
        {
            _logger = logger;
            _appHost = appHost;
            _appConfig = appConfig;
            _resourceFileManager = resourceFileManager;
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Gets the path of the directory containing the static web interface content, or null if the server is not
        /// hosting the web client.
        /// </summary>
        private string? WebClientUiPath => GetWebClientUiPath(_appConfig, _serverConfigurationManager);

        /// <summary>
        /// Gets the configuration pages.
        /// </summary>
        /// <param name="enableInMainMenu">Whether to enable in the main menu.</param>
        /// <param name="pageType">The <see cref="ConfigurationPageInfo"/>.</param>
        /// <response code="200">ConfigurationPages returned.</response>
        /// <response code="404">Server still loading.</response>
        /// <returns>An <see cref="IEnumerable{ConfigurationPageInfo}"/> with infos about the plugins.</returns>
        [HttpGet("web/ConfigurationPages")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<ConfigurationPageInfo?>> GetConfigurationPages(
            [FromQuery] bool? enableInMainMenu,
            [FromQuery] ConfigurationPageType? pageType)
        {
            const string unavailableMessage = "The server is still loading. Please try again momentarily.";

            var pages = _appHost.GetExports<IPluginConfigurationPage>().ToList();

            if (pages == null)
            {
                return NotFound(unavailableMessage);
            }

            // Don't allow a failing plugin to fail them all
            var configPages = pages.Select(p =>
                {
                    try
                    {
                        return new ConfigurationPageInfo(p);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting plugin information from {Plugin}", p.GetType().Name);
                        return null;
                    }
                })
                .Where(i => i != null)
                .ToList();

            configPages.AddRange(_appHost.Plugins.SelectMany(GetConfigPages));

            if (pageType.HasValue)
            {
                configPages = configPages.Where(p => p!.ConfigurationPageType == pageType).ToList();
            }

            if (enableInMainMenu.HasValue)
            {
                configPages = configPages.Where(p => p!.EnableInMainMenu == enableInMainMenu.Value).ToList();
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
            IPlugin? plugin = null;
            Stream? stream = null;

            var isJs = false;
            var isTemplate = false;

            var page = _appHost.GetExports<IPluginConfigurationPage>().FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (page != null)
            {
                plugin = page.Plugin;
                stream = page.GetHtmlStream();
            }

            if (plugin == null)
            {
                var altPage = GetPluginPages().FirstOrDefault(p => string.Equals(p.Item1.Name, name, StringComparison.OrdinalIgnoreCase));
                if (altPage != null)
                {
                    plugin = altPage.Item2;
                    stream = plugin.GetType().Assembly.GetManifestResourceStream(altPage.Item1.EmbeddedResourcePath);

                    isJs = string.Equals(Path.GetExtension(altPage.Item1.EmbeddedResourcePath), ".js", StringComparison.OrdinalIgnoreCase);
                    isTemplate = altPage.Item1.EmbeddedResourcePath.EndsWith(".template.html", StringComparison.Ordinal);
                }
            }

            if (plugin != null && stream != null)
            {
                if (isJs)
                {
                    return File(stream, MimeTypes.GetMimeType("page.js"));
                }

                if (isTemplate)
                {
                    return File(stream, MimeTypes.GetMimeType("page.html"));
                }

                return File(stream, MimeTypes.GetMimeType("page.html"));
            }

            return NotFound();
        }

        /// <summary>
        /// Gets the robots.txt.
        /// </summary>
        /// <response code="200">Robots.txt returned.</response>
        /// <returns>The robots.txt.</returns>
        [HttpGet("robots.txt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult GetRobotsTxt()
        {
            return GetWebClientResource("robots.txt");
        }

        /// <summary>
        /// Gets a resource from the web client.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <response code="200">Web client returned.</response>
        /// <response code="404">Server does not host a web client.</response>
        /// <returns>The resource.</returns>
        [HttpGet("web/{*resourceName}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult GetWebClientResource([FromRoute] string resourceName)
        {
            if (!_appConfig.HostWebClient() || WebClientUiPath == null)
            {
                return NotFound("Server does not host a web client.");
            }

            var path = resourceName;
            var basePath = WebClientUiPath;

            var requestPathAndQuery = Request.GetEncodedPathAndQuery();
            // Bounce them to the startup wizard if it hasn't been completed yet
            if (!_serverConfigurationManager.Configuration.IsStartupWizardCompleted
                && !requestPathAndQuery.Contains("wizard", StringComparison.OrdinalIgnoreCase)
                && requestPathAndQuery.Contains("index", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect("index.html?start=wizard#!/wizardstart.html");
            }

            return PhysicalFile(_resourceFileManager.GetResourcePath(basePath, path), MimeTypes.GetMimeType(path));
        }

        /// <summary>
        /// Gets the favicon.
        /// </summary>
        /// <response code="200">Favicon.ico returned.</response>
        /// <returns>The favicon.</returns>
        [HttpGet("favicon.ico")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult GetFavIcon()
        {
            return GetWebClientResource("favicon.ico");
        }

        /// <summary>
        /// Gets the path of the directory containing the static web interface content.
        /// </summary>
        /// <param name="appConfig">The app configuration.</param>
        /// <param name="serverConfigManager">The server configuration manager.</param>
        /// <returns>The directory path, or null if the server is not hosting the web client.</returns>
        public static string? GetWebClientUiPath(IConfiguration appConfig, IServerConfigurationManager serverConfigManager)
        {
            if (!appConfig.HostWebClient())
            {
                return null;
            }

            if (!string.IsNullOrEmpty(serverConfigManager.Configuration.DashboardSourcePath))
            {
                return serverConfigManager.Configuration.DashboardSourcePath;
            }

            return serverConfigManager.ApplicationPaths.WebPath;
        }

        private IEnumerable<ConfigurationPageInfo> GetConfigPages(IPlugin plugin)
        {
            return GetPluginPages(plugin).Select(i => new ConfigurationPageInfo(plugin, i.Item1));
        }

        private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages(IPlugin plugin)
        {
            if (!(plugin is IHasWebPages hasWebPages))
            {
                return new List<Tuple<PluginPageInfo, IPlugin>>();
            }

            return hasWebPages.GetPages().Select(i => new Tuple<PluginPageInfo, IPlugin>(i, plugin));
        }

        private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages()
        {
            return _appHost.Plugins.SelectMany(GetPluginPages);
        }
    }
}
