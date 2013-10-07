using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication.Native;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        private static ApplicationHost _appHost;

        private static App _app;

        private static ILogger _logger;

        private static bool _isRunningAsService = false;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            var startFlag = Environment.GetCommandLineArgs().ElementAtOrDefault(1);
            _isRunningAsService = string.Equals(startFlag, "-service", StringComparison.OrdinalIgnoreCase);

            var appPaths = CreateApplicationPaths(_isRunningAsService);

            var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
            logManager.ReloadLogger(LogSeverity.Info);

            var logger = _logger = logManager.GetLogger("Main");

            BeginLog(logger);

            // Install directly
            if (string.Equals(startFlag, "-installservice", StringComparison.OrdinalIgnoreCase))
            {
                logger.Info("Performing service installation");
                InstallService(logger);
                return;
            }

            // Restart with admin rights, then install
            if (string.Equals(startFlag, "-installserviceasadmin", StringComparison.OrdinalIgnoreCase))
            {
                logger.Info("Performing service installation");
                RunServiceInstallation();
                return;
            }

            // Uninstall directly
            if (string.Equals(startFlag, "-uninstallservice", StringComparison.OrdinalIgnoreCase))
            {
                logger.Info("Performing service uninstallation");
                UninstallService(logger);
                return;
            }

            // Restart with admin rights, then uninstall
            if (string.Equals(startFlag, "-uninstallserviceasadmin", StringComparison.OrdinalIgnoreCase))
            {
                logger.Info("Performing service uninstallation");
                RunServiceUninstallation();
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            RunServiceInstallationIfNeeded();

            var currentProcess = Process.GetCurrentProcess();

            if (IsAlreadyRunning(currentProcess))
            {
                logger.Info("Shutting down because another instance of Media Browser Server is already running.");
                return;
            }

            if (PerformUpdateIfNeeded(appPaths, logger))
            {
                logger.Info("Exiting to perform application update.");
                return;
            }

            try
            {
                RunApplication(appPaths, logManager, _isRunningAsService);
            }
            finally
            {
                OnServiceShutdown();
            }
        }

        /// <summary>
        /// Determines whether [is already running] [the specified current process].
        /// </summary>
        /// <param name="currentProcess">The current process.</param>
        /// <returns><c>true</c> if [is already running] [the specified current process]; otherwise, <c>false</c>.</returns>
        private static bool IsAlreadyRunning(Process currentProcess)
        {
            var runningPath = Process.GetCurrentProcess().MainModule.FileName;

            var duplicate = Process.GetProcesses().FirstOrDefault(i =>
                {
                    try
                    {
                        return string.Equals(runningPath, i.MainModule.FileName) && currentProcess.Id != i.Id;
                    }
                    catch (Win32Exception)
                    {
                        return false;
                    }
                });

            if (duplicate != null)
            {
                _logger.Info("Found a duplicate process. Giving it time to exit.");

                if (!duplicate.WaitForExit(5000))
                {
                    _logger.Info("The duplicate process did not exit.");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the application paths.
        /// </summary>
        /// <param name="runAsService">if set to <c>true</c> [run as service].</param>
        /// <returns>ServerApplicationPaths.</returns>
        private static ServerApplicationPaths CreateApplicationPaths(bool runAsService)
        {
            if (runAsService)
            {
#if (RELEASE)
                var systemPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

                var programDataPath = Path.GetDirectoryName(systemPath);

                return new ServerApplicationPaths(programDataPath);
#endif
            }

            return new ServerApplicationPaths();
        }

        /// <summary>
        /// Begins the log.
        /// </summary>
        /// <param name="logger">The logger.</param>
        private static void BeginLog(ILogger logger)
        {
            logger.Info("Media Browser Server started");
            logger.Info("Command line: {0}", string.Join(" ", Environment.GetCommandLineArgs()));

            logger.Info("Server: {0}", Environment.MachineName);
            logger.Info("Operating system: {0}", Environment.OSVersion.ToString());
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="runService">if set to <c>true</c> [run service].</param>
        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, bool runService)
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            MigrateShortcuts(appPaths.RootFolderPath);

            _appHost = new ApplicationHost(appPaths, logManager);

            _app = new App(_appHost, _appHost.LogManager.GetLogger("App"), runService);

            if (runService)
            {
                _app.AppStarted += (sender, args) => StartService(logManager);
            }
            else
            {
                // Not crazy about this but it's the only way to suppress ffmpeg crash dialog boxes
                SetErrorMode(ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOALIGNMENTFAULTEXCEPT |
                             ErrorModes.SEM_NOGPFAULTERRORBOX | ErrorModes.SEM_NOOPENFILEERRORBOX);
            }

            _app.Run();
        }

        static void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLogon)
            {
                BrowserLauncher.OpenDashboard(_appHost.UserManager, _appHost.ServerConfigurationManager, _appHost, _logger);
            }
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        private static void StartService(ILogManager logManager)
        {
            var service = new BackgroundService(logManager.GetLogger("Service"));

            service.Disposed += service_Disposed;

            ServiceBase.Run(service);
        }

        /// <summary>
        /// Handles the Disposed event of the service control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        static void service_Disposed(object sender, EventArgs e)
        {
            OnServiceShutdown();
        }

        private static void OnServiceShutdown()
        {
            _logger.Info("Shutting down");

            _appHost.Dispose();

            if (!_isRunningAsService)
            {
                SetErrorMode(ErrorModes.SYSTEM_DEFAULT);
            }

            _app.Dispatcher.Invoke(_app.Shutdown);
        }

        /// <summary>
        /// Installs the service.
        /// </summary>
        private static void InstallService(ILogger logger)
        {
            var runningPath = Process.GetCurrentProcess().MainModule.FileName;

            try
            {
                ManagedInstallerClass.InstallHelper(new[] { runningPath });

                using (var process = Process.Start("cmd.exe", "/c sc failure " + BackgroundService.Name + " reset= 0 actions= restart/1000/restart/1000/restart/60000"))
                {
                    process.WaitForExit();
                }

                logger.Info("Service installation succeeded");
            }
            catch (Exception ex)
            {
                logger.ErrorException("Uninstall failed", ex);
            }
        }

        /// <summary>
        /// Uninstalls the service.
        /// </summary>
        private static void UninstallService(ILogger logger)
        {
            var runningPath = Process.GetCurrentProcess().MainModule.FileName;

            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", runningPath });

                logger.Info("Service uninstallation succeeded");
            }
            catch (Exception ex)
            {
                logger.ErrorException("Uninstall failed", ex);
            }
        }

        private static void RunServiceInstallationIfNeeded()
        {
            var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == BackgroundService.Name);

            if (ctl == null)
            {
                RunServiceInstallation();
            }
        }

        /// <summary>
        /// Runs the service installation.
        /// </summary>
        private static void RunServiceInstallation()
        {
            var runningPath = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo
            {
                FileName = runningPath,

                Arguments = "-installservice",

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Runs the service uninstallation.
        /// </summary>
        private static void RunServiceUninstallation()
        {
            var runningPath = Process.GetCurrentProcess().MainModule.FileName;

            var startInfo = new ProcessStartInfo
            {
                FileName = runningPath,

                Arguments = "-uninstallservice",

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        /// <summary>
        /// Handles the SessionEnding event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SessionEndingEventArgs"/> instance containing the event data.</param>
        static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason == SessionEndReasons.SystemShutdown || !_isRunningAsService)
            {
                Shutdown();
            }
        }

        /// <summary>
        /// Handles the UnhandledException event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            _logger.ErrorException("UnhandledException", exception);

            _appHost.LogManager.Flush();

            if (!_isRunningAsService)
            {
                _app.OnUnhandledException(exception);
            }

            if (!Debugger.IsAttached)
            {
                Environment.Exit(Marshal.GetHRForException(exception));
            }
        }

        /// <summary>
        /// Performs the update if needed.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logger">The logger.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private static bool PerformUpdateIfNeeded(ServerApplicationPaths appPaths, ILogger logger)
        {
            // Look for the existence of an update archive
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, Constants.MbServerPkgName + ".zip");
            if (File.Exists(updateArchive))
            {
                logger.Info("An update is available from {0}", updateArchive);

                // Update is there - execute update
                try
                {
                    var serviceName = _isRunningAsService ? BackgroundService.Name : string.Empty;
                    new ApplicationUpdater().UpdateApplication(MBApplication.MBServer, appPaths, updateArchive, logger, serviceName);

                    // And just let the app exit so it can update
                    return true;
                }
                catch (Exception e)
                {
                    logger.ErrorException("Error starting updater.", e);

                    MessageBox.Show(string.Format("Error attempting to update application.\n\n{0}\n\n{1}", e.GetType().Name, e.Message));
                }
            }

            return false;
        }

        public static void Shutdown()
        {
            if (_isRunningAsService)
            {
                ShutdownWindowsService();
            }
            else
            {
                ShutdownWindowsApplication();
            }
        }

        public static void Restart()
        {
            _logger.Info("Disposing app host");
            _appHost.Dispose();

            if (!_isRunningAsService)
            {
                _logger.Info("Starting server application");
                RestartWindowsApplication();
            }
            else
            {
                _logger.Info("Calling Enviornment.Exit to tell Windows to restart the server.");
                Environment.Exit(1);
            }
        }

        private static void RestartWindowsApplication()
        {
            System.Windows.Forms.Application.Restart();
        }

        private static void ShutdownWindowsApplication()
        {
            _app.Dispatcher.Invoke(_app.Shutdown);
        }

        private static void ShutdownWindowsService()
        {
            _logger.Info("Stopping background service");
            var service = new ServiceController(BackgroundService.Name);

            service.Refresh();

            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
            }
        }

        /// <summary>
        /// Sets the error mode.
        /// </summary>
        /// <param name="uMode">The u mode.</param>
        /// <returns>ErrorModes.</returns>
        [DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        /// <summary>
        /// Enum ErrorModes
        /// </summary>
        [Flags]
        public enum ErrorModes : uint
        {
            /// <summary>
            /// The SYSTE m_ DEFAULT
            /// </summary>
            SYSTEM_DEFAULT = 0x0,
            /// <summary>
            /// The SE m_ FAILCRITICALERRORS
            /// </summary>
            SEM_FAILCRITICALERRORS = 0x0001,
            /// <summary>
            /// The SE m_ NOALIGNMENTFAULTEXCEPT
            /// </summary>
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            /// <summary>
            /// The SE m_ NOGPFAULTERRORBOX
            /// </summary>
            SEM_NOGPFAULTERRORBOX = 0x0002,
            /// <summary>
            /// The SE m_ NOOPENFILEERRORBOX
            /// </summary>
            SEM_NOOPENFILEERRORBOX = 0x8000
        }

        private static void MigrateShortcuts(string directory)
        {
            Directory.CreateDirectory(directory);

            foreach (var file in Directory.EnumerateFiles(directory, "*.lnk", SearchOption.AllDirectories).ToList())
            {
                MigrateShortcut(file);
            }
        }

        private static void MigrateShortcut(string file)
        {
            var newFile = Path.ChangeExtension(file, ".mblink");

            try
            {
                var resolvedPath = FileSystem.ResolveShortcut(file);

                if (!string.IsNullOrEmpty(resolvedPath))
                {
                    FileSystem.CreateShortcut(newFile, resolvedPath);
                }
            }
            finally
            {
                File.Delete(file);
            }
        }
    }
}
