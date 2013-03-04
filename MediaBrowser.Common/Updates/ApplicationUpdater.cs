using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Kernel;

namespace MediaBrowser.Common.Updates
{
    public enum MBApplication
    {
        MBServer,
        MBTheater
    }

    /// <summary>
    /// Update the specified application using the specified archive
    /// </summary>
    public class ApplicationUpdater
    {
        private const string UpdaterExe = "Mediabrowser.Installer.exe";
        public void UpdateApplication(MBApplication app, IApplicationPaths appPaths, string archive)
        {
            // Use our installer passing it the specific archive
            // We need to copy to a temp directory and execute it there
            var source = Path.Combine(appPaths.ProgramSystemPath, UpdaterExe);
            var target = Path.Combine(Path.GetTempPath(), UpdaterExe);
            var product = app == MBApplication.MBTheater ? "mbt" : "server";
            File.Copy(source, target, true);
            Process.Start(UpdaterExe, string.Format("product={0} archive=\"{1}\" caller={2}", product, archive, Process.GetCurrentProcess().Id));

            // That's it.  The installer will do the work once we exit
        }
    }
}
