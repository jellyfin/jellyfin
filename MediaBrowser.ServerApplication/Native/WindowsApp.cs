using System;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Networking;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CommonIO;
using MediaBrowser.Model.System;
using MediaBrowser.Server.Implementations.Persistence;
using MediaBrowser.Server.Startup.Common.FFMpeg;
using OperatingSystem = MediaBrowser.Server.Startup.Common.OperatingSystem;

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
                    SystemArchitecture = System.Environment.Is64BitOperatingSystem ? Architecture.X64 : Architecture.X86,
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
            MainStartup.Invoke(Standby.PreventSleep);
        }

        public void AllowSystemStandby()
        {
            MainStartup.Invoke(Standby.AllowSleep);
        }

        public FFMpegInstallInfo GetFfmpegInstallInfo()
        {
            var info = new FFMpegInstallInfo();

            info.FFMpegFilename = "ffmpeg.exe";
            info.FFProbeFilename = "ffprobe.exe";
            info.Version = "0";

            return info;
        }

        public void LaunchUrl(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true,
            };

            process.Exited += ProcessExited;

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error launching url: {0}", ex, url);

                throw;
            }
        }

        public IDbConnector GetDbConnector()
        {
            return new DbConnector(_logger);
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        public void EnableLoopback(string appName)
        {
            LoopUtil.Run(appName);
        }

        public bool PortsRequireAuthorization(string applicationPath)
        {
            var appNameSrch = Path.GetFileName(applicationPath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",

                Arguments = "advfirewall firewall show rule \"" + appNameSrch + "\"",

                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.Start();

                try
                {
                    var data = process.StandardOutput.ReadToEnd() ?? string.Empty;

                    if (data.IndexOf("Block", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        _logger.Info("Found potential windows firewall rule blocking Emby Server: " + data);
                    }

                    //var parts = data.Split('\n');

                    //return parts.Length > 4;
                    //return Confirm();
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error querying windows firewall", ex);

                    // Hate having to do this
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex1)
                    {
                        _logger.ErrorException("Error killing process", ex1);
                    }

                    throw;
                }
            }
        }
    }
}