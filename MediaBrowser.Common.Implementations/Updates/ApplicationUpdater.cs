using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System.Diagnostics;
using System.IO;

namespace MediaBrowser.Common.Implementations.Updates
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
        private const string UpdaterExe = "Mediabrowser.Updater.exe";
        private const string UpdaterDll = "Mediabrowser.InstallUtil.dll";
        public void UpdateApplication(MBApplication app, IApplicationPaths appPaths, string archive, ILogger logger, string restartServiceName)
        {
            // First see if there is a version file and read that in
            var version = "Unknown";
            if (File.Exists(archive + ".ver"))
            {
                version = File.ReadAllText(archive + ".ver");
            }

            // Use our installer passing it the specific archive
            // We need to copy to a temp directory and execute it there
            var source = Path.Combine(appPaths.ProgramSystemPath, UpdaterExe);

            logger.Info("Copying updater to temporary location");
            var tempUpdater = Path.Combine(Path.GetTempPath(), UpdaterExe);
            File.Copy(source, tempUpdater, true);
            source = Path.Combine(appPaths.ProgramSystemPath, UpdaterDll);
            var tempUpdaterDll = Path.Combine(Path.GetTempPath(), UpdaterDll);

            logger.Info("Copying updater dependencies to temporary location");
            File.Copy(source, tempUpdaterDll, true);
            var product = app == MBApplication.MBTheater ? "mbt" : "server";
            // Our updater needs SS and ionic
            source = Path.Combine(appPaths.ProgramSystemPath, "ServiceStack.Text.dll");
            File.Copy(source, Path.Combine(Path.GetTempPath(), "ServiceStack.Text.dll"), true);
            source = Path.Combine(appPaths.ProgramSystemPath, "SharpCompress.dll");
            File.Copy(source, Path.Combine(Path.GetTempPath(), "SharpCompress.dll"), true);

            logger.Info("Starting updater process.");
            Process.Start(tempUpdater, string.Format("product={0} archive=\"{1}\" caller={2} pismo=false version={3} service={4} installpath={5}", product, archive, Process.GetCurrentProcess().Id, version, restartServiceName ?? string.Empty, appPaths.ProgramDataPath));

            // That's it.  The installer will do the work once we exit
        }
    }
}
