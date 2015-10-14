using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Controller.Net;
using WebMarkupMin.Core;
using WebMarkupMin.Core.Minifiers;
using WebMarkupMin.Core.Settings;

namespace MediaBrowser.WebDashboard.Api
{
    public class PackageCreator
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;

        public PackageCreator(IFileSystem fileSystem, ILocalizationManager localization, ILogger logger, IServerConfigurationManager config, IJsonSerializer jsonSerializer)
        {
            _fileSystem = fileSystem;
            _localization = localization;
            _logger = logger;
            _config = config;
            _jsonSerializer = jsonSerializer;
        }

        public async Task<Stream> GetResource(string path,
            string mode,
            string localizationCulture,
            string appVersion,
            bool enableMinification)
        {
            Stream resourceStream;

            if (path.Equals("scripts/all.js", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllJavascript(mode, localizationCulture, appVersion, enableMinification).ConfigureAwait(false);
                enableMinification = false;
            }
            else if (path.Equals("css/all.css", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllCss(enableMinification).ConfigureAwait(false);
                enableMinification = false;
            }
            else
            {
                resourceStream = GetRawResourceStream(path);
            }

            if (resourceStream != null)
            {
                // Don't apply any caching for html pages
                // jQuery ajax doesn't seem to handle if-modified-since correctly
                if (IsFormat(path, "html"))
                {
                    if (IsCoreHtml(path))
                    {
                        resourceStream = await ModifyHtml(resourceStream, mode, appVersion, localizationCulture, enableMinification).ConfigureAwait(false);
                    }
                }
                else if (IsFormat(path, "js"))
                {
                    if (path.IndexOf("thirdparty", StringComparison.OrdinalIgnoreCase) == -1 && path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        resourceStream = await ModifyJs(resourceStream, enableMinification).ConfigureAwait(false);
                    }
                }
                else if (IsFormat(path, "css"))
                {
                    if (path.IndexOf("thirdparty", StringComparison.OrdinalIgnoreCase) == -1 && path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        resourceStream = await ModifyCss(resourceStream, enableMinification).ConfigureAwait(false);
                    }
                }
            }

            return resourceStream;
        }

        /// <summary>
        /// Determines whether the specified path is HTML.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="format">The format.</param>
        /// <returns><c>true</c> if the specified path is HTML; otherwise, <c>false</c>.</returns>
        private bool IsFormat(string path, string format)
        {
            return Path.GetExtension(path).EndsWith(format, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the dashboard UI path.
        /// </summary>
        /// <value>The dashboard UI path.</value>
        public string DashboardUIPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_config.Configuration.DashboardSourcePath))
                {
                    return _config.Configuration.DashboardSourcePath;
                }

                return Path.Combine(_config.ApplicationPaths.ApplicationResourcesPath, "dashboard-ui");
            }
        }

        /// <summary>
        /// Gets the dashboard resource path.
        /// </summary>
        /// <param name="virtualPath">The virtual path.</param>
        /// <returns>System.String.</returns>
        private string GetDashboardResourcePath(string virtualPath)
        {
            var rootPath = DashboardUIPath;

            var fullPath = Path.Combine(rootPath, virtualPath.Replace('/', Path.DirectorySeparatorChar));

            try
            {
                fullPath = Path.GetFullPath(fullPath);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in Path.GetFullPath", ex);
            }

            // Don't allow file system access outside of the source folder
            if (!_fileSystem.ContainsSubPath(rootPath, fullPath))
            {
                throw new SecurityException("Access denied");
            }

            return fullPath;
        }

