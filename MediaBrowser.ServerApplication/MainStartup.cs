using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using Microsoft.Win32;
using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        /// <summary>
        /// The single instance mutex
        /// </summary>
        private static Mutex _singleInstanceMutex;

        private static ApplicationHost _appHost;

        private static App _app;

        private static BackgroundService _backgroundService;

        private static ILogger _logger;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            var startFlag = Environment.GetCommandLineArgs().ElementAtOrDefault(1);
            var runService = string.Equals(startFlag, "-service", StringComparison.OrdinalIgnoreCase);

            var appPaths = CreateApplicationPaths(runService);

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

            bool createdNew;

            var runningPath = Process.GetCurrentProcess().MainModule.FileName.Replace(Path.DirectorySeparatorChar.ToString(), string.Empty);

            _singleInstanceMutex = new Mutex(true, @"Local\" + runningPath, out createdNew);

            if (!createdNew)
            {
                _singleInstanceMutex = null;
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
                RunApplication(appPaths, logManager, runService);
            }
            finally
            {
                logger.Info("Shutting down");

                ReleaseMutex(logger);

                _appHost.Dispose();
            }
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

            var commandLineArgs = Environment.GetCommandLineArgs();

            _appHost = new ApplicationHost(appPaths, logManager);

            _app = new App(_appHost, _appHost.LogManager.GetLogger("App"), runService);

            if (runService)
            {
                _app.AppStarted += (sender, args) => StartService(logManager);
            }

            _app.Run();
        }

        /// <summary>
        /// Starts the service.
        /// </summary>
        private static void StartService(ILogManager logManager)
        {
            var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == BackgroundService.Name);

            if (ctl == null)
            {
                RunServiceInstallation();
            }

            var service = new BackgroundService(logManager.GetLogger("Service"));

            service.Disposed += service_Disposed;

            ServiceBase.Run(service);

            _backgroundService = service;
        }

        /// <summary>
        /// Handles the Disposed event of the service control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        static async void service_Disposed(object sender, EventArgs e)
        {
            await _appHost.Shutdown();
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
            if (e.Reason == SessionEndReasons.SystemShutdown || _backgroundService == null)
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

            if (_backgroundService == null)
            {
                _app.OnUnhandledException(exception);
            }

            if (!Debugger.IsAttached)
            {
                Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
            }
        }

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        internal static void ReleaseMutex(ILogger logger)
        {
            if (_singleInstanceMutex == null)
            {
                return;
            }

            logger.Debug("Releasing mutex");

            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Close();
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
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
                    new ApplicationUpdater().UpdateApplication(MBApplication.MBServer, appPaths, updateArchive);

                    // And just let the app exit so it can update
                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Error attempting to update application.\n\n{0}\n\n{1}", e.GetType().Name, e.Message));
                }
            }

            return false;
        }

        public static void Shutdown()
        {
            if (_backgroundService != null)
            {
                _backgroundService.Stop();
            }
            else
            {
                _app.Dispatcher.Invoke(_app.Shutdown);
            }
        }

        public static void Restart()
        {
            // Second instance will start first, so release the mutex and dispose the http server ahead of time
            _app.Dispatcher.Invoke(() => ReleaseMutex(_logger));

            _appHost.Dispose();

            RestartInternal();

            _app.Dispatcher.Invoke(_app.Shutdown);
        }

        private static void RestartInternal()
        {
            if (_backgroundService == null)
            {
                System.Windows.Forms.Application.Restart();
            }
            else
            {
                //var controller = new ServiceController()
                //{
                //    ServiceName = BackgroundService.Name
                //};
            }
        }
    }
}
