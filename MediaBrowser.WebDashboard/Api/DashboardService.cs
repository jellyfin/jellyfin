using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Reflection;
using MediaBrowser.Model.Services;

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

    [Route("/web/Package", "GET")]
    public class GetDashboardPackage
    {
        public string Mode { get; set; }
    }

    [Route("/robots.txt", "GET")]
    public class GetRobotsTxt
    {
    }

    /// <summary>
    /// Class GetDashboardResource
    /// </summary>
    [Route("/web/{ResourceName*}", "GET")]
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
        private readonly ILocalizationManager _localization;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IAssemblyInfo _assemblyInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        public DashboardService(IServerApplicationHost appHost, IServerConfigurationManager serverConfigurationManager, IFileSystem fileSystem, ILocalizationManager localization, IJsonSerializer jsonSerializer, IAssemblyInfo assemblyInfo, ILogger logger, IHttpResultFactory resultFactory)
        {
            _appHost = appHost;
            _serverConfigurationManager = serverConfigurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
            _assemblyInfo = assemblyInfo;
            _logger = logger;
            _resultFactory = resultFactory;
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
                    stream = _assemblyInfo.GetManifestResourceStream(plugin.GetType(), altPage.Item1.EmbeddedResourcePath);
                }
            }

            if (plugin != null && stream != null)
            {
                return _resultFactory.GetStaticResult(Request, plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => GetPackageCreator().ModifyHtml("dummy.html", stream, null, _appHost.ApplicationVersion.ToString(), null, false));
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
            const string unavilableMessage = "The server is still loading. Please try again momentarily.";

            var instance = ServerEntryPoint.Instance;

            if (instance == null)
            {
                throw new InvalidOperationException(unavilableMessage);
            }

            var pages = instance.PluginConfigurationPages;

            if (pages == null)
            {
                throw new InvalidOperationException(unavilableMessage);
            }

            if (request.PageType.HasValue)
            {
                pages = pages.Where(p => p.ConfigurationPageType == request.PageType.Value).ToList();
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
                    _logger.ErrorException("Error getting plugin information from {0}", ex, p.GetType().Name);
                    return null;
                }
            })
                .Where(i => i != null)
                .ToList();

            configPages.AddRange(_appHost.Plugins.SelectMany(GetConfigPages));

            return _resultFactory.GetOptimizedResult(Request, configPages);
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

            path = path.Replace("bower_components" + _appHost.ApplicationVersion, "bower_components", StringComparison.OrdinalIgnoreCase);

            var contentType = MimeTypes.GetMimeType(path);

            // Bounce them to the startup wizard if it hasn't been completed yet
            if (!_serverConfigurationManager.Configuration.IsStartupWizardCompleted && path.IndexOf("wizard", StringComparison.OrdinalIgnoreCase) == -1 && GetPackageCreator().IsCoreHtml(path))
            {
                // But don't redirect if an html import is being requested.
                if (path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    Request.Response.Redirect("wizardstart.html");
                    return null;
                }
            }

            path = path.Replace("scripts/jquery.mobile-1.4.5.min.map", "thirdparty/jquerymobile-1.4.5/jquery.mobile-1.4.5.min.map", StringComparison.OrdinalIgnoreCase);

            var localizationCulture = GetLocalizationCulture();

            // Don't cache if not configured to do so
            // But always cache images to simulate production
            if (!_serverConfigurationManager.Configuration.EnableDashboardResponseCaching &&
                !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                !contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
            {
                var stream = await GetResourceStream(path, localizationCulture).ConfigureAwait(false);
                return _resultFactory.GetResult(stream, contentType);
            }

            TimeSpan? cacheDuration = null;

            // Cache images unconditionally - updates to image files will require new filename
            // If there's a version number in the query string we can cache this unconditionally
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(request.V))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var cacheKey = (_appHost.ApplicationVersion.ToString() + (localizationCulture ?? string.Empty) + path).GetMD5();

            return await _resultFactory.GetStaticResult(Request, cacheKey, null, cacheDuration, contentType, () => GetResourceStream(path, localizationCulture)).ConfigureAwait(false);
        }

        private string GetLocalizationCulture()
        {
            return _serverConfigurationManager.Configuration.UICulture;
        }

        /// <summary>
        /// Gets the resource stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <returns>Task{Stream}.</returns>
        private Task<Stream> GetResourceStream(string path, string localizationCulture)
        {
            var minify = _serverConfigurationManager.Configuration.EnableDashboardResourceMinification;

            return GetPackageCreator()
                .GetResource(path, null, localizationCulture, _appHost.ApplicationVersion.ToString(), minify);
        }

        private PackageCreator GetPackageCreator()
        {
            return new PackageCreator(_fileSystem, _localization, _logger, _serverConfigurationManager, _jsonSerializer);
        }

        private List<string> GetDeployIgnoreExtensions()
        {
            var list = new List<string>();

            list.Add(".log");
            list.Add(".txt");
            list.Add(".map");
            list.Add(".md");
            list.Add(".gz");
            list.Add(".bat");
            list.Add(".sh");

            return list;
        }

        private List<Tuple<string, bool>> GetDeployIgnoreFilenames()
        {
            var list = new List<Tuple<string, bool>>();

            list.Add(new Tuple<string, bool>("copying", true));
            list.Add(new Tuple<string, bool>("license", true));
            list.Add(new Tuple<string, bool>("license-mit", true));
            list.Add(new Tuple<string, bool>("gitignore", false));
            list.Add(new Tuple<string, bool>("npmignore", false));
            list.Add(new Tuple<string, bool>("jshintrc", false));
            list.Add(new Tuple<string, bool>("gruntfile", false));
            list.Add(new Tuple<string, bool>("bowerrc", false));
            list.Add(new Tuple<string, bool>("jscsrc", false));
            list.Add(new Tuple<string, bool>("hero.svg", false));
            list.Add(new Tuple<string, bool>("travis.yml", false));
            list.Add(new Tuple<string, bool>("build.js", false));
            list.Add(new Tuple<string, bool>("editorconfig", false));
            list.Add(new Tuple<string, bool>("gitattributes", false));

            return list;
        }

        public async Task<object> Get(GetDashboardPackage request)
        {
            var mode = request.Mode;

            var path = string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ?
                Path.Combine(_serverConfigurationManager.ApplicationPaths.ProgramDataPath, "webclient-dump")
                : "C:\\dev\\emby-web-mobile\\src";

            try
            {
                _fileSystem.DeleteDirectory(path, true);
            }
            catch (IOException)
            {

            }

            var creator = GetPackageCreator();

            CopyDirectory(creator.DashboardUIPath, path);

            string culture = null;

            var appVersion = _appHost.ApplicationVersion.ToString();

            // Try to trim the output size a bit
            var bowerPath = Path.Combine(path, "bower_components");

            foreach (var ext in GetDeployIgnoreExtensions())
            {
                DeleteFilesByExtension(bowerPath, ext);
            }

            DeleteFilesByExtension(bowerPath, ".json", "strings\\");

            foreach (var ignore in GetDeployIgnoreFilenames())
            {
                DeleteFilesByName(bowerPath, ignore.Item1, ignore.Item2);
            }

            DeleteFoldersByName(bowerPath, "demo");
            DeleteFoldersByName(bowerPath, "test");
            DeleteFoldersByName(bowerPath, "guides");
            DeleteFoldersByName(bowerPath, "grunt");
            DeleteFoldersByName(bowerPath, "rollups");

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                DeleteFoldersByName(Path.Combine(bowerPath, "emby-webcomponents", "fonts"), "montserrat");
                DeleteFoldersByName(Path.Combine(bowerPath, "emby-webcomponents", "fonts"), "opensans");
                DeleteFoldersByName(Path.Combine(bowerPath, "emby-webcomponents", "fonts"), "roboto");
            }

            _fileSystem.DeleteDirectory(Path.Combine(bowerPath, "jquery", "src"), true);

            DeleteCryptoFiles(Path.Combine(bowerPath, "cryptojslib", "components"));

            DeleteFoldersByName(Path.Combine(bowerPath, "jquery"), "src");
            DeleteFoldersByName(Path.Combine(bowerPath, "jstree"), "src");
            //DeleteFoldersByName(Path.Combine(bowerPath, "Sortable"), "meteor");
            //DeleteFoldersByName(Path.Combine(bowerPath, "Sortable"), "st");
            //DeleteFoldersByName(Path.Combine(bowerPath, "Swiper"), "src");

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                // Delete things that are unneeded in an attempt to keep the output as trim as possible
                _fileSystem.DeleteDirectory(Path.Combine(path, "css", "images", "tour"), true);
            }

            await DumpHtml(creator.DashboardUIPath, path, mode, culture, appVersion);

            await DumpFile("css/all.css", Path.Combine(path, "css", "all.css"), mode, culture, appVersion).ConfigureAwait(false);

            return "";
        }

        private void DeleteCryptoFiles(string path)
        {
            var files = _fileSystem.GetFiles(path)
                .ToList();

            var keepFiles = new[] { "core-min.js", "md5-min.js", "sha1-min.js" };

            foreach (var file in files)
            {
                if (!keepFiles.Contains(file.Name, StringComparer.OrdinalIgnoreCase))
                {
                    _fileSystem.DeleteFile(file.FullName);
                }
            }
        }

        private void DeleteFilesByExtension(string path, string extension, string exclude = null)
        {
            var files = _fileSystem.GetFiles(path, true)
                .Where(i => string.Equals(i.Extension, extension, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in files)
            {
                if (!string.IsNullOrWhiteSpace(exclude))
                {
                    if (file.FullName.IndexOf(exclude, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        continue;
                    }
                }
                _fileSystem.DeleteFile(file.FullName);
            }
        }

        private void DeleteFilesByName(string path, string name, bool exact = false)
        {
            var files = _fileSystem.GetFiles(path, true)
                .Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase) || (!exact && i.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1))
                .ToList();

            foreach (var file in files)
            {
                _fileSystem.DeleteFile(file.FullName);
            }
        }

        private void DeleteFoldersByName(string path, string name)
        {
            var directories = _fileSystem.GetDirectories(path, true)
                .Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var directory in directories)
            {
                _fileSystem.DeleteDirectory(directory.FullName, true);
            }
        }

        private async Task DumpHtml(string source, string destination, string mode, string culture, string appVersion)
        {
            foreach (var file in _fileSystem.GetFiles(source))
            {
                var filename = file.Name;

                await DumpFile(filename, Path.Combine(destination, filename), mode, culture, appVersion).ConfigureAwait(false);
            }
        }

        private async Task DumpFile(string resourceVirtualPath, string destinationFilePath, string mode, string culture, string appVersion)
        {
            using (var stream = await GetPackageCreator().GetResource(resourceVirtualPath, mode, culture, appVersion, false).ConfigureAwait(false))
            {
                using (var fs = _fileSystem.GetFileStream(destinationFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        private void CopyDirectory(string source, string destination)
        {
            _fileSystem.CreateDirectory(destination);

            //Now Create all of the directories
            foreach (var dirPath in _fileSystem.GetDirectories(source, true))
                _fileSystem.CreateDirectory(dirPath.FullName.Replace(source, destination));

            //Copy all the files & Replaces any files with the same name
            foreach (var newPath in _fileSystem.GetFiles(source, true))
                _fileSystem.CopyFile(newPath.FullName, newPath.FullName.Replace(source, destination), true);
        }
    }

}
