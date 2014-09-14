using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication.IO;
using MediaBrowser.ServerApplication.Native;
using MediaBrowser.ServerApplication.Splash;
using MediaBrowser.ServerApplication.Updates;
using Microsoft.Win32;
using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        private static ApplicationHost _appHost;

        private static ILogger _logger;

        private static bool _isRunningAsService = false;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        public static void Main()
        {
            var options = new StartupOptions();
            _isRunningAsService = options.ContainsOption("-service");

            var applicationPath = Process.GetCurrentProcess().MainModule.FileName;

            var appPaths = CreateApplicationPaths(applicationPath, _isRunningAsService);

            var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
            logManager.ReloadLogger(LogSeverity.Debug);
            logManager.AddConsoleOutput();

            var logger = _logger = logManager.GetLogger("Main");

            BeginLog(logger, appPaths);

            // Install directly
            if (options.ContainsOption("-installservice"))
            {
                logger.Info("Performing service installation");
                InstallService(applicationPath, logger);
                return;
            }

            // Restart with admin rights, then install
            if (options.ContainsOption("-installserviceasadmin"))
            {
                logger.Info("Performing service installation");
                RunServiceInstallation(applicationPath);
                return;
            }

            // Uninstall directly
            if (options.ContainsOption("-uninstallservice"))
            {
                logger.Info("Performing service uninstallation");
                UninstallService(applicationPath, logger);
                return;
            }

            // Restart with admin rights, then uninstall
            if (options.ContainsOption("-uninstallserviceasadmin"))
            {
                logger.Info("Performing service uninstallation");
                RunServiceUninstallation(applicationPath);
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            RunServiceInstallationIfNeeded(applicationPath);

            var currentProcess = Process.GetCurrentProcess();

            if (IsAlreadyRunning(applicationPath, currentProcess))
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
                RunApplication(appPaths, logManager, _isRunningAsService, options);
            }
            finally
            {
                OnServiceShutdown();
            }
        }

        /// <summary>
        /// Determines whether [is already running] [the specified current process].
        /// </summary>
        /// <param name="applicationPath">The application path.</param>
        /// <param name="currentProcess">The current process.</param>
        /// <returns><c>true</c> if [is already running] [the specified current process]; otherwise, <c>false</c>.</returns>
        private static bool IsAlreadyRunning(string applicationPath, Process currentProcess)
        {
            var filename = Path.GetFileName(applicationPath);

            var duplicate = Process.GetProcesses().FirstOrDefault(i =>
            {
                try
                {
                    return string.Equals(filename, Path.GetFileName(i.MainModule.FileName)) && currentProcess.Id != i.Id;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            if (duplicate != null)
            {
                _logger.Info("Found a duplicate process. Giving it time to exit.");

                if (!duplicate.WaitForExit(10000))
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
        private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, bool runAsService)
        {
            if (runAsService)
            {
                var systemPath = Path.GetDirectoryName(applicationPath);

                var programDataPath = Path.GetDirectoryName(systemPath);

                return new ServerApplicationPaths(programDataPath, applicationPath);
            }

            return new ServerApplicationPaths(applicationPath);
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public static bool CanSelfRestart
        {
            get
            {
                return !_isRunningAsService;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public static bool CanSelfUpdate
        {
            get
            {
                return !_isRunningAsService;
            }
        }

        /// <summary>
        /// Begins the log.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="appPaths">The app paths.</param>
        private static void BeginLog(ILogger logger, IApplicationPaths appPaths)
        {
            logger.Info("Media Browser Server started");
            ApplicationHost.LogEnvironmentInfo(logger, appPaths);
        }

        private static readonly TaskCompletionSource<bool> ApplicationTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="runService">if set to <c>true</c> [run service].</param>
        /// <param name="options">The options.</param>
        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, bool runService, StartupOptions options)
        {
            _appHost = new ApplicationHost(appPaths, logManager, true, runService, options);

            var initProgress = new Progress<double>();

            if (!runService)
            {
                ShowSplashScreen(_appHost.ApplicationVersion, initProgress, logManager.GetLogger("Splash"));
                
                // Not crazy about this but it's the only way to suppress ffmpeg crash dialog boxes
                SetErrorMode(ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOALIGNMENTFAULTEXCEPT |
                             ErrorModes.SEM_NOGPFAULTERRORBOX | ErrorModes.SEM_NOOPENFILEERRORBOX);
            }


            var task = _appHost.Init(initProgress);
            task = task.ContinueWith(new Action<Task>(a => _appHost.RunStartupTasks()));

            if (runService)
            {
                StartService(logManager);
            }
            else
            {
                Task.WaitAll(task);

                SystemEvents.SessionEnding += SystemEvents_SessionEnding;
                SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
   
                HideSplashScreen();

                ShowTrayIcon();
                
                task = ApplicationTaskCompletionSource.Task;
                Task.WaitAll(task);
            }
        }

        private static ServerNotifyIcon _serverNotifyIcon;
        private static void ShowTrayIcon()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            _serverNotifyIcon = new ServerNotifyIcon(_appHost.LogManager, _appHost, _appHost.ServerConfigurationManager, _appHost.UserManager, _appHost.LibraryManager, _appHost.JsonSerializer, _appHost.LocalizationManager, _appHost.UserViewManager);
            Application.Run();
        }

        private static SplashForm _splash;
        private static Thread _splashThread;
        private static void ShowSplashScreen(Version appVersion, Progress<double> progress, ILogger logger)
        {
            var thread = new Thread(() =>
            {
                _splash = new SplashForm(appVersion, progress);

                _splash.ShowDialog();
            });
           
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            _splashThread = thread;
        }

        private static void HideSplashScreen()
        {
            if (_splash != null)
            {
                Action act = () =>
                {
                    _splash.Close();
                    _splashThread = null;
                };

                _splash.Invoke(act);
            }
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
            ApplicationTaskCompletionSource.SetResult(true);
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
        }

        /// <summary>
        /// Installs the service.
        /// </summary>
        private static void InstallService(string applicationPath, ILogger logger)
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { applicationPath });

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
        private static void UninstallService(string applicationPath, ILogger logger)
        {
            try
            {
                ManagedInstallerClass.InstallHelper(new[] { "/u", applicationPath });

                logger.Info("Service uninstallation succeeded");
            }
            catch (Exception ex)
            {
                logger.ErrorException("Uninstall failed", ex);
            }
        }

        private static void RunServiceInstallationIfNeeded(string applicationPath)
        {
            var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == BackgroundService.Name);

            if (ctl == null)
            {
                RunServiceInstallation(applicationPath);
            }
        }

        /// <summary>
        /// Runs the service installation.
        /// </summary>
        private static void RunServiceInstallation(string applicationPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = applicationPath,

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
        private static void RunServiceUninstallation(string applicationPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = applicationPath,

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

            LogUnhandledException(exception);

            _appHost.LogManager.Flush();

            if (!_isRunningAsService)
            {
                MessageBox.Show("Unhandled exception: " + exception.Message);
            }

            if (!Debugger.IsAttached)
            {
                Environment.Exit(Marshal.GetHRForException(exception));
            }
        }

        private static void LogUnhandledException(Exception ex)
        {
            _logger.ErrorException("UnhandledException", ex);

            var path = Path.Combine(_appHost.ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, "unhandled_" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var builder = LogHelper.GetLogMessage(ex);

            File.WriteAllText(path, builder.ToString());
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
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, "MBServer" + ".zip");
            if (File.Exists(updateArchive))
            {
                logger.Info("An update is available from {0}", updateArchive);

                // Update is there - execute update
                try
                {
                    var serviceName = _isRunningAsService ? BackgroundService.Name : string.Empty;
                    new ApplicationUpdater().UpdateApplication(appPaths, updateArchive, logger, serviceName);

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
                _logger.Info("Hiding server notify icon");
                _serverNotifyIcon.Visible = false;

                _logger.Info("Executing windows forms restart");
                //Application.Restart();
                Process.Start(_appHost.ServerConfigurationManager.ApplicationPaths.ApplicationPath);

                _logger.Info("Calling Application.Exit");
                Environment.Exit(0);
            }
        }

        private static void ShutdownWindowsApplication()
        {
            _logger.Info("Hiding server notify icon");
            _serverNotifyIcon.Visible = false;

            _logger.Info("Calling Application.Exit");
            Application.Exit();

            _logger.Info("Calling ApplicationTaskCompletionSource.SetResult");
            ApplicationTaskCompletionSource.SetResult(true);
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
    }
}
