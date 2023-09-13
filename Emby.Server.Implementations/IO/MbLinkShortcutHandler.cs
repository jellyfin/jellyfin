#pragma warning disable CS1591

using System;
using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    public class MbLinkShortcutHandler : IShortcutHandler
    {
        public string Extension => ".mblink";

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

        public void Create(string shortcutPath, string targetPath)
        {
            ArgumentException.ThrowIfNullOrEmpty(shortcutPath);
            ArgumentException.ThrowIfNullOrEmpty(targetPath);

            File.WriteAllText(shortcutPath, targetPath);
        }
    }
}
