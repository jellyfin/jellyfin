using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Networking;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CommonIO;
using MediaBrowser.Controller.Power;
using MediaBrowser.Server.Startup.Common.FFMpeg;

namespace MediaBrowser.ServerApplication.Native
{
    public class WindowsApp : INativeApp
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public WindowsApp(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        public List<Assembly> GetAssembliesWithParts()
        {
            var list = new List<Assembly>();

            if (!System.Environment.Is64BitProcess)
            {
                //list.Add(typeof(PismoIsoManager).Assembly);
            }

            list.Add(GetType().Assembly);

            return list;
        }

        public void AuthorizeServer(int udpPort, int httpServerPort, int httpsPort, string applicationPath, string tempDirectory)
        {
            ServerAuthorization.AuthorizeServer(udpPort, httpServerPort, httpsPort, applicationPath, tempDirectory);
        }

        public NativeEnvironment Environment
        {
            get
            {
                return new NativeEnvironment
                {
                    OperatingSystem = OperatingSystem.Windows,
                    SystemArchitecture = System.Environment.Is64BitOperatingSystem ? Architecture.X86_X64 : Architecture.X86,
                    OperatingSystemVersionString = System.Environment.OSVersion.VersionString
                };
            }
        }

        public bool SupportsLibraryMonitor
        {
            get { return true; }
        }

        public bool SupportsRunningAsService
        {
            get
            {
                return true;
            }
        }

        public bool IsRunningAsService
        {
            get;
            set;
        }

        public bool CanSelfRestart
        {
            get
            {
                return MainStartup.CanSelfRestart;
            }
        }

        public bool SupportsAutoRunAtStartup
        {
            get
            {
                return true;
            }
        }

        public bool CanSelfUpdate
        {
            get
            {
                return MainStartup.CanSelfUpdate;
            }
        }

        public void Shutdown()
        {
            MainStartup.Shutdown();
        }

        public void Restart(StartupOptions startupOptions)
        {
            MainStartup.Restart();
        }

        public void ConfigureAutoRun(bool autorun)
        {
            var shortcutPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.StartMenu), "Emby", "Emby Server.lnk");

            var startupPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup);

            if (autorun)
            {
                //Copy our shortut into the startup folder for this user
                var targetPath = Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "Emby Server.lnk");
                _fileSystem.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.Copy(shortcutPath, targetPath, true);
            }
            else
            {
                //Remove our shortcut from the startup folder for this user
                _fileSystem.DeleteFile(Path.Combine(startupPath, Path.GetFileName(shortcutPath) ?? "Emby Server.lnk"));
            }
        }

        public INetworkManager CreateNetworkManager(ILogger logger)
        {
            return new NetworkManager(logger);
        }

        public void PreventSystemStandby()
        {
            Standby.PreventSystemStandby();
        }

        public IPowerManagement GetPowerManagement()
        {
            return new WindowsPowerManagement(_logger);
        }

        public FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            var info = new FFMpegInstallInfo();

            info.FFMpegFilename = "ffmpeg.exe";
            info.FFProbeFilename = "ffprobe.exe";
            info.Version = "20160401";
            info.ArchiveType = "7z";
            info.IsEmbedded = true;
            info.DownloadUrls = GetDownloadUrls();

            return info;
        }

        private string[] GetDownloadUrls()
        {
            switch (Environment.SystemArchitecture)
            {
                case Architecture.X86_X64:
                    return new[] { "MediaBrowser.ServerApplication.ffmpeg.ffmpegx64.7z" };
                case Architecture.X86:
                    return new[] { "MediaBrowser.ServerApplication.ffmpeg.ffmpegx86.7z" };
            }

            return new string[] { };
        }
    }
}