        public async Task<Stream> ModifyCss(Stream sourceStream, bool enableMinification)
        {
            using (sourceStream)
            {
                string content;

                using (var memoryStream = new MemoryStream())
                {
                    await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    content = Encoding.UTF8.GetString(memoryStream.ToArray());

                    if (enableMinification)
                    {
                        try
                        {
                            var result = new KristensenCssMinifier().Minify(content, false, Encoding.UTF8);

                            if (result.Errors.Count > 0)
                            {
                                _logger.Error("Error minifying css: " + result.Errors[0].Message);
                            }
                            else
                            {
                                content = result.MinifiedContent;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error minifying css", ex);
                        }
                    }
                }

                var bytes = Encoding.UTF8.GetBytes(content);

                return new MemoryStream(bytes);
            }
        }

        public async Task<Stream> ModifyJs(Stream sourceStream, bool enableMinification)
        {
            using (sourceStream)
            {
                string content;

                using (var memoryStream = new MemoryStream())
                {
                    await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    content = Encoding.UTF8.GetString(memoryStream.ToArray());

                    if (enableMinification)
                    {
                        try
                        {
                            var result = new CrockfordJsMinifier().Minify(content, false, Encoding.UTF8);

                            if (result.Errors.Count > 0)
                            {
                                _logger.Error("Error minifying javascript: " + result.Errors[0].Message);
                            }
                            else
                            {
                                content = result.MinifiedContent;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error minifying javascript", ex);
                        }
                    }
                }

                var bytes = Encoding.UTF8.GetBytes(content);

                return new MemoryStream(bytes);
            }
        }

        public bool IsCoreHtml(string path)
        {
            if (path.IndexOf("vulcanize", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            if (path.IndexOf(".template.html", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            path = GetDashboardResourcePath(path);
            var parent = Path.GetDirectoryName(path);

            var basePath = DashboardUIPath;

            return string.Equals(basePath, parent, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(Path.Combine(basePath, "voice"), parent, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Modifies the HTML by adding common meta tags, css and js.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="appVersion">The application version.</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <param name="enableMinification">if set to <c>true</c> [enable minification].</param>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> ModifyHtml(Stream sourceStream, string mode, string appVersion, string localizationCulture, bool enableMinification)
        {
            using (sourceStream)
            {
                string html;

                using (var memoryStream = new MemoryStream())
                {
                    await sourceStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    html = Encoding.UTF8.GetString(memoryStream.ToArray());

                    if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
                    {
                        html = ModifyForCordova(html);
                    }

                    if (!string.IsNullOrWhiteSpace(localizationCulture))
                    {
                        var lang = localizationCulture.Split('-').FirstOrDefault();

                        html = html.Replace("<html>", "<html data-culture=\"" + localizationCulture + "\" lang=\"" + lang + "\">");
                    }

                    if (enableMinification)
                    {
                        try
                        {
                            var minifier = new HtmlMinifier(new HtmlMinificationSettings
                            {
                                AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes,
                                RemoveOptionalEndTags = false,
                                RemoveTagsWithoutContent = false
                            });
                            var result = minifier.Minify(html, false);

                            if (result.Errors.Count > 0)
                            {
                                _logger.Error("Error minifying html: " + result.Errors[0].Message);
                            }
                            else
                            {
                                html = result.MinifiedContent;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error minifying html", ex);
                        }
                    }

                    html = html.Replace("<body>", "<body><paper-drawer-panel class=\"mainDrawerPanel mainDrawerPanelPreInit\" forceNarrow><div class=\"mainDrawer\" drawer></div><div class=\"mainDrawerPanelContent\" main><!--<div class=\"pageContainer\">")
                        .Replace("</body>", "</div>--></div></paper-drawer-panel></body>");
                }

                var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + appVersion : string.Empty;

                var imports = new[]
                {
                    "vulcanize-out.html" + versionString
                };
                var importsHtml = string.Join("", imports.Select(i => "<link rel=\"import\" href=\"" + i + "\">").ToArray());

                // It would be better to make polymer completely dynamic and loaded on demand, but seeing issues with that
                // In chrome it is causing the body to be hidden while loading, which leads to width-check methods to return 0 for everything
                //imports = "";

                html = html.Replace("<head>", "<head>" + GetMetaTags(mode) + GetCommonCss(mode, appVersion));

                html = html.Replace("</body>", GetInitialJavascript(mode, appVersion) + importsHtml + GetCommonJavascript(mode, appVersion) + "</body>");

                var bytes = Encoding.UTF8.GetBytes(html);

                return new MemoryStream(bytes);
            }
        }

        private string ModifyForCordova(string html)
        {
            // Strip everything between CORDOVA_EXCLUDE_START and CORDOVA_EXCLUDE_END
            html = ReplaceBetween(html, "<!--CORDOVA_EXCLUDE_START-->", "<!--CORDOVA_EXCLUDE_END-->", string.Empty);

            // Replace CORDOVA_REPLACE_SUPPORTER_SUBMIT_START
            html = ReplaceBetween(html, "<!--CORDOVA_REPLACE_SUPPORTER_SUBMIT_START-->", "<!--CORDOVA_REPLACE_SUPPORTER_SUBMIT_END-->", "<i class=\"fa fa-check\"></i><span>${ButtonPurchase}</span>");

            return html;
        }

        private string ReplaceBetween(string html, string startToken, string endToken, string newHtml)
        {
            var start = html.IndexOf(startToken, StringComparison.OrdinalIgnoreCase);

            if (start == -1)
            {
                return html;
            }

            var end = html.IndexOf(endToken, start, StringComparison.OrdinalIgnoreCase);

            if (end == -1)
            {
                return html;
            }

            string result = html.Substring(start, end - start);
            html = html.Replace(result, newHtml);

            return ReplaceBetween(html, startToken, endToken, newHtml);
        }

        private string GetLocalizationToken(string phrase)
        {
            return "${" + phrase + "}";
        }

        /// <summary>
        /// Gets the meta tags.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetMetaTags(string mode)
        {
            var sb = new StringBuilder();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                //sb.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src *; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval'\">");
            }

            sb.Append("<meta http-equiv=\"X-UA-Compatibility\" content=\"IE=Edge\">");
            sb.Append("<meta name=\"format-detection\" content=\"telephone=no\">");
            sb.Append("<meta name=\"msapplication-tap-highlight\" content=\"no\">");
            sb.Append("<meta name=\"viewport\" content=\"user-scalable=no, initial-scale=1, maximum-scale=1, minimum-scale=1, width=device-width\">");
            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"application-name\" content=\"Emby\">");
            //sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            sb.Append("<meta name=\"robots\" content=\"noindex, nofollow, noarchive\" />");

            // Open graph tags
            sb.Append("<meta property=\"og:title\" content=\"Emby\" />");
            sb.Append("<meta property=\"og:site_name\" content=\"Emby\"/>");
            sb.Append("<meta property=\"og:url\" content=\"http://emby.media\" />");
            sb.Append("<meta property=\"og:description\" content=\"Energize your media.\" />");
            sb.Append("<meta property=\"og:type\" content=\"article\" />");
            sb.Append("<meta property=\"fb:app_id\" content=\"1618309211750238\" />");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"css/images/touchicon.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"css/images/touchicon72.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"css/images/touchicon114.png\" />");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\" />");
            sb.Append("<link rel=\"shortcut icon\" href=\"css/images/favicon.ico\" />");
            sb.Append("<meta name=\"msapplication-TileImage\" content=\"css/images/touchicon144.png\">");
            sb.Append("<meta name=\"msapplication-TileColor\" content=\"#333333\">");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the common CSS.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private string GetCommonCss(string mode, string version)
        {
            var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + version : string.Empty;

            var files = new[]
                            {
                                "css/all.css" + versionString
                            };

            var tags = files.Select(s => string.Format("<link rel=\"stylesheet\" href=\"{0}\" />", s)).ToArray();

            return string.Join(string.Empty, tags);
        }

        /// <summary>
        /// Gets the common javascript.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private string GetInitialJavascript(string mode, string version)
        {
            var builder = new StringBuilder();

            var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + version : string.Empty;

            var files = new List<string>
            {
                "bower_components/webcomponentsjs/webcomponents-lite.js" + versionString
            };

            var tags = files.Select(s => string.Format("<script src=\"{0}\"></script>", s)).ToArray();

            builder.Append(string.Join(string.Empty, tags));

            return builder.ToString();
        }

        /// <summary>
        /// Gets the common javascript.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private string GetCommonJavascript(string mode, string version)
        {
            var builder = new StringBuilder();

            var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + version : string.Empty;

            var files = new List<string>
            {
                "scripts/all.js" + versionString
            };

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                files.Insert(0, "cordova.js");
            }

            var tags = files.Select(s => string.Format("<script src=\"{0}\"></script>", s)).ToArray();

            builder.Append(string.Join(string.Empty, tags));

            return builder.ToString();
        }

        /// <summary>
        /// Gets a stream containing all concatenated javascript
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetAllJavascript(string mode, string culture, string version, bool enableMinification)
        {
            var memoryStream = new MemoryStream();
            var newLineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);

            await AppendResource(memoryStream, "bower_components/jquery/dist/jquery.min.js", newLineBytes).ConfigureAwait(false);

            //await AppendLocalization(memoryStream, culture, excludePhrases).ConfigureAwait(false);
            await memoryStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(mode))
            {
                var appModeBytes = Encoding.UTF8.GetBytes(string.Format("window.appMode='{0}';", mode));
                await memoryStream.WriteAsync(appModeBytes, 0, appModeBytes.Length).ConfigureAwait(false);
            }

            // Write the version string for the dashboard comparison function
            var versionString = string.Format("window.dashboardVersion='{0}';", version);
            var versionBytes = Encoding.UTF8.GetBytes(versionString);

            await memoryStream.WriteAsync(versionBytes, 0, versionBytes.Length).ConfigureAwait(false);
            await memoryStream.WriteAsync(newLineBytes, 0, newLineBytes.Length).ConfigureAwait(false);

            var builder = new StringBuilder();

            var commonFiles = new[]
            {
                "bower_components/requirejs/require.js",
                "thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.js",
                "thirdparty/browser.js",
                "thirdparty/jquery.unveil-custom.js",
                "apiclient/logger.js",
                "apiclient/md5.js",
                "apiclient/store.js",
                "apiclient/device.js",
                "apiclient/credentials.js",
                "apiclient/ajax.js",
                "apiclient/events.js",
                "apiclient/deferred.js",
                "apiclient/apiclient.js"
            }.ToList();

            commonFiles.Add("apiclient/connectionmanager.js");

            foreach (var file in commonFiles)
            {
                using (var fs = _fileSystem.GetFileStream(GetDashboardResourcePath(file), FileMode.Open, FileAccess.Read, FileShare.ReadWrite, true))
                {
                    using (var streamReader = new StreamReader(fs))
                    {
                        var text = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        builder.Append(text);
                        builder.Append(Environment.NewLine);
                    }
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

            if (enableMinification)
            {
                try
                {
                    var result = new CrockfordJsMinifier().Minify(js, false, Encoding.UTF8);

                    if (result.Errors.Count > 0)
                    {
                        _logger.Error("Error minifying javascript: " + result.Errors[0].Message);
                    }
                    else
                    {
                        js = result.MinifiedContent;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error minifying javascript", ex);
                }
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
                                "globalize.js",
                                "site.js",
                                "librarybrowser.js",
                                "librarylist.js",
                                "librarymenu.js",
                                "mediacontroller.js",
                                "backdrops.js",
                                "sync.js",
                                "playlistmanager.js",
                                "appsettings.js",
                                "mediaplayer.js",
                                "mediaplayer-video.js",
                                "alphapicker.js",
                                "notifications.js",
                                "remotecontrol.js",
                                "search.js",
                                "thememediaplayer.js"
                            };
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


        /// <summary>
        /// Gets all CSS.
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        private async Task<Stream> GetAllCss(bool enableMinification)
        {
            var memoryStream = new MemoryStream();

            var files = new[]
                                  {
                                      "thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.theme.css",
                                      "thirdparty/jquerymobile-1.4.5/jquery.mobile.custom.structure.css",
                                      "css/site.css",
                                      "css/chromecast.css",
                                      "css/mediaplayer.css",
                                      "css/mediaplayer-video.css",
                                      "css/librarymenu.css",
                                      "css/librarybrowser.css",
                                      "css/card.css",
                                      "css/notifications.css",
                                      "css/search.css",
                                      "css/remotecontrol.css",
                                      "css/userimage.css",
                                      "css/nowplaying.css",
                                      "css/materialize.css",
                                      "thirdparty/paper-button-style.css"
                                  };

            var builder = new StringBuilder();

            foreach (var file in files)
            {
                var path = GetDashboardResourcePath(file);

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

            if (enableMinification)
            {
                try
                {
                    var result = new KristensenCssMinifier().Minify(builder.ToString(), false, Encoding.UTF8);

                    if (result.Errors.Count > 0)
                    {
                        _logger.Error("Error minifying css: " + result.Errors[0].Message);
                    }
                    else
                    {
                        css = result.MinifiedContent;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error minifying css", ex);
                }
            }

            var bytes = Encoding.UTF8.GetBytes(css);
            memoryStream.Write(bytes, 0, bytes.Length);

            memoryStream.Position = 0;
            return memoryStream;
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

    }
}
