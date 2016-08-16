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

            if (path.Equals("css/all.css", StringComparison.OrdinalIgnoreCase))
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
                        resourceStream = await ModifyHtml(path, resourceStream, mode, appVersion, localizationCulture, enableMinification).ConfigureAwait(false);
                    }
                }
                else if (IsFormat(path, "js"))
                {
                    if (path.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) == -1 && path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        resourceStream = await ModifyJs(resourceStream, enableMinification).ConfigureAwait(false);
                    }
                }
                else if (IsFormat(path, "css"))
                {
                    if (path.IndexOf(".min.", StringComparison.OrdinalIgnoreCase) == -1 && path.IndexOf("bower_components", StringComparison.OrdinalIgnoreCase) == -1)
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
        /// <param name="path">The path.</param>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="appVersion">The application version.</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <param name="enableMinification">if set to <c>true</c> [enable minification].</param>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> ModifyHtml(string path, Stream sourceStream, string mode, string appVersion, string localizationCulture, bool enableMinification)
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
                    }
                    else if (!string.IsNullOrWhiteSpace(path) && !string.Equals(path, "index.html", StringComparison.OrdinalIgnoreCase))
                    {
                        var index = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
                        if (index != -1)
                        {
                            html = html.Substring(index);
                            index = html.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                            if (index != -1)
                            {
                                html = html.Substring(0, index+7);
                            }
                        }
                        var mainFile = File.ReadAllText(GetDashboardResourcePath("index.html"));

                        html = ReplaceFirst(mainFile, "<div class=\"mainAnimatedPages skinBody\"></div>", "<div class=\"mainAnimatedPages skinBody hide\">" + html + "</div>");
                    }

                    if (!string.IsNullOrWhiteSpace(localizationCulture))
                    {
                        var lang = localizationCulture.Split('-').FirstOrDefault();

                        html = html.Replace("<html", "<html data-culture=\"" + localizationCulture + "\" lang=\"" + lang + "\"");
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
                }

                html = html.Replace("<head>", "<head>" + GetMetaTags(mode) + GetCommonCss(mode, appVersion));

                // Disable embedded scripts from plugins. We'll run them later once resources have loaded
                if (html.IndexOf("<script", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    html = html.Replace("<script", "<!--<script");
                    html = html.Replace("</script>", "</script>-->");
                }

                html = html.Replace("</body>", GetCommonJavascript(mode, appVersion) + "</body>");

                var bytes = Encoding.UTF8.GetBytes(html);

                return new MemoryStream(bytes);
            }
        }

        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
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
                sb.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src * 'unsafe-inline' 'unsafe-eval' data: filesystem:;\">");
            }

            sb.Append("<link rel=\"manifest\" href=\"manifest.json\">");
            sb.Append("<meta http-equiv=\"X-UA-Compatibility\" content=\"IE=Edge\">");
            sb.Append("<meta name=\"format-detection\" content=\"telephone=no\">");
            sb.Append("<meta name=\"msapplication-tap-highlight\" content=\"no\">");
            sb.Append("<meta name=\"viewport\" content=\"user-scalable=no, initial-scale=1, maximum-scale=1, minimum-scale=1, width=device-width\">");
            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"application-name\" content=\"Emby\">");
            //sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            sb.Append("<meta name=\"robots\" content=\"noindex, nofollow, noarchive\">");

            // Open graph tags
            sb.Append("<meta property=\"og:title\" content=\"Emby\">");
            sb.Append("<meta property=\"og:site_name\" content=\"Emby\">");
            sb.Append("<meta property=\"og:url\" content=\"http://emby.media\">");
            sb.Append("<meta property=\"og:description\" content=\"Energize your media.\">");
            sb.Append("<meta property=\"og:type\" content=\"article\">");
            sb.Append("<meta property=\"fb:app_id\" content=\"1618309211750238\">");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"touchicon.png\">");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"touchicon72.png\">");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"touchicon114.png\">");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\">");
            sb.Append("<link rel=\"shortcut icon\" href=\"css/images/favicon.ico\">");
            sb.Append("<meta name=\"msapplication-TileImage\" content=\"touchicon144.png\">");
            sb.Append("<meta name=\"msapplication-TileColor\" content=\"#333333\">");
            sb.Append("<meta name=\"theme-color\" content=\"#43A047\">");

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

            var tags = files.Select(s => string.Format("<link rel=\"stylesheet\" href=\"{0}\" async />", s)).ToArray();

            return string.Join(string.Empty, tags);
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

            builder.Append("<script>");
            if (!string.IsNullOrWhiteSpace(mode))
            {
                builder.AppendFormat("window.appMode='{0}';", mode);
            }

            if (!string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                builder.AppendFormat("window.dashboardVersion='{0}';", version);
            }

            builder.Append("</script>");

            var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + version : string.Empty;

            var files = new List<string>();

            files.Add("bower_components/requirejs/require.js" + versionString);

            files.Add("scripts/site.js" + versionString);

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                files.Insert(0, "cordova.js");
            }

            var tags = files.Select(s => string.Format("<script src=\"{0}\" defer></script>", s)).ToArray();

            builder.Append(string.Join(string.Empty, tags));

            return builder.ToString();
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
                                      "css/site.css",
                                      "css/librarymenu.css",
                                      "css/librarybrowser.css",
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
