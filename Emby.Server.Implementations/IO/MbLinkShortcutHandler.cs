using System;
using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// Handler for .mblink shortcut files.
    /// </summary>
    public class MbLinkShortcutHandler : IShortcutHandler
    {
        /// <inheritdoc />
        public string Extension => ".mblink";

        /// <inheritdoc />
        public string? Resolve(string shortcutPath)
        {
            ArgumentException.ThrowIfNullOrEmpty(shortcutPath);

            if (Path.GetExtension(shortcutPath.AsSpan()).Equals(".mblink", StringComparison.OrdinalIgnoreCase))
            {
                var path = File.ReadAllText(shortcutPath);

                return Path.TrimEndingDirectorySeparator(path);
            }

            return null;
        }

        /// <inheritdoc />
        public void Create(string shortcutPath, string targetPath)
        {
            ArgumentException.ThrowIfNullOrEmpty(shortcutPath);
            ArgumentException.ThrowIfNullOrEmpty(targetPath);

            File.WriteAllText(shortcutPath, targetPath);
        }
    }
}
