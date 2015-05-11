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
            var isHtml = IsHtml(path);

            Stream resourceStream;

            if (path.Equals("scripts/all.js", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllJavascript(mode, localizationCulture, appVersion, enableMinification).ConfigureAwait(false);
            }
            else if (path.Equals("css/all.css", StringComparison.OrdinalIgnoreCase))
            {
                resourceStream = await GetAllCss(enableMinification).ConfigureAwait(false);
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
                    resourceStream = await ModifyHtml(resourceStream, mode, localizationCulture, enableMinification).ConfigureAwait(false);
                }
            }

            return resourceStream;
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
            return Path.Combine(DashboardUIPath, virtualPath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Modifies the HTML by adding common meta tags, css and js.
        /// </summary>
        /// <param name="sourceStream">The source stream.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="localizationCulture">The localization culture.</param>
        /// <param name="enableMinification">if set to <c>true</c> [enable minification].</param>
        /// <returns>Task{Stream}.</returns>
        public async Task<Stream> ModifyHtml(Stream sourceStream, string mode, string localizationCulture, bool enableMinification)
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

                    if (enableMinification)
                    {
                        try
                        {
                            var minifier = new HtmlMinifier(new HtmlMinificationSettings());
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

                var version = GetType().Assembly.GetName().Version;

                html = html.Replace("<head>", "<head>" + GetMetaTags(mode) + GetCommonCss(mode, version) + GetCommonJavascript(mode, version));

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
        private static string GetMetaTags(string mode)
        {
            var sb = new StringBuilder();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("<meta http-equiv=\"Content-Security-Policy\" content=\"default-src *; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval'\">");
            }

            sb.Append("<meta http-equiv=\"X-UA-Compatibility\" content=\"IE=Edge\">");
            sb.Append("<meta name=\"format-detection\" content=\"telephone=no\">");
            sb.Append("<meta name=\"msapplication-tap-highlight\" content=\"no\">");
            sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, minimum-scale=1, maximum-scale=1, user-scalable=no\">");
            sb.Append("<meta name=\"apple-mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"mobile-web-app-capable\" content=\"yes\">");
            sb.Append("<meta name=\"application-name\" content=\"Emby\">");
            //sb.Append("<meta name=\"apple-mobile-web-app-status-bar-style\" content=\"black-translucent\">");

            sb.Append("<meta name=\"robots\" content=\"noindex, nofollow, noarchive\" />");

            // http://developer.apple.com/library/ios/#DOCUMENTATION/AppleApplications/Reference/SafariWebContent/ConfiguringWebApplications/ConfiguringWebApplications.html
            sb.Append("<link rel=\"apple-touch-icon\" href=\"css/images/touchicon.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"72x72\" href=\"css/images/touchicon72.png\" />");
            sb.Append("<link rel=\"apple-touch-icon\" sizes=\"114x114\" href=\"css/images/touchicon114.png\" />");
            sb.Append("<link rel=\"apple-touch-startup-image\" href=\"css/images/iossplash.png\" />");
            sb.Append("<link rel=\"shortcut icon\" href=\"css/images/favicon.ico\" />");
            sb.Append("<meta name=\"msapplication-TileImage\" content=\"css/images/touchicon144.png\">");
            sb.Append("<meta name=\"msapplication-TileColor\" content=\"#23456B\">");

            return sb.ToString();
        }

        /// <summary>
        /// Gets the common CSS.
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="version">The version.</param>
        /// <returns>System.String.</returns>
        private string GetCommonCss(string mode, Version version)
        {
            var versionString = !string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase) ? "?v=" + version : string.Empty;

            var files = new[]
                            {
                                "thirdparty/jquerymobile-1.4.5/jquery.mobile-1.4.5.min.css",
                                "thirdparty/fontawesome/css/font-awesome.min.css" + versionString,
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
        private string GetCommonJavascript(string mode, Version version)
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

            // jQuery + jQuery mobile
            await AppendResource(memoryStream, "thirdparty/jquery-2.1.1.min.js", newLineBytes).ConfigureAwait(false);
            await AppendResource(memoryStream, "thirdparty/jquerymobile-1.4.5/jquery.mobile-1.4.5.min.js", newLineBytes).ConfigureAwait(false);

            await AppendResource(memoryStream, "thirdparty/browser.js", newLineBytes).ConfigureAwait(false);

            await AppendResource(memoryStream, "thirdparty/require.js", newLineBytes).ConfigureAwait(false);

            await AppendResource(memoryStream, "thirdparty/jquery.unveil-custom.js", newLineBytes).ConfigureAwait(false);

            await AppendLocalization(memoryStream, culture).ConfigureAwait(false);
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

            var apiClientFiles = new[]
            {
                "thirdparty/apiclient/logger.js",
                "thirdparty/apiclient/md5.js",
                "thirdparty/apiclient/sha1.js",
                "thirdparty/apiclient/store.js",
                "thirdparty/apiclient/network.js",
                "thirdparty/apiclient/device.js",
                "thirdparty/apiclient/credentials.js",
                "thirdparty/apiclient/ajax.js",
                "thirdparty/apiclient/events.js",
                "thirdparty/apiclient/deferred.js",
                "thirdparty/apiclient/apiclient.js",
                "thirdparty/apiclient/connectservice.js"
            }.ToList();

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                apiClientFiles.Add("thirdparty/cordova/serverdiscovery.js");
            }
            else
            {
                apiClientFiles.Add("thirdparty/apiclient/serverdiscovery.js");
            }
            apiClientFiles.Add("thirdparty/apiclient/connectionmanager.js");

            if (string.Equals(mode, "cordova", StringComparison.OrdinalIgnoreCase))
            {
                apiClientFiles.Add("thirdparty/cordova/remotecontrols.js");
            }
            
            foreach (var file in apiClientFiles)
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
                                "site.js",
                                "librarybrowser.js",
                                "librarylist.js",
                                "editorsidebar.js",
                                "librarymenu.js",
                                "mediacontroller.js",
                                "chromecast.js",
                                "backdrops.js",
                                "sync.js",
                                "syncjob.js",
                                "appservices.js",
                                "playlistmanager.js",

                                "mediaplayer.js",
                                "mediaplayer-video.js",
                                "nowplayingbar.js",
                                "nowplayingpage.js",
                                "taskbutton.js",

                                "ratingdialog.js",
                                "aboutpage.js",
                                "alphapicker.js",
                                "addpluginpage.js",
                                "advancedconfigurationpage.js",
                                "metadataadvanced.js",
                                "autoorganizetv.js",
                                "autoorganizelog.js",
                                "channels.js",
                                "channelslatest.js",
                                "channelitems.js",
                                "channelsettings.js",
                                "connectlogin.js",
                                "dashboardgeneral.js",
                                "dashboardhosting.js",
                                "dashboardpage.js",
                                "device.js",
                                "devices.js",
                                "devicesupload.js",
                                "directorybrowser.js",
                                "dlnaprofile.js",
                                "dlnaprofiles.js",
                                "dlnasettings.js",
                                "dlnaserversettings.js",
                                "editcollectionitems.js",
                                "edititemmetadata.js",
                                "edititemimages.js",
                                "edititemsubtitles.js",

                                "playbackconfiguration.js",
                                "cinemamodeconfiguration.js",
                                "encodingsettings.js",

                                "externalplayer.js",
                                "favorites.js",
                                "forgotpassword.js",
                                "forgotpasswordpin.js",
                                "gamesrecommendedpage.js",
                                "gamesystemspage.js",
                                "gamespage.js",
                                "gamegenrepage.js",
                                "gamestudiospage.js",
                                "homelatest.js",
                                "indexpage.js",
                                "itembynamedetailpage.js",
                                "itemdetailpage.js",
                                "itemlistpage.js",
                                "kids.js",
                                "librarypathmapping.js",
                                "reports.js",
                                "librarysettings.js",
                                "livetvchannel.js",
                                "livetvchannels.js",
                                "livetvguide.js",
                                "livetvitems.js",
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
                                "metadatanfo.js",
                                "moviegenres.js",
                                "moviecollections.js",
                                "movies.js",
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
                                "photos.js",
                                "playlists.js",
                                "playlistedit.js",

                                "plugincatalogpage.js",
                                "pluginspage.js",
                                "remotecontrol.js",
                                "scheduledtaskpage.js",
                                "scheduledtaskspage.js",
                                "search.js",
                                "selectserver.js",
                                "serversecurity.js",
                                "songs.js",
                                "streamingsettings.js",
                                "supporterkeypage.js",
                                "supporterpage.js",
                                "syncactivity.js",
                                "syncsettings.js",
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
                                "usernew.js",
                                "myprofile.js",
                                "userpassword.js",
                                "userprofilespage.js",
                                "userparentalcontrol.js",
                                "userlibraryaccess.js",
                                "wizardagreement.js",
                                "wizardfinishpage.js",
                                "wizardservice.js",
                                "wizardstartpage.js",
                                "wizardsettings.js",
                                "wizarduserpage.js"
                            };
        }

        private async Task AppendLocalization(Stream stream, string culture)
        {
            var js = "window.localizationGlossary=" + _jsonSerializer.SerializeToString(_localization.GetJavaScriptLocalizationDictionary(culture));

            var bytes = Encoding.UTF8.GetBytes(js);
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
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
            var files = new[]
                                  {
                                      "site.css",
                                      "chromecast.css",
                                      "mediaplayer.css",
                                      "mediaplayer-video.css",
                                      "librarymenu.css",
                                      "librarybrowser.css",
                                      "detailtable.css",
                                      "card.css",
                                      "tileitem.css",
                                      "metadataeditor.css",
                                      "notifications.css",
                                      "search.css",
                                      "pluginupdates.css",
                                      "remotecontrol.css",
                                      "userimage.css",
                                      "livetv.css",
                                      "nowplaying.css",
                                      "icons.css",
                                      "materialize.css"
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

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(css));

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
