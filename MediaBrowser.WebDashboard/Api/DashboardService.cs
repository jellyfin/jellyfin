using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonIO;
using WebMarkupMin.Core.Minifiers;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class GetDashboardConfigurationPages
    /// </summary>
    [Route("/dashboard/ConfigurationPages", "GET")]
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
    [Route("/dashboard/ConfigurationPage", "GET")]
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
    [Route("/dashboard/Package", "GET")]
    public class GetDashboardPackage
    {
        public string Mode { get; set; }
    }

    /// <summary>
    /// Class GetDashboardResource
    /// </summary>
    [Route("/web/{ResourceName*}", "GET")]
    [Route("/dashboard/{ResourceName*}", "GET")]
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
    public class DashboardService : IRestfulService, IHasResultFactory
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the HTTP result factory.
        /// </summary>
        /// <value>The HTTP result factory.</value>
        public IHttpResultFactory ResultFactory { get; set; }

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

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        /// <param name="serverConfigurationManager">The server configuration manager.</param>
        /// <param name="fileSystem">The file system.</param>
        public DashboardService(IServerApplicationHost appHost, IServerConfigurationManager serverConfigurationManager, IFileSystem fileSystem, ILocalizationManager localization, IJsonSerializer jsonSerializer)
        {
            _appHost = appHost;
            _serverConfigurationManager = serverConfigurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardConfigurationPage request)
        {
            var page = ServerEntryPoint.Instance.PluginConfigurationPages.First(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

            return ResultFactory.GetStaticResult(Request, page.Plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => GetPackageCreator().ModifyHtml(page.GetHtmlStream(), null, _appHost.ApplicationVersion.ToString(), null, false));
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
                pages = pages.Where(p => p.ConfigurationPageType == request.PageType.Value);
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
                    Logger.ErrorException("Error getting plugin information from {0}", ex, p.GetType().Name);
                    return null;
                }
            })
                .Where(i => i != null)
                .ToList();

            return ResultFactory.GetOptimizedResult(Request, configPages);
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardResource request)
        {
            var path = request.ResourceName;

            var contentType = MimeTypes.GetMimeType(path);

            // Bounce them to the startup wizard if it hasn't been completed yet
            if (!_serverConfigurationManager.Configuration.IsStartupWizardCompleted && path.IndexOf("wizard", StringComparison.OrdinalIgnoreCase) == -1 && GetPackageCreator().IsCoreHtml(path))
            {
                // But don't redirect if an html import is being requested.
                if (path.IndexOf("vulcanize", StringComparison.OrdinalIgnoreCase) == -1 && path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
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
                return ResultFactory.GetResult(GetResourceStream(path, localizationCulture).Result, contentType);
            }

            TimeSpan? cacheDuration = null;

            // Cache images unconditionally - updates to image files will require new filename
            // If there's a version number in the query string we can cache this unconditionally
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(request.V))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var assembly = GetType().Assembly.GetName();

            var cacheKey = (assembly.Version + (localizationCulture ?? string.Empty) + path).GetMD5();

            return ResultFactory.GetStaticResult(Request, cacheKey, null, cacheDuration, contentType, () => GetResourceStream(path, localizationCulture));
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
            return new PackageCreator(_fileSystem, _localization, Logger, _serverConfigurationManager, _jsonSerializer);
        }

        /// <summary>
        /// Determines whether the specified path is HTML.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the specified path is HTML; otherwise, <c>false</c>.</returns>
        private bool IsHtml(string path)
        {
            return Path.GetExtension(path).EndsWith("html", StringComparison.OrdinalIgnoreCase);
        }

        private void CopyFile(string src, string dst)
        {
			_fileSystem.CreateDirectory(Path.GetDirectoryName(dst));
			_fileSystem.CopyFile(src, dst, true);
        }

        public async Task<object> Get(GetDashboardPackage request)
        {
            var path = Path.Combine(_serverConfigurationManager.ApplicationPaths.ProgramDataPath,
                "webclient-dump");

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

            var mode = request.Mode;

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                // Overwrite certain files with cordova specific versions
                var cordovaVersion = Path.Combine(path, "cordova", "registrationservices.js");
				_fileSystem.CopyFile(cordovaVersion, Path.Combine(path, "scripts", "registrationservices.js"), true);
				_fileSystem.DeleteFile(cordovaVersion);

                // Delete things that are unneeded in an attempt to keep the output as trim as possible
				_fileSystem.DeleteDirectory(Path.Combine(path, "css", "images", "tour"), true);
				_fileSystem.DeleteDirectory(Path.Combine(path, "apiclient", "alt"), true);

				_fileSystem.DeleteFile(Path.Combine(path, "thirdparty", "jquerymobile-1.4.5", "jquery.mobile-1.4.5.min.map"));

				_fileSystem.DeleteDirectory(Path.Combine(path, "bower_components"), true);
				_fileSystem.DeleteDirectory(Path.Combine(path, "thirdparty", "viblast"), true);

                // But we do need this
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "webcomponentsjs", "webcomponents-lite.js"), Path.Combine(path, "bower_components", "webcomponentsjs", "webcomponents-lite.js"));
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "webcomponentsjs", "webcomponents-lite.min.js"), Path.Combine(path, "bower_components", "webcomponentsjs", "webcomponents-lite.min.js"));
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "velocity", "velocity.min.js"), Path.Combine(path, "bower_components", "velocity", "velocity.min.js"));
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "requirejs", "require.js"), Path.Combine(path, "bower_components", "requirejs", "require.js"));
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "fastclick", "lib", "fastclick.js"), Path.Combine(path, "bower_components", "fastclick", "lib", "fastclick.js"));
                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "jquery", "dist", "jquery.min.js"), Path.Combine(path, "bower_components", "jquery", "dist", "jquery.min.js"));

                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "jstree", "dist", "jstree.min.js"), Path.Combine(path, "bower_components", "jstree", "dist", "jstree.min.js"));
                
                CopyDirectory(Path.Combine(creator.DashboardUIPath, "bower_components", "swipebox", "src", "css"), Path.Combine(path, "bower_components", "swipebox", "src", "css"));
                CopyDirectory(Path.Combine(creator.DashboardUIPath, "bower_components", "swipebox", "src", "js"), Path.Combine(path, "bower_components", "swipebox", "src", "js"));
                CopyDirectory(Path.Combine(creator.DashboardUIPath, "bower_components", "swipebox", "src", "img"), Path.Combine(path, "bower_components", "swipebox", "src", "img"));

                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "hammerjs", "hammer.min.js"), Path.Combine(path, "bower_components", "hammerjs", "hammer.min.js"));

                CopyFile(Path.Combine(creator.DashboardUIPath, "bower_components", "Sortable", "Sortable.min.js"), Path.Combine(path, "bower_components", "Sortable", "Sortable.min.js"));
            }
            
            MinifyCssDirectory(Path.Combine(path, "css"));
            MinifyJsDirectory(Path.Combine(path, "scripts"));
            MinifyJsDirectory(Path.Combine(path, "apiclient"));
            MinifyJsDirectory(Path.Combine(path, "voice"));

            await DumpHtml(creator.DashboardUIPath, path, mode, culture, appVersion);
            await DumpJs(creator.DashboardUIPath, path, mode, culture, appVersion);

            await DumpFile("scripts/all.js", Path.Combine(path, "scripts", "all.js"), mode, culture, appVersion).ConfigureAwait(false);
            await DumpFile("css/all.css", Path.Combine(path, "css", "all.css"), mode, culture, appVersion).ConfigureAwait(false);

            return "";
        }

        private void MinifyCssDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.css", SearchOption.AllDirectories))
            {
                try
                {
					var text = _fileSystem.ReadAllText(file, Encoding.UTF8);

                    var result = new KristensenCssMinifier().Minify(text, false, Encoding.UTF8);

                    if (result.Errors.Count > 0)
                    {
                        Logger.Error("Error minifying css: " + result.Errors[0].Message);
                    }
                    else
                    {
                        text = result.MinifiedContent;
						_fileSystem.WriteAllText(file, text, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error minifying css", ex);
                }
            }
        }

        private void MinifyJsDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.js", SearchOption.AllDirectories))
            {
                try
                {
					var text = _fileSystem.ReadAllText(file, Encoding.UTF8);

                    var result = new CrockfordJsMinifier().Minify(text, false, Encoding.UTF8);

                    if (result.Errors.Count > 0)
                    {
                        Logger.Error("Error minifying javascript: " + result.Errors[0].Message);
                    }
                    else
                    {
                        text = result.MinifiedContent;
						_fileSystem.WriteAllText(file, text, Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error minifying css", ex);
                }
            }
        }

        private async Task DumpHtml(string source, string destination, string mode, string culture, string appVersion)
        {
            foreach (var file in Directory.GetFiles(source, "*.html", SearchOption.TopDirectoryOnly))
            {
                var filename = Path.GetFileName(file);

                await DumpFile(filename, Path.Combine(destination, filename), mode, culture, appVersion).ConfigureAwait(false);
            }

            var excludeFiles = new List<string>();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                excludeFiles.Add("supporterkey.html");
            }

            foreach (var file in excludeFiles)
            {
				_fileSystem.DeleteFile(Path.Combine(destination, file));
            }
        }

        private async Task DumpJs(string source, string mode, string destination, string culture, string appVersion)
        {
            foreach (var file in Directory.GetFiles(source, "*.js", SearchOption.TopDirectoryOnly))
            {
                var filename = Path.GetFileName(file);

                await DumpFile("scripts/" + filename, Path.Combine(destination, "scripts", filename), mode, culture, appVersion).ConfigureAwait(false);
            }
        }

        private async Task DumpFile(string resourceVirtualPath, string destinationFilePath, string mode, string culture, string appVersion)
        {
            using (var stream = await GetPackageCreator().GetResource(resourceVirtualPath, mode, culture, appVersion, true).ConfigureAwait(false))
            {
                using (var fs = _fileSystem.GetFileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        private void CopyDirectory(string source, string destination)
        {
			_fileSystem.CreateDirectory(destination);

            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(source, "*",
                SearchOption.AllDirectories))
				_fileSystem.CreateDirectory(dirPath.Replace(source, destination));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(source, "*.*",
                SearchOption.AllDirectories))
				_fileSystem.CopyFile(newPath, newPath.Replace(source, destination), true);
        }
    }

}
