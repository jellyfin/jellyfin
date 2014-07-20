using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

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
        /// Gets the dashboard UI path.
        /// </summary>
        /// <value>The dashboard UI path.</value>
        public string DashboardUIPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_serverConfigurationManager.Configuration.DashboardSourcePath))
                {
                    return _serverConfigurationManager.Configuration.DashboardSourcePath;
                }

                var runningDirectory = Path.GetDirectoryName(_serverConfigurationManager.ApplicationPaths.ApplicationPath);

                return Path.Combine(runningDirectory, "dashboard-ui");
            }
        }

        /// <summary>
        /// Gets the dashboard resource path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns>System.String.</returns>
        private string GetDashboardResourcePath(string virtualPath)
        {
            return Path.Combine(DashboardUIPath, virtualPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardConfigurationPage request)
        {
            var page = ServerEntryPoint.Instance.PluginConfigurationPages.First(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

            return ResultFactory.GetStaticResult(Request, page.Plugin.Version.ToString().GetMD5(), null, null, MimeTypes.GetMimeType("page.html"), () => ModifyHtml(page.GetHtmlStream(), null));
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

            var isHtml = IsHtml(path);
            var localizationCulture = GetLocalizationCulture();

            // Don't cache if not configured to do so
            // But always cache images to simulate production
            if (!_serverConfigurationManager.Configuration.EnableDashboardResponseCaching &&
                !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) &&
                !contentType.StartsWith("font/", StringComparison.OrdinalIgnoreCase))
            {
                return ResultFactory.GetResult(GetResourceStream(path, isHtml, localizationCulture).Result, contentType);
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

            return ResultFactory.GetStaticResult(Request, cacheKey, null, cacheDuration, contentType, () => GetResourceStream(path, isHtml, localizationCulture));
        }

        private string GetLocalizationCulture()
        {
            return _serverConfigurationManager.Configuration.UICulture;
        }

        /// <summary>
        /// Gets the resource stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isHtml">if set to <c>true</c> [is HTML].</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetResourceStream(string path, bool isHtml, string localizationCulture)
        {
            Stream resourceStream;

            if (path.Equals("scripts/all.js", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllJavascript().ConfigureAwait(false);
            }
            else if (path.Equals("css/all.css", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllCss().ConfigureAwait(false);
            }
            else
            {
                resourceStream = GetRawResourceStream(path);
            }

            if (resourceStream != null)
            {
                // Don't apply any caching for html pages
                // jQuery ajax doesn't seem to handle if-modified-since correctly
                if (isHtml)
                {
                    resourceStream = await ModifyHtml(resourceStream, localizationCulture).ConfigureAwait(false);
                }
            }

            return resourceStream;
        }

        /// <summary>
        /// Gets the raw resource stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Task{Stream}.</returns>
        private Stream GetRawResourceStream(string path)
        {
            return _fileSystem.GetFileStream(GetDashboardResourcePath(path), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true);
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

        /// <summary>
        /// Modifies the HTML by adding common meta tags, css and js.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> ModifyHtml(Stream sourceStream, string localizationCulture)
        {
            using (sourceStream)
            {
                string html;

                using (var memoryStream = new MemoryStream())
                {
                    await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    html = Encoding.UTF8.GetString(memoryStream.ToArray());

                    if (!string.IsNullOrWhiteSpace(localizationCulture))
                    {
                        var lang = localizationCulture.Split('-').FirstOrDefault();

                        html = _localization.LocalizeDocument(html, localizationCulture, GetLocalizationToken);

                        html = html.Replace("<html>", "<html lang=\"" + lang + "\">");
                    }

                    //try
                    //{
                    //    var minifier = new HtmlMinifier(new HtmlMinificationSettings(true));

                    //    html = minifier.Minify(html).MinifiedContent;
                    //}
                    //catch (Exception ex)
                    //{
                    //    Logger.ErrorException("Error minifying html", ex);
                    //}
                }

                var version = GetType().Assembly.GetName().Version;

                html = html.Replace("<head>", "<head>" + GetMetaTags() + GetCommonCss(version) + GetCommonJavascript(version));

                var bytes = Encoding.UTF8.GetBytes(html);

                return new MemoryStream(bytes);
            }
        }

        private string GetLocalizationToken(string phrase)
        {
            return "${" + phrase + "}";
        }

        /// <summary>
        /// Gets the meta tags.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetMetaTags()
        {
            var sb = new StringBuilder();

            sb.Append("<meta http-equiv=\"X-UA-Compatibility\" content=\"IE=Edge\">");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, user-scalable=no\">");
            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"application-name\" content=\"Media Browser\">");
            //sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            sb.Append("<link rel=\"icon\" sizes=\"114x114\" href=\"css/images/touchicon114.png\" />");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"css/images/touchicon.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"css/images/touchicon72.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"css/images/touchicon114.png\" />");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\" />");
            sb.Append("<link rel=\"shortcut icon\" href=\"css/images/favicon.ico\" />");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the common CSS.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private static string GetCommonCss(Version version)
        {
            var versionString = "?v=" + version;

            var files = new[]
                            {
                                "thirdparty/jquerymobile-1.4.3/jquery.mobile-1.4.3.min.css",
                                "css/all.css" + versionString
                            };

            var tags = files.Select(s => string.Format("<link rel=\"stylesheet\" href=\"{0}\" />", s)).ToArray();

            return string.Join(string.Empty, tags);
        }

        /// <summary>
        /// Gets the common javascript.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private static string GetCommonJavascript(Version version)
        {
            var builder = new StringBuilder();

            var versionString = "?v=" + version;

            var files = new[]
                            {
                                "scripts/all.js" + versionString,
                                "thirdparty/jstree1.0/jquery.jstree.min.js"
            };

            var tags = files.Select(s => string.Format("<script src=\"{0}\"></script>", s)).ToArray();

            builder.Append(string.Join(string.Empty, tags));

            return builder.ToString();
        }

        /// <summary>
        /// Gets a stream containing all concatenated javascript
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetAllJavascript()
        {
            var memoryStream = new MemoryStream();
            var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

            // jQuery + jQuery mobile
            await AppendResource(memoryStream, "thirdparty/jquery-2.1.1.min.js", newLineBytes).ConfigureAwait(false);
            await AppendResource(memoryStream, "thirdparty/jquerymobile-1.4.3/jquery.mobile-1.4.3.min.js", newLineBytes).ConfigureAwait(false);

            await AppendResource(memoryStream, "thirdparty/jquery.unveil-custom.js", newLineBytes).ConfigureAwait(false);
            await AppendResource(memoryStream, "thirdparty/cast_sender.js", newLineBytes).ConfigureAwait(false);
            
            await AppendLocalization(memoryStream).ConfigureAwait(false);
            await memoryStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);

            // Write the version string for the dashboard comparison function
            var versionString = string.Format("window.dashboardVersion='{0}';", _appHost.ApplicationVersion);
            var versionBytes = Encoding.UTF8.GetBytes(versionString);

            await memoryStream.WriteAsync(versionBytes, 0, versionBytes.Length).ConfigureAwait(false);
            await memoryStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);

            var builder = new StringBuilder();

            using (var fs = _fileSystem.GetFileStream(GetDashboardResourcePath("thirdparty/mediabrowser.apiclient.js"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                using (var streamReader = new StreamReader(fs))
                {
                    var text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    builder.Append(text);
                    builder.Append(Environment.NewLine);
                }
            }

            foreach (var file in GetScriptFiles())
            {
                var path = GetDashboardResourcePath("scripts/" + file);

                using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                {
                    using (var streamReader = new StreamReader(fs))
                    {
                        var text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        builder.Append(text);
                        builder.Append(Environment.NewLine);
                    }
                }
            }

            var js = builder.ToString();

            try
            {
                var result = new CrockfordJsMinifier().Minify(js, false, Encoding.UTF8);

                js = result.MinifiedContent;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error minifying javascript", ex);
            }

            var bytes = Encoding.UTF8.GetBytes(js);
            await memoryStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            memoryStream.Position = 0;
            return memoryStream;
        }

        private IEnumerable<string> GetScriptFiles()
        {
            return new[]
                            {
                                "extensions.js",
                                "site.js",
                                "librarybrowser.js",
                                "librarylist.js",
                                "editorsidebar.js",
                                "librarymenu.js",
                                "mediacontroller.js",
                                "chromecast.js",
                                "backdrops.js",
                                "sync.js",

                                "mediaplayer.js",
                                "mediaplayer-video.js",
                                "nowplayingbar.js",
                                "nowplayingpage.js",

                                "ratingdialog.js",
                                "aboutpage.js",
                                "alphapicker.js",
                                "addpluginpage.js",
                                "advancedconfigurationpage.js",
                                "metadataadvanced.js",
                                "appsplayback.js",
                                "autoorganizetv.js",
                                "autoorganizelog.js",
                                "channels.js",
                                "channelslatest.js",
                                "channelitems.js",
                                "channelsettings.js",
                                "dashboardgeneral.js",
                                "dashboardpage.js",
                                "directorybrowser.js",
                                "dlnaprofile.js",
                                "dlnaprofiles.js",
                                "dlnasettings.js",
                                "dlnaserversettings.js",
                                "editcollectionitems.js",
                                "edititemmetadata.js",
                                "edititemimages.js",
                                "edititemsubtitles.js",
                                "encodingsettings.js",
                                "favorites.js",
                                "gamesrecommendedpage.js",
                                "gamesystemspage.js",
                                "gamespage.js",
                                "gamegenrepage.js",
                                "gamestudiospage.js",
                                "homelatest.js",
                                "indexpage.js",
                                "itembynamedetailpage.js",
                                "itemdetailpage.js",
                                "itemgallery.js",
                                "itemlistpage.js",
                                "librarypathmapping.js",
                                "reports.js",
                                "librarysettings.js",
                                "livetvchannel.js",
                                "livetvchannels.js",
                                "livetvguide.js",
                                "livetvnewrecording.js",
                                "livetvprogram.js",
                                "livetvrecording.js",
                                "livetvrecordinglist.js",
                                "livetvrecordings.js",
                                "livetvtimer.js",
                                "livetvseriestimer.js",
                                "livetvseriestimers.js",
                                "livetvsettings.js",
                                "livetvsuggested.js",
                                "livetvstatus.js",
                                "livetvtimers.js",

                                "loginpage.js",
                                "logpage.js",
                                "medialibrarypage.js",
                                "metadataconfigurationpage.js",
                                "metadataimagespage.js",
                                "metadatasubtitles.js",
                                "metadatachapters.js",
                                "metadataxbmc.js",
                                "moviegenres.js",
                                "moviecollections.js",
                                "movies.js",
                                "movieslatest.js",
                                "moviepeople.js",
                                "moviesrecommended.js",
                                "moviestudios.js",
                                "movietrailers.js",
                                "musicalbums.js",
                                "musicalbumartists.js",
                                "musicartists.js",
                                "musicgenres.js",
                                "musicrecommended.js",
                                "musicvideos.js",

                                "mypreferencesdisplay.js",
                                "mypreferenceslanguages.js",
                                "mypreferenceswebclient.js",

                                "notifications.js",
                                "notificationlist.js",
                                "notificationsetting.js",
                                "notificationsettings.js",
                                "playlist.js",
                                "plugincatalogpage.js",
                                "pluginspage.js",
                                "remotecontrol.js",
                                "scheduledtaskpage.js",
                                "scheduledtaskspage.js",
                                "search.js",
                                "serversecurity.js",
                                "songs.js",
                                "supporterkeypage.js",
                                "supporterpage.js",
                                "episodes.js",
                                "thememediaplayer.js",
                                "tvgenres.js",
                                "tvlatest.js",
                                "tvpeople.js",
                                "tvrecommended.js",
                                "tvshows.js",
                                "tvstudios.js",
                                "tvupcoming.js",
                                "useredit.js",
                                "userpassword.js",
                                "userimagepage.js",
                                "userprofilespage.js",
                                "userparentalcontrol.js",
                                "wizardfinishpage.js",
                                "wizardservice.js",
                                "wizardstartpage.js",
                                "wizardsettings.js",
                                "wizarduserpage.js"
                            };
        }

        private async Task AppendLocalization(Stream stream)
        {
            var js = "window.localizationGlossary=" + _jsonSerializer.SerializeToString(_localization.GetJavaScriptLocalizationDictionary(GetLocalizationCulture()));

            var bytes = Encoding.UTF8.GetBytes(js);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all CSS.
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetAllCss()
        {
            var files = new[]
                                  {
                                      "site.css",
                                      "chromecast.css",
                                      "mediaplayer.css",
                                      "mediaplayer-video.css",
                                      "librarymenu.css",
                                      "librarybrowser.css",
                                      "detailtable.css",
                                      "posteritem.css",
                                      "tileitem.css",
                                      "metadataeditor.css",
                                      "notifications.css",
                                      "search.css",
                                      "pluginupdates.css",
                                      "remotecontrol.css",
                                      "userimage.css",
                                      "livetv.css",
                                      "nowplaying.css",
                                      "icons.css"
                                  };

            var builder = new StringBuilder();

            foreach (var file in files)
            {
                var path = GetDashboardResourcePath("css/" + file);

                using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                {
                    using (var streamReader = new StreamReader(fs))
                    {
                        var text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        builder.Append(text);
                        builder.Append(Environment.NewLine);
                    }
                }
            }

            var css = builder.ToString();

            //try
            //{
            //    var result = new KristensenCssMinifier().Minify(builder.ToString(), false, Encoding.UTF8);

            //    css = result.MinifiedContent;
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorException("Error minifying css", ex);
            //}

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(css));

            memoryStream.Position = 0;
            return memoryStream;
        }

        /// <summary>
        /// Appends the resource.
        /// </summary>
        /// <param name="outputStream">The output stream.</param>
        /// <param name="path">The path.</param>
        /// <param name="newLineBytes">The new line bytes.</param>
        /// <returns>Task.</returns>
        private async Task AppendResource(Stream outputStream, string path, byte[] newLineBytes)
        {
            path = GetDashboardResourcePath(path);

            using (var fs = _fileSystem.GetFileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
            {
                using (var streamReader = new StreamReader(fs))
                {
                    var text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    var bytes = Encoding.UTF8.GetBytes(text);
                    await outputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }

            await outputStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);
        }
    }

}
