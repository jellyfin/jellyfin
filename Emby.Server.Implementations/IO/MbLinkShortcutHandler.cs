using System;
using System.IO;

using MediaBrowser.Controller.IO;
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

        public string Extension
        {
            get { return ".mblink"; }
        }

        public string Resolve(string shortcutPath)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException("filenshortcutPathame");
            }

            if (string.Equals(Path.GetExtension(shortcutPath), ".mblink", StringComparison.OrdinalIgnoreCase))
            {
                var path = _fileSystem.ReadAllText(shortcutPath);

                return _fileSystem.NormalizePath(path);
            }

            return null;
        }

        public void Create(string shortcutPath, string targetPath)
        {
            if (string.IsNullOrEmpty(shortcutPath))
            {
                throw new ArgumentNullException("shortcutPath");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentNullException("targetPath");
            }

            _fileSystem.WriteAllText(shortcutPath, targetPath);
        }
    }
}
