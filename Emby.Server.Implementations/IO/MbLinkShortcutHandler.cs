#pragma warning disable CS1591

using System;
using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    public class MbLinkShortcutHandler : IShortcutHandler
    {
        private readonly IFileSystem _fileSystem;

        public MbLinkShortcutHandler(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public string Extension => ".mblink";

        public string Resolve(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentException("Shortcut path is empty or null.", nameof(shortcutPath));
            }

            if (string.Equals(Path.GetExtension(shortcutPath), ".mblink", StringComparison.OrdinalIgnoreCase))
            {
                var path = File.ReadAllText(shortcutPath);

                return _fileSystem.NormalizePath(path);
            }

            return null;
        }

        public void Create(string shortcutPath, string targetPath)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException(nameof(shortcutPath));
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException(nameof(targetPath));
            }

            File.WriteAllText(shortcutPath, targetPath);
        }
    }
}
