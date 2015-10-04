using MediaBrowser.Common.IO;
using System;
using System.IO;
using CommonIO;

namespace MediaBrowser.ServerApplication.Native
{
    /// <summary>
    /// Class Autorun
    /// </summary>
    public static class Autorun
    {
        /// <summary>
        /// Configures the specified autorun.
        /// </summary>
        /// <param name="autorun">if set to <c>true</c> [autorun].</param>
        /// <param name="fileSystem">The file system.</param>
        public static void Configure(bool autorun, IFileSystem fileSystem)
        {
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Media Browser 3", "Media Browser Server.lnk");

            if (!Directory.Exists(Path.GetDirectoryName(shortcutPath)))
            {
                shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Emby", "Emby Server.lnk");
            }

            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            // Remove lnk from old name
            try
            {
                fileSystem.DeleteFile(Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "Media Browser Server.lnk"));
            }
            catch
            {
                
            }

            if (autorun)
            {
                //Copy our shortut into the startup folder for this user
                File.Copy(shortcutPath, Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "Emby Server.lnk"), true);
            }
            else
            {
                //Remove our shortcut from the startup folder for this user
                fileSystem.DeleteFile(Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "Emby Server.lnk"));
            }
        }
    }
}
