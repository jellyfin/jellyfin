using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Themes;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Themes
{
    public class AppThemeManager : IAppThemeManager
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IJsonSerializer _json;
        private readonly ILogger _logger;

        private readonly string[] _supportedImageExtensions = { ".png", ".jpg", ".jpeg" };

        public AppThemeManager(IServerApplicationPaths appPaths, IFileSystem fileSystem, IJsonSerializer json, ILogger logger)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _json = json;
            _logger = logger;
        }

        private string ThemePath
        {
            get
            {
                return Path.Combine(_appPaths.ItemsByNamePath, "appthemes");
            }
        }

        private string GetThemesPath(string applicationName)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                throw new ArgumentNullException("applicationName");
            }

            // Force everything lowercase for consistency and maximum compatibility with case-sensitive file systems
            var name = _fileSystem.GetValidFilename(applicationName.ToLower());

            return Path.Combine(ThemePath, name);
        }

        private string GetThemePath(string applicationName, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            
            // Force everything lowercase for consistency and maximum compatibility with case-sensitive file systems
            name = _fileSystem.GetValidFilename(name.ToLower());

            return Path.Combine(GetThemesPath(applicationName), name);
        }

        public IEnumerable<AppThemeInfo> GetThemes(string applicationName)
        {
            var path = GetThemesPath(applicationName);

            try
            {
                return Directory
                    .EnumerateFiles(path, "*", SearchOption.AllDirectories)
                    .Where(i => string.Equals(Path.GetExtension(i), ".json", StringComparison.OrdinalIgnoreCase))
                    .Select(i =>
                    {
                        try
                        {
                            return _json.DeserializeFromFile<AppThemeInfo>(i);
                        }
                        catch (Exception ex)
                        {
                            _logger.ErrorException("Error deserializing {0}", ex, i);
                            return null;
                        }

                    }).Where(i => i != null);
            }
            catch (DirectoryNotFoundException)
            {
                return new List<AppThemeInfo>();
            }
        }

        public AppTheme GetTheme(string applicationName, string name)
        {
            var themePath = GetThemePath(applicationName, name);
            var file = Path.Combine(themePath, "theme.json");

            var theme = _json.DeserializeFromFile<AppTheme>(file);

            theme.Images = new DirectoryInfo(themePath)
                .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .Where(i => _supportedImageExtensions.Contains(i.Extension, StringComparer.OrdinalIgnoreCase))
                .Select(GetThemeImage)
                .ToList();

            return theme;
        }

        private ThemeImage GetThemeImage(FileInfo file)
        {
            var dateModified = _fileSystem.GetLastWriteTimeUtc(file);

            var cacheTag = (file.FullName + dateModified.Ticks).GetMD5().ToString("N");

            return new ThemeImage
            {
                CacheTag = cacheTag,
                Name = file.Name
            };
        }

        public void SaveTheme(AppTheme theme)
        {
            var themePath = GetThemePath(theme.ApplicationName, theme.Name);
            var file = Path.Combine(themePath, "theme.json");

            Directory.CreateDirectory(themePath);

            // Clone it so that we don't serialize all the images - they're always dynamic
            var clone = new AppTheme
            {
                ApplicationName = theme.ApplicationName,
                Name = theme.Name,
                Options = theme.Options,
                Images = null
            };

            _json.SerializeToFile(clone, file);
        }

        public InternalThemeImage GetImageImageInfo(string applicationName, string themeName, string imageName)
        {
            var themePath = GetThemePath(applicationName, themeName);

            var fullPath = Path.Combine(themePath, imageName);

            var file = new DirectoryInfo(themePath).EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                .First(i => string.Equals(i.FullName, fullPath, StringComparison.OrdinalIgnoreCase));

            var themeImage = GetThemeImage(file);

            return new InternalThemeImage
            {
                CacheTag = themeImage.CacheTag,
                Name = themeImage.Name,
                Path = file.FullName,
                DateModified = _fileSystem.GetLastWriteTimeUtc(file)
            };
        }
    }
}
