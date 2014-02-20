using System;
using System.IO;

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
        public static void Configure(bool autorun)
        {
            var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Media Browser 3", "Media Browser Server.lnk");

            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

            if (autorun)
            {
                //Copy our shortut into the startup folder for this user
                File.Copy(shortcutPath, Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "MBstartup.lnk"), true);
            }
            else
            {
                //Remove our shortcut from the startup folder for this user
                File.Delete(Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "MBstartup.lnk"));
            }
        }
    }
}
