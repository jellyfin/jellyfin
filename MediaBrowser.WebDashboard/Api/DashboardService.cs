using System.Reflection;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Server.Implementations.HttpServer;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediaBrowser.WebDashboard.Api
{
    /// <summary>
    /// Class GetDashboardConfigurationPages
    /// </summary>
    [Route("/dashboard/ConfigurationPages", "GET")]
    public class GetDashboardConfigurationPages : IReturn<List<IPluginConfigurationPage>>
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
    public class GetDashboardConfigurationPage : IReturn<IPluginConfigurationPage>
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
    /// Class GetDashboardInfo
    /// </summary>
    [Route("/dashboard/dashboardInfo", "GET")]
    public class GetDashboardInfo : IReturn<DashboardInfo>
    {
    }

    /// <summary>
    /// Class DashboardService
    /// </summary>
    [Export(typeof(IRestfulService))]
    public class DashboardService : BaseRestService
    {
        /// <summary>
        /// Gets or sets the task manager.
        /// </summary>
        /// <value>The task manager.</value>
        private readonly ITaskManager _taskManager;

        /// <summary>
        /// The _user manager
        /// </summary>
        private readonly IUserManager _userManager;

        private readonly IServerApplicationHost _appHost;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardService" /> class.
        /// </summary>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        public DashboardService(ITaskManager taskManager, IUserManager userManager, IServerApplicationHost appHost, ILibraryManager libraryManager)
        {
            _taskManager = taskManager;
            _userManager = userManager;
            _appHost = appHost;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardInfo request)
        {
            return GetDashboardInfo(_appHost, Logger, _taskManager, _userManager, _libraryManager).Result;
        }

        /// <summary>
        /// Gets the dashboard info.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>DashboardInfo.</returns>
        public static async Task<DashboardInfo> GetDashboardInfo(IServerApplicationHost appHost, ILogger logger, ITaskManager taskManager, IUserManager userManager, ILibraryManager libraryManager)
        {
            var connections = userManager.ConnectedUsers.ToArray();

            var dtoBuilder = new DtoBuilder(logger, libraryManager);

            var tasks = userManager.Users.Where(u => connections.Any(c => c.UserId == u.Id)).Select(dtoBuilder.GetUserDto);
            var users = await Task.WhenAll(tasks).ConfigureAwait(false);

            return new DashboardInfo
            {
                SystemInfo = appHost.GetSystemInfo(),

                RunningTasks = taskManager.ScheduledTasks.Where(i => i.State == TaskState.Running || i.State == TaskState.Cancelling)
                                     .Select(ScheduledTaskHelpers.GetTaskInfo)
                                     .ToArray(),

                ApplicationUpdateTaskId = taskManager.ScheduledTasks.First(t => t.ScheduledTask.GetType().Name.Equals("SystemUpdateTask", StringComparison.OrdinalIgnoreCase)).Id,

                ActiveConnections = connections,

                Users = users
            };
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardConfigurationPage request)
        {
            var page = ServerEntryPoint.Instance.PluginConfigurationPages.First(p => p.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase));

            return ToStaticResult(page.Plugin.Version.ToString().GetMD5(), page.Plugin.AssemblyDateLastModified, null, MimeTypes.GetMimeType("page.html"), () => ModifyHtml(page.GetHtmlStream()));
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetDashboardConfigurationPages request)
        {
            var pages = ServerEntryPoint.Instance.PluginConfigurationPages;

            if (request.PageType.HasValue)
            {
                pages = pages.Where(p => p.ConfigurationPageType == request.PageType.Value);
            }

            return ToOptimizedResult(pages.ToList());
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

            TimeSpan? cacheDuration = null;

            // Cache images unconditionally - updates to image files will require new filename
            // If there's a version number in the query string we can cache this unconditionally
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(request.V))
            {
                cacheDuration = TimeSpan.FromDays(365);
            }

            var assembly = GetType().Assembly.GetName();

            return ToStaticResult(assembly.Version.ToString().GetMD5(), null, cacheDuration, contentType, () => GetResourceStream(path));
        }

        /// <summary>
        /// Gets the resource stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetResourceStream(string path)
        {
            Stream resourceStream;

            if (path.Equals("scripts/all.js", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllJavascript().ConfigureAwait(false);
            }
            else
            {
                resourceStream = GetType().Assembly.GetManifestResourceStream("MediaBrowser.WebDashboard.Html." + ConvertUrlToResourcePath(path));
            }

            if (resourceStream != null)
            {
                var isHtml = IsHtml(path);

                // Don't apply any caching for html pages
                // jQuery ajax doesn't seem to handle if-modified-since correctly
                if (isHtml)
                {
                    resourceStream = await ModifyHtml(resourceStream).ConfigureAwait(false);
                }
            }

            return resourceStream;
        }

        /// <summary>
        /// Redirects the specified CTX.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <param name="url">The URL.</param>
        private void Redirect(HttpListenerContext ctx, string url)
        {
            // Try to prevent the browser from caching the redirect response (the right way)
            ctx.Response.Headers[HttpResponseHeader.CacheControl] = "no-cache, no-store, must-revalidate";
            ctx.Response.Headers[HttpResponseHeader.Pragma] = "no-cache, no-store, must-revalidate";
            ctx.Response.Headers[HttpResponseHeader.Expires] = "-1";

            ctx.Response.Redirect(url);
            ctx.Response.Close();
        }

        /// <summary>
        /// Preserves the current query string when redirecting
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="newUrl">The new URL.</param>
        /// <returns>System.String.</returns>
        private string GetRedirectUrl(HttpListenerRequest request, string newUrl)
        {
            var query = request.Url.Query;

            return string.IsNullOrEmpty(query) ? newUrl : newUrl + query;
        }

        /// <summary>
        /// Converts the URL to a manifest resource path.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        private string ConvertUrlToResourcePath(string url)
        {
            var parts = url.Split('/');
            var normalizedParts = new string[parts.Length];

            for (var i = 0; i < parts.Length; i++)
            {
                // We have to do some tricky string replacements for all parts of the path except the last
                if (i < parts.Length - 1)
                {
                    // Find the index of the first period as well as the first dash
                    var periodIndex = parts[i].IndexOf('.');
                    var slashIndex = parts[i].IndexOf('-');

                    // Replace all periods with "._" and dashes with "_"
                    normalizedParts[i] = parts[i].Replace(".", "._").Replace("-", "_");

                    // If the first period occurred before the first slash, change it back from "._" to just "."
                    if (periodIndex < slashIndex)
                    {
                        var regex = new Regex("\\._");
                        normalizedParts[i] = regex.Replace(normalizedParts[i], ".", 1);
                    }
                }
                else
                {
                    normalizedParts[i] = parts[i];
                }
            }

            return string.Join(".", normalizedParts);
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
        /// <returns>Task{Stream}.</returns>
        internal async Task<Stream> ModifyHtml(Stream sourceStream)
        {
            string html;

            using (var memoryStream = new MemoryStream())
            {
                await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                html = Encoding.UTF8.GetString(memoryStream.ToArray());
            }

            var version = GetType().Assembly.GetName().Version;

            html = html.Replace("<head>", "<head>" + GetMetaTags() + GetCommonCss(version) + GetCommonJavascript(version));

            var bytes = Encoding.UTF8.GetBytes(html);

            sourceStream.Dispose();

            return new MemoryStream(bytes);
        }

        /// <summary>
        /// Gets the meta tags.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetMetaTags()
        {
            var sb = new StringBuilder();

            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, user-scalable=no\">");
            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"css/images/touchicon.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"css/images/touchicon72.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"css/images/touchicon114.png\" />");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\">");

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
                                "http://code.jquery.com/mobile/1.3.0/jquery.mobile-1.3.0.min.css",
                                "thirdparty/jqm-icon-pack-3.0/font-awesome/jqm-icon-pack-3.0.0-fa.css",
                                "css/site.css" + versionString
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
            var versionString = "?v=" + version;

            var files = new[]
                            {
                                "http://ajax.googleapis.com/ajax/libs/jquery/1.8.3/jquery.min.js", 
                                "http://code.jquery.com/mobile/1.3.0/jquery.mobile-1.3.0.min.js",
                                "scripts/all.js" + versionString
            };

            var tags = files.Select(s => string.Format("<script src=\"{0}\"></script>", s)).ToArray();

            return string.Join(string.Empty, tags);
        }

        /// <summary>
        /// Gets a stream containing all concatenated javascript
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetAllJavascript()
        {
            const string resourcePrefix = "MediaBrowser.WebDashboard.Html.scripts.";
            var assembly = GetType().Assembly;

            var scriptFiles = new[]
                                  {
                                      "Extensions.js",
                                      "Site.js",
                                      "AboutPage.js",
                                      "AddPluginPage.js",
                                      "AdvancedConfigurationPage.js",
                                      "AdvancedMetadataConfigurationPage.js",
                                      "PluginCatalogPage.js",
                                      "DashboardPage.js",
                                      "DisplaySettingsPage.js",
                                      "EditUserPage.js",
                                      "IndexPage.js",
                                      "ItemDetailPage.js",
                                      "ItemListPage.js",
                                      "LoginPage.js",
                                      "LogPage.js",
                                      "MediaLibraryPage.js",
                                      "MediaPlayer.js",
                                      "MetadataConfigurationPage.js",
                                      "MetadataImagesPage.js",
                                      "PluginsPage.js",
                                      "PluginUpdatesPage.js",
                                      "ScheduledTaskPage.js",
                                      "ScheduledTasksPage.js",
                                      "UpdatePasswordPage.js",
                                      "UserImagePage.js",
                                      "UserProfilesPage.js",
                                      "WizardFinishPage.js",
                                      "WizardStartPage.js",
                                      "WizardUserPage.js",
                                      "SupporterKeyPage.js",
                                      "SupporterPage.js"
                                  };

            var memoryStream = new MemoryStream();

            var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

            await AppendResource(assembly, memoryStream, "MediaBrowser.WebDashboard.ApiClient.js", newLineBytes).ConfigureAwait(false);

            foreach (var file in scriptFiles)
            {
                await AppendResource(assembly, memoryStream, resourcePrefix + file, newLineBytes).ConfigureAwait(false);
            }

            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task AppendResource(Assembly assembly, Stream outputStream, string path, byte[] newLineBytes)
        {
            using (var stream = assembly.GetManifestResourceStream(path))
            {
                await stream.CopyToAsync(outputStream).ConfigureAwait(false);

                await outputStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);
            }
        }

    }

}
