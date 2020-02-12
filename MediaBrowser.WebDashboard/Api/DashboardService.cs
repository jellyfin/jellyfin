using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class GetDashboardConfigurationPages
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
    /// Class GetDashboardConfigurationPage
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

    [Route("/web/Package", "GET", IsHidden = true)]
    public class GetDashboardPackage
    {
        public string Mode { get; set; }
    }

    [Route("/robots.txt", "GET", IsHidden = true)]
    public class GetRobotsTxt
    {
    }

    /// <summary>
    /// Class GetDashboardResource
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
    /// Class DashboardService
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

        /// <summary>
        /// Gets or sets the request context.
        /// </summary>
        /// <value>The request context.</value>
        public IRequest Request { get; set; }

        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IServerApplicationHost _appHost;

        /// <summary>
        /// The _server configuration manager
        /// </summary>
        private readonly IServerConfigurationManager _serverConfigurationManager;

        private readonly IFileSystem _fileSystem;
        private IResourceFileManager _resourceFileManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService" /> class.
        /// </summary>
        public DashboardService(
            IServerApplicationHost appHost,
            IResourceFileManager resourceFileManager,
            IServerConfigurationManager serverConfigurationManager,
            IFileSystem fileSystem,
            ILogger logger,
            IHttpResultFactory resultFactory)
        {
            _appHost = appHost;
            _serverConfigurationManager = serverConfigurationManager;
            _fileSystem = fileSystem;
            _logger = logger;
            _resultFactory = resultFactory;
            _resourceFileManager = resourceFileManager;
        }

        /// <summary>
        /// Gets the path for the web interface.
        /// </summary>
        /// <value>The path for the web interface.</value>
        public string DashboardUIPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_serverConfigurationManager.Configuration.DashboardSourcePath))
                {
                    return _serverConfigurationManager.Configuration.DashboardSourcePath;
                }

                return _serverConfigurationManager.ApplicationPaths.WebPath;
            }
        }

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
                    isTemplate = altPage.Item1.EmbeddedResourcePath.EndsWith(".template.html");
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

                return _resultFactory.GetStaticResult(Request, plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => GetPackageCreator(DashboardUIPath).ModifyHtml("dummy.html", stream, null, _appHost.ApplicationVersionString, null));
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
            var path = request.ResourceName;

            var contentType = MimeTypes.GetMimeType(path);
            var basePath = DashboardUIPath;

            // Bounce them to the startup wizard if it hasn't been completed yet
            if (!_serverConfigurationManager.Configuration.IsStartupWizardCompleted &&
                Request.RawUrl.IndexOf("wizard", StringComparison.OrdinalIgnoreCase) == -1 &&
                PackageCreator.IsCoreHtml(path))
            {
                // But don't redirect if an html import is being requested.
                if (path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    Request.Response.Redirect("index.html?start=wizard#!/wizardstart.html");
                    return null;
                }
            }

            var localizationCulture = GetLocalizationCulture();

            // Don't cache if not configured to do so
            // But always cache images to simulate production
            if (!_serverConfigurationManager.Configuration.EnableDashboardResponseCaching &&
                !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                !contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
            {
                var stream = await GetResourceStream(basePath, path, localizationCulture).ConfigureAwait(false);
                return _resultFactory.GetResult(Request, stream, contentType);
            }

            TimeSpan? cacheDuration = null;

            // Cache images unconditionally - updates to image files will require new filename
            // If there's a version number in the query string we can cache this unconditionally
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(request.V))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var cacheKey = (_appHost.ApplicationVersionString + (localizationCulture ?? string.Empty) + path).GetMD5();

            // html gets modified on the fly
            if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase))
            {
                return await _resultFactory.GetStaticResult(Request, cacheKey, null, cacheDuration, contentType, () => GetResourceStream(basePath, path, localizationCulture)).ConfigureAwait(false);
            }

            return await _resultFactory.GetStaticFileResult(Request, _resourceFileManager.GetResourcePath(basePath, path));
        }

        private string GetLocalizationCulture()
        {
            return _serverConfigurationManager.Configuration.UICulture;
        }

        /// <summary>
        /// Gets the resource stream.
        /// </summary>
        private Task<Stream> GetResourceStream(string basePath, string virtualPath, string localizationCulture)
        {
            return GetPackageCreator(basePath)
                .GetResource(virtualPath, null, localizationCulture, _appHost.ApplicationVersionString);
        }

        private PackageCreator GetPackageCreator(string basePath)
        {
            return new PackageCreator(basePath, _resourceFileManager);
        }

        public async Task<object> Get(GetDashboardPackage request)
        {
            var mode = request.Mode;

            var inputPath = string.IsNullOrWhiteSpace(mode) ?
                DashboardUIPath
                : "C:\\dev\\emby-web-mobile-master\\dist";

            var targetPath = !string.IsNullOrWhiteSpace(mode) ?
                inputPath
                : "C:\\dev\\emby-web-mobile\\src";

            var packageCreator = GetPackageCreator(inputPath);

            if (!string.Equals(inputPath, targetPath, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    Directory.Delete(targetPath, true);
                }
                catch (IOException)
                {

                }

                CopyDirectory(inputPath, targetPath);
            }

            var appVersion = _appHost.ApplicationVersionString;

            await DumpHtml(packageCreator, inputPath, targetPath, mode, appVersion);

            return "";
        }

        private async Task DumpHtml(PackageCreator packageCreator, string source, string destination, string mode, string appVersion)
        {
            foreach (var file in _fileSystem.GetFiles(source))
            {
                var filename = file.Name;

                if (!string.Equals(file.Extension, ".html", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                await DumpFile(packageCreator, filename, Path.Combine(destination, filename), mode, appVersion).ConfigureAwait(false);
            }
        }

        private async Task DumpFile(PackageCreator packageCreator, string resourceVirtualPath, string destinationFilePath, string mode, string appVersion)
        {
            using (var stream = await packageCreator.GetResource(resourceVirtualPath, mode, null, appVersion).ConfigureAwait(false))
            using (var fs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await stream.CopyToAsync(fs);
            }
        }

        private void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            //Now Create all of the directories
            foreach (var dirPath in _fileSystem.GetDirectories(source, true))
                Directory.CreateDirectory(dirPath.FullName.Replace(source, destination));

            //Copy all the files & Replaces any files with the same name
            foreach (var newPath in _fileSystem.GetFiles(source, true))
                File.Copy(newPath.FullName, newPath.FullName.Replace(source, destination), true);
        }
    }

}
