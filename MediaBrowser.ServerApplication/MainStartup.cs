using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.Server.Startup.Common.Browser;
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
using CommonIO.Windows;
using Emby.Drawing.ImageMagick;
using ImageMagickSharp;
using MediaBrowser.Common.Net;
using MediaBrowser.Server.Implementations.Logging;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        private static ApplicationHost _appHost;

        private static ILogger _logger;

        private static bool _isRunningAsService = false;
        private static bool _appHostDisposed;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        public static void Main()
        {
            var options = new StartupOptions();
            _isRunningAsService = options.ContainsOption("-service");

            var currentProcess = Process.GetCurrentProcess();

            var applicationPath = currentProcess.MainModule.FileName;
            var architecturePath = Path.Combine(Path.GetDirectoryName(applicationPath), Environment.Is64BitProcess ? "x64" : "x86");

            Wand.SetMagickCoderModulePath(architecturePath);

            var success = SetDllDirectory(architecturePath);

            var appPaths = CreateApplicationPaths(applicationPath, _isRunningAsService);

            var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
            logManager.ReloadLogger(LogSeverity.Debug);
            logManager.AddConsoleOutput();

            var logger = _logger = logManager.GetLogger("Main");

            ApplicationHost.LogEnvironmentInfo(logger, appPaths, true);

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

                if (!duplicate.WaitForExit(15000))
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
        /// <param name="applicationPath">The application path.</param>
        /// <param name="runAsService">if set to <c>true</c> [run as service].</param>
        /// <returns>ServerApplicationPaths.</returns>
        private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, bool runAsService)
        {
            var resourcesPath = Path.GetDirectoryName(applicationPath);

            if (runAsService)
            {
                var systemPath = Path.GetDirectoryName(applicationPath);

                var programDataPath = Path.GetDirectoryName(systemPath);

                return new ServerApplicationPaths(programDataPath, applicationPath, resourcesPath);
            }

            return new ServerApplicationPaths(ApplicationPathHelper.GetProgramDataPath(applicationPath), applicationPath, resourcesPath);
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
            var fileSystem = new WindowsFileSystem(new PatternsLogger(logManager.GetLogger("FileSystem")));
            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));
            //fileSystem.AddShortcutHandler(new LnkShortcutHandler(fileSystem));

            var nativeApp = new WindowsApp(fileSystem, _logger)
            {
                IsRunningAsService = runService
            };

            _appHost = new ApplicationHost(appPaths,
                logManager,
                options,
                fileSystem,
                "emby.windows.zip",
                nativeApp);

            var initProgress = new Progress<double>();

            if (!runService)
            {
                if (!options.ContainsOption("-nosplash")) ShowSplashScreen(_appHost.ApplicationVersion, initProgress, logManager.GetLogger("Splash"));

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

                task = InstallVcredistIfNeeded(_appHost, _logger);
                Task.WaitAll(task);

                task = InstallFrameworkV46IfNeeded(_logger);
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
        private static TaskScheduler _mainTaskScheduler;
        private static void ShowTrayIcon()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            _serverNotifyIcon = new ServerNotifyIcon(_appHost.LogManager, _appHost, _appHost.ServerConfigurationManager, _appHost.LocalizationManager);
            _mainTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
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
                BrowserLauncher.OpenDashboard(_appHost);
            }
        }

        public static void Invoke(Action action)
        {
            if (_isRunningAsService)
            {
                action();
            }
            else
            {
                Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, _mainTaskScheduler ?? TaskScheduler.Current);
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

            DisposeAppHost();
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
            var serviceName = BackgroundService.GetExistingServiceName();
            var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);

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

            new UnhandledExceptionWriter(_appHost.ServerConfigurationManager.ApplicationPaths, _logger, _appHost.LogManager).Log(exception);

            if (!_isRunningAsService)
            {
                MessageBox.Show("Unhandled exception: " + exception.Message);
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
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, "MBServer" + ".zip");
            if (File.Exists(updateArchive))
            {
                logger.Info("An update is available from {0}", updateArchive);

                // Update is there - execute update
                try
                {
                    var serviceName = _isRunningAsService ? BackgroundService.GetExistingServiceName() : string.Empty;
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
                DisposeAppHost();

                ShutdownWindowsApplication();
            }
        }

        public static void Restart()
        {
            DisposeAppHost();

            if (!_isRunningAsService)
            {
                //_logger.Info("Hiding server notify icon");
                //_serverNotifyIcon.Visible = false;

                _logger.Info("Starting new instance");
                //Application.Restart();
                Process.Start(_appHost.ServerConfigurationManager.ApplicationPaths.ApplicationPath);

                ShutdownWindowsApplication();
            }
        }

        private static void DisposeAppHost()
        {
            if (!_appHostDisposed)
            {
                _logger.Info("Disposing app host");

                _appHostDisposed = true;
                _appHost.Dispose();
            }
        }

        private static void ShutdownWindowsApplication()
        {
            //_logger.Info("Calling Application.Exit");
            //Application.Exit();

            _logger.Info("Calling Environment.Exit");
            Environment.Exit(0);

            _logger.Info("Calling ApplicationTaskCompletionSource.SetResult");
            ApplicationTaskCompletionSource.SetResult(true);
        }

        private static void ShutdownWindowsService()
        {
            _logger.Info("Stopping background service");
            var service = new ServiceController(BackgroundService.GetExistingServiceName());

            service.Refresh();

            if (service.Status == ServiceControllerStatus.Running)
            {
                service.Stop();
            }
        }

        private static async Task InstallFrameworkV46IfNeeded(ILogger logger)
        {
            bool installFrameworkV46 = false;

            try
            {
                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                    .OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\"))
                {
                    if (ndpKey != null && ndpKey.GetValue("Release") != null)
                    {
                        if ((int)ndpKey.GetValue("Release") <= 393295)
                        {
                            //Found framework V4, but not yet V4.6
                            installFrameworkV46 = true;
                        }
                    }
                    else
                    {
                        //Nothing found in the registry for V4
                        installFrameworkV46 = true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting .NET Framework version", ex);
            }

            _logger.Info(".NET Framework 4.6 found: {0}", !installFrameworkV46);

            if (installFrameworkV46)
            {
                try
                {
                    await InstallFrameworkV46().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.ErrorException("Error installing .NET Framework version 4.6", ex);
                }
            }
        }

        private static async Task InstallFrameworkV46()
        {
            var httpClient = _appHost.HttpClient;

            var tmp = await httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = "https://github.com/MediaBrowser/Emby.Resources/raw/master/netframeworkV46/NDP46-KB3045560-Web.exe",
                Progress = new Progress<double>()

            }).ConfigureAwait(false);

            var exePath = Path.ChangeExtension(tmp, ".exe");
            File.Copy(tmp, exePath);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false,
                Arguments = "/q /norestart"
            };


            _logger.Info("Running {0}", startInfo.FileName);

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                //process.ExitCode
                /*
                0 --> Installation completed successfully.
                1602 --> The user canceled installation.
                1603 --> A fatal error occurred during installation.
                1641 --> A restart is required to complete the installation. This message indicates success.
                3010 --> A restart is required to complete the installation. This message indicates success.
                5100 --> The user's computer does not meet system requirements.
                 */
            }
        }

        private static async Task InstallVcredistIfNeeded(ApplicationHost appHost, ILogger logger)
        {
            try
            {
                var version = ImageMagickEncoder.GetVersion();
                return;
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error loading ImageMagick", ex);
            }

            try
            {
                await InstallVcredist().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error installing Visual Studio C++ runtime", ex);
            }
        }

        private async static Task InstallVcredist()
        {
            var httpClient = _appHost.HttpClient;

            var tmp = await httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = GetVcredistUrl(),
                Progress = new Progress<double>()

            }).ConfigureAwait(false);

            var exePath = Path.ChangeExtension(tmp, ".exe");
            File.Copy(tmp, exePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
                ErrorDialog = false
            };

            _logger.Info("Running {0}", startInfo.FileName);

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
            }
        }

        private static string GetVcredistUrl()
        {
            if (Environment.Is64BitProcess)
            {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_x64.exe";
            }

            // TODO: ARM url - https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_arm.exe

            return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_x86.exe";
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
