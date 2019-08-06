using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller;

namespace MediaBrowser.WebDashboard.Api
{
    public class PackageCreator
    {
        private readonly string _basePath;
        private readonly IResourceFileManager _resourceFileManager;

        public PackageCreator(string basePath, IResourceFileManager resourceFileManager)
        {
            _basePath = basePath;
            _resourceFileManager = resourceFileManager;
        }

        public async Task<Stream> GetResource(
            string virtualPath,
            string mode,
            string localizationCulture,
            string appVersion)
        {
            var resourcePath = _resourceFileManager.GetResourcePath(_basePath, virtualPath);
            Stream resourceStream = File.OpenRead(resourcePath);

            if (resourceStream != null && IsCoreHtml(virtualPath))
            {
                resourceStream = await ModifyHtml(virtualPath, resourceStream, mode, appVersion, localizationCulture).ConfigureAwait(false);
            }

            return resourceStream;
        }

        public static bool IsCoreHtml(string path)
        {
            if (path.IndexOf(".template.html", StringComparison.OrdinalIgnoreCase) != -1)
            {
                return false;
            }

            return string.Equals(Path.GetExtension(path), ".html", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Modifies the HTML by adding common meta tags, css and js.
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> ModifyHtml(
            string path,
            Stream sourceStream,
            string mode,
            string appVersion,
            string localizationCulture)
        {
            var isMainIndexPage = string.Equals(path, "index.html", StringComparison.OrdinalIgnoreCase);

            string html;
            using (var reader = new StreamReader(sourceStream, Encoding.UTF8))
            {
                html = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            if (isMainIndexPage && !string.IsNullOrWhiteSpace(localizationCulture))
            {
                var lang = localizationCulture.Split('-')[0];

                html = html.Replace("<html", "<html data-culture=\"" + localizationCulture + "\" lang=\"" + lang + "\"");
            }

            if (isMainIndexPage)
            {
                html = html.Replace("<head>", "<head>" + GetMetaTags(mode));
            }

            // Disable embedded scripts from plugins. We'll run them later once resources have loaded
            if (html.IndexOf("<script", StringComparison.OrdinalIgnoreCase) != -1)
            {
                html = html.Replace("<script", "<!--<script");
                html = html.Replace("</script>", "</script>-->");
            }

            if (isMainIndexPage)
            {
                html = html.Replace("</body>", GetCommonJavascript(mode, appVersion) + "</body>");
            }

            var bytes = Encoding.UTF8.GetBytes(html);

            return new MemoryStream(bytes);

        }

        /// <summary>
        /// Gets the meta tags.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetMetaTags(string mode)
        {
            var sb = new StringBuilder();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode, "android", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src * 'self' 'unsafe-inline' 'unsafe-eval' data: gap: file: filesystem: ws: wss:;\">");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the common javascript.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private static string GetCommonJavascript(string mode, string version)
        {
            var builder = new StringBuilder();

            builder.Append("<script>");
            if (!string.IsNullOrWhiteSpace(mode))
            {
                builder.AppendFormat("window.appMode='{0}';", mode);
            }
            else
            {
                builder.AppendFormat("window.dashboardVersion='{0}';", version);
            }

            builder.Append("</script>");

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append("<script src=\"cordova.js\" defer></script>");
            }

            builder.Append("<script src=\"scripts/apploader.js");
            if (!string.IsNullOrWhiteSpace(version))
            {
                builder.Append("?v=");
                builder.Append(version);
            }

            builder.Append("\" defer></script>");

            return builder.ToString();
        }
    }
}
