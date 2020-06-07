#pragma warning disable CS1591
#pragma warning disable SA1402
#pragma warning disable SA1649

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class GetDashboardConfigurationPages.
    /// </summary>
    [Route("/web/ConfigurationPages", "GET")]
    public class GetDashboardConfigurationPages : IReturn<List<ConfigurationPageInfo>>
    {
        /// <summary>
        /// Gets or sets the type of the page.
        /// </summary>
        /// <value>The type of the page.</value>
        public ConfigurationPageType? PageType { get; set; }

        public bool? EnableInMainMenu { get; set; }
    }

    /// <summary>
    /// Class GetDashboardConfigurationPage.
    /// </summary>
    [Route("/web/ConfigurationPage", "GET")]
    public class GetDashboardConfigurationPage
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
    }

    [Route("/robots.txt", "GET", IsHidden = true)]
    public class GetRobotsTxt
    {
    }

    /// <summary>
    /// Class GetDashboardResource.
    /// </summary>
    [Route("/web/{ResourceName*}", "GET", IsHidden = true)]
    public class GetDashboardResource
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the V.
        /// </summary>
        /// <value>The V.</value>
        public string V { get; set; }
    }

    [Route("/favicon.ico", "GET", IsHidden = true)]
    public class GetFavIcon
    {
    }

    /// <summary>
    /// Class DashboardService.
    /// </summary>
    public class DashboardService : IService, IRequiresRequest
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private readonly ILogger _logger;

        /// <summary>
        /// Gets or sets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        private readonly IHttpResultFactory _resultFactory;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfiguration _appConfig;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IFileSystem _fileSystem;
        private readonly IResourceFileManager _resourceFileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="appConfig">The application configuration.</param>
        /// <param name="resourceFileManager">The resource file manager.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="resultFactory">The result factory.</param>
        public DashboardService(
            ILogger<DashboardService> logger,
            IServerApplicationHost appHost,
            IConfiguration appConfig,
            IResourceFileManager resourceFileManager,
            IServerConfigurationManager serverConfigurationManager,
            IFileSystem fileSystem,
            IHttpResultFactory resultFactory)
        {
            _logger = logger;
            _appHost = appHost;
            _appConfig = appConfig;
            _resourceFileManager = resourceFileManager;
            _serverConfigurationManager = serverConfigurationManager;
            _fileSystem = fileSystem;
            _resultFactory = resultFactory;
        }

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        /// <summary>
        /// Gets the path of the directory containing the static web interface content, or null if the server is not
        /// hosting the web client.
        /// </summary>
        public string DashboardUIPath => GetDashboardUIPath(_appConfig, _serverConfigurationManager);

        /// <summary>
        /// Gets the path of the directory containing the static web interface content.
        /// </summary>
        /// <param name="appConfig">The app configuration.</param>
        /// <param name="serverConfigManager">The server configuration manager.</param>
        /// <returns>The directory path, or null if the server is not hosting the web client.</returns>
        public static string GetDashboardUIPath(IConfiguration appConfig, IServerConfigurationManager serverConfigManager)
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

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetFavIcon request)
        {
            return Get(new GetDashboardResource
            {
                ResourceName = "favicon.ico"
            });
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public Task<object> Get(GetDashboardConfigurationPage request)
        {
            IPlugin plugin = null;
            Stream stream = null;

            var isJs = false;
            var isTemplate = false;

            var page = ServerEntryPoint.Instance.PluginConfigurationPages.FirstOrDefault(p => string.Equals(p.Name, request.Name, StringComparison.OrdinalIgnoreCase));
            if (page != null)
            {
                plugin = page.Plugin;
                stream = page.GetHtmlStream();
            }

            if (plugin == null)
            {
                var altPage = GetPluginPages().FirstOrDefault(p => string.Equals(p.Item1.Name, request.Name, StringComparison.OrdinalIgnoreCase));
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
                    return _resultFactory.GetStaticResult(Request, plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.js"), () => Task.FromResult(stream));
                }

                if (isTemplate)
                {
                    return _resultFactory.GetStaticResult(Request, plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => Task.FromResult(stream));
                }

                return _resultFactory.GetStaticResult(Request, plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => Task.FromResult(stream));
            }

            throw new ResourceNotFoundException();
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardConfigurationPages request)
        {
            const string unavailableMessage = "The server is still loading. Please try again momentarily.";

            var instance = ServerEntryPoint.Instance;

            if (instance == null)
            {
                throw new InvalidOperationException(unavailableMessage);
            }

            var pages = instance.PluginConfigurationPages;

            if (pages == null)
            {
                throw new InvalidOperationException(unavailableMessage);
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

            if (request.PageType.HasValue)
            {
                configPages = configPages.Where(p => p.ConfigurationPageType == request.PageType.Value).ToList();
            }

            if (request.EnableInMainMenu.HasValue)
            {
                configPages = configPages.Where(p => p.EnableInMainMenu == request.EnableInMainMenu.Value).ToList();
            }

            return configPages;
        }

        private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages()
        {
            return _appHost.Plugins.SelectMany(GetPluginPages);
        }

        private IEnumerable<Tuple<PluginPageInfo, IPlugin>> GetPluginPages(IPlugin plugin)
        {
            var hasConfig = plugin as IHasWebPages;

            if (hasConfig == null)
            {
                return new List<Tuple<PluginPageInfo, IPlugin>>();
            }

            return hasConfig.GetPages().Select(i => new Tuple<PluginPageInfo, IPlugin>(i, plugin));
        }

        private IEnumerable<ConfigurationPageInfo> GetConfigPages(IPlugin plugin)
        {
            return GetPluginPages(plugin).Select(i => new ConfigurationPageInfo(plugin, i.Item1));
        }

        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "request", Justification = "Required for ServiceStack")]
        public object Get(GetRobotsTxt request)
        {
            return Get(new GetDashboardResource
            {
                ResourceName = "robots.txt"
            });
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public async Task<object> Get(GetDashboardResource request)
        {
            if (!_appConfig.HostWebClient() || DashboardUIPath == null)
            {
                throw new ResourceNotFoundException();
            }

            var path = request?.ResourceName;
            var basePath = DashboardUIPath;

            // Bounce them to the startup wizard if it hasn't been completed yet
            if (!_serverConfigurationManager.Configuration.IsStartupWizardCompleted
                && !Request.RawUrl.Contains("wizard", StringComparison.OrdinalIgnoreCase)
                && Request.RawUrl.Contains("index", StringComparison.OrdinalIgnoreCase))
            {
                Request.Response.Redirect("index.html?start=wizard#!/wizardstart.html");
                return null;
            }

            return await _resultFactory.GetStaticFileResult(Request, _resourceFileManager.GetResourcePath(basePath, path)).ConfigureAwait(false);
        }
    }
}
