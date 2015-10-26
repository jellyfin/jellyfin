using System;
using System.IO;
using CommonIO;

namespace MediaBrowser.Server.Startup.Common
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

            File.WriteAllText(shortcutPath, targetPath);
        }
    }
}
