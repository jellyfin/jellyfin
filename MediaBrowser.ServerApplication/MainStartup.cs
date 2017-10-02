using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.ServerApplication.Native;
using MediaBrowser.ServerApplication.Splash;
using MediaBrowser.ServerApplication.Updates;
using Microsoft.Win32;
using System;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emby.Drawing;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Browser;
using Emby.Server.Implementations.EnvironmentInfo;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Logging;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
using SystemEvents = Emby.Server.Implementations.SystemEvents;

namespace MediaBrowser.ServerApplication
{
    public class MainStartup
    {
        private static IServerApplicationPaths _appPaths;
        private static ILogManager _logManager;

        private static ILogger _logger;

        public static bool IsRunningAsService = false;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        public static string ApplicationPath;

        private static IFileSystem FileSystem;
        private static bool _restartOnShutdown;

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            var options = new StartupOptions(Environment.GetCommandLineArgs());
            IsRunningAsService = options.ContainsOption("-service");

            var currentProcess = Process.GetCurrentProcess();

            ApplicationPath = currentProcess.MainModule.FileName;
            var architecturePath = Path.Combine(Path.GetDirectoryName(ApplicationPath), Environment.Is64BitProcess ? "x64" : "x86");

            var success = SetDllDirectory(architecturePath);

            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());

            var appPaths = CreateApplicationPaths(ApplicationPath, IsRunningAsService);
            _appPaths = appPaths;

            using (var logManager = new SimpleLogManager(appPaths.LogDirectoryPath, "server"))
            {
                _logManager = logManager;

                logManager.ReloadLogger(LogSeverity.Debug);
                logManager.AddConsoleOutput();

                var logger = _logger = logManager.GetLogger("Main");

                ApplicationHost.LogEnvironmentInfo(logger, appPaths, true);

                // Uninstall directly
                if (options.ContainsOption("-uninstallservice"))
                {
                    logger.Info("Performing service uninstallation");
                    UninstallService(ApplicationPath, logger);
                    return;
                }

                // Restart with admin rights, then uninstall
                if (options.ContainsOption("-uninstallserviceasadmin"))
                {
                    logger.Info("Performing service uninstallation");
                    RunServiceUninstallation(ApplicationPath);
                    return;
                }

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                if (IsAlreadyRunning(ApplicationPath, currentProcess))
                {
                    logger.Info("Shutting down because another instance of Emby Server is already running.");
                    return;
                }

                if (PerformUpdateIfNeeded(appPaths, logger))
                {
                    logger.Info("Exiting to perform application update.");
                    return;
                }

                RunApplication(appPaths, logManager, IsRunningAsService, options);

                logger.Info("Shutdown complete");

                if (_restartOnShutdown)
                {
                    logger.Info("Starting new server process");
                    var restartCommandLine = GetRestartCommandLine();

                    Process.Start(restartCommandLine.Item1, restartCommandLine.Item2);
                }
            }
        }

        public static Tuple<string, string> GetRestartCommandLine()
        {
            var currentProcess = Process.GetCurrentProcess();
            var processModulePath = currentProcess.MainModule.FileName;

            return new Tuple<string, string>(processModulePath, Environment.CommandLine);
        }

        private static bool IsServiceInstalled()
        {
            try
            {
                var serviceName = BackgroundService.GetExistingServiceName();
                var ctl = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);

                return ctl != null;
            }
            catch
            {
                return false;
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
            var duplicate = Process.GetProcesses().FirstOrDefault(i =>
            {
                try
                {
                    if (currentProcess.Id == i.Id)
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                try
                {
                    //_logger.Info("Module: {0}", i.MainModule.FileName);
                    if (string.Equals(applicationPath, i.MainModule.FileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            });

            if (duplicate != null)
            {
                _logger.Info("Found a duplicate process. Giving it time to exit.");

                if (!duplicate.WaitForExit(40000))
                {
                    _logger.Info("The duplicate process did not exit.");
                    return true;
                }
            }

            if (!IsRunningAsService)
            {
                return IsAlreadyRunningAsService(applicationPath);
            }

            return false;
        }

        private static bool IsAlreadyRunningAsService(string applicationPath)
        {
            try
            {
                var serviceName = BackgroundService.GetExistingServiceName();

                WqlObjectQuery wqlObjectQuery = new WqlObjectQuery(string.Format("SELECT * FROM Win32_Service WHERE State = 'Running' AND Name = '{0}'", serviceName));
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(wqlObjectQuery);
                ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();

                foreach (ManagementObject managementObject in managementObjectCollection)
                {
                    var obj = managementObject.GetPropertyValue("PathName");
                    if (obj == null)
                    {
                        continue;
                    }
                    var path = obj.ToString();

                    _logger.Info("Service path: {0}", path);
                    // Need to use indexOf instead of equality because the path will have the full service command line
                    if (path.IndexOf(applicationPath, StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        _logger.Info("The windows service is already running");
                        MessageBox.Show("Emby Server is already running as a Windows Service. Only one instance is allowed at a time. To run as a tray icon, shut down the Windows Service.");
                        return true;
                    }
                }
            }
            catch (COMException)
            {
                // Catch errors thrown due to WMI not being initialized
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
            var appFolderPath = Path.GetDirectoryName(applicationPath);

            var resourcesPath = Path.GetDirectoryName(applicationPath);

            if (runAsService && IsServiceInstalled())
            {
                var systemPath = Path.GetDirectoryName(applicationPath);

                var programDataPath = Path.GetDirectoryName(systemPath);

                return new ServerApplicationPaths(programDataPath, appFolderPath, resourcesPath);
            }

            return new ServerApplicationPaths(ApplicationPathHelper.GetProgramDataPath(applicationPath), appFolderPath, resourcesPath);
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public static bool CanSelfRestart
        {
            get
            {
                if (IsRunningAsService)
                {
                    return false;
                }
                else
                {
                    return true;
                }
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
#if DEBUG
                return false;
#endif

                if (IsRunningAsService)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="runService">if set to <c>true</c> [run service].</param>
        /// <param name="options">The options.</param>
        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, bool runService, StartupOptions options)
        {
            var environmentInfo = new EnvironmentInfo();

            var fileSystem = new ManagedFileSystem(logManager.GetLogger("FileSystem"), environmentInfo, appPaths.TempDirectory);

            FileSystem = fileSystem;

            using (var appHost = new WindowsAppHost(appPaths,
                logManager,
                options,
                fileSystem,
                new PowerManagement(),
                "emby.windows.zip",
                environmentInfo,
                new NullImageEncoder(),
                new SystemEvents(logManager.GetLogger("SystemEvents")),
                new Networking.NetworkManager(logManager.GetLogger("NetworkManager"))))
            {
                var initProgress = new Progress<double>();

                if (!runService)
                {
                    if (!options.ContainsOption("-nosplash")) ShowSplashScreen(appHost.ApplicationVersion, initProgress, logManager.GetLogger("Splash"));

                    // Not crazy about this but it's the only way to suppress ffmpeg crash dialog boxes
                    SetErrorMode(ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOALIGNMENTFAULTEXCEPT |
                                 ErrorModes.SEM_NOGPFAULTERRORBOX | ErrorModes.SEM_NOOPENFILEERRORBOX);
                }

                var task = appHost.Init(initProgress);
                Task.WaitAll(task);

                if (!runService)
                {
                    task = InstallVcredist2013IfNeeded(appHost.HttpClient, _logger);
                    Task.WaitAll(task);

                    // needed by skia
                    task = InstallVcredist2015IfNeeded(appHost.HttpClient, _logger);
                    Task.WaitAll(task);
                }

                // set image encoder here
                appHost.ImageProcessor.ImageEncoder = ImageEncoderHelper.GetImageEncoder(_logger, logManager, fileSystem, options, () => appHost.HttpClient, appPaths);

                task = task.ContinueWith(new Action<Task>(a => appHost.RunStartupTasks()), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);

                if (runService && IsServiceInstalled())
                {
                    StartService(logManager);
                }
                else
                {
                    Task.WaitAll(task);

                    HideSplashScreen();

                    ShowTrayIcon(appHost);
                }
            }
        }

        private static ServerNotifyIcon _serverNotifyIcon;
        private static TaskScheduler _mainTaskScheduler;
        private static void ShowTrayIcon(ApplicationHost appHost)
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            _serverNotifyIcon = new ServerNotifyIcon(appHost.LogManager, appHost, appHost.ServerConfigurationManager, appHost.LocalizationManager);
            _mainTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Application.Run();
        }

        internal static SplashForm _splash;
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

        public static void Invoke(Action action)
        {
            if (IsRunningAsService)
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

            ServiceBase.Run(service);
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
        /// Handles the UnhandledException event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            new UnhandledExceptionWriter(_appPaths, _logger, _logManager, FileSystem, new ConsoleLogger()).Log(exception);

            if (!IsRunningAsService)
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
            // Not supported
            if (IsRunningAsService)
            {
                return false;
            }

            // Look for the existence of an update archive
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, "MBServer" + ".zip");
            if (File.Exists(updateArchive))
            {
                logger.Info("An update is available from {0}", updateArchive);

                // Update is there - execute update
                try
                {
                    var serviceName = IsRunningAsService ? BackgroundService.GetExistingServiceName() : string.Empty;
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
            if (IsRunningAsService && IsServiceInstalled())
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
            if (IsRunningAsService)
            {
            }
            else
            {
                _restartOnShutdown = true;
                ShutdownWindowsApplication();
            }
        }

        private static void ShutdownWindowsApplication()
        {
            if (_serverNotifyIcon != null)
            {
                _serverNotifyIcon.Dispose();
                _serverNotifyIcon = null;
            }

            _logger.Info("Calling Application.Exit");
            Application.Exit();
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

        private static async Task InstallVcredist2013IfNeeded(IHttpClient httpClient, ILogger logger)
        {
            // Reference 
            // http://stackoverflow.com/questions/12206314/detect-if-visual-c-redistributable-for-visual-studio-2012-is-installed

            try
            {
                var subkey = Environment.Is64BitProcess
                    ? "SOFTWARE\\WOW6432Node\\Microsoft\\VisualStudio\\12.0\\VC\\Runtimes\\x64"
                    : "SOFTWARE\\Microsoft\\VisualStudio\\12.0\\VC\\Runtimes\\x86";

                using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                    .OpenSubKey(subkey))
                {
                    if (ndpKey != null && ndpKey.GetValue("Version") != null)
                    {
                        var installedVersion = ((string)ndpKey.GetValue("Version")).TrimStart('v');
                        if (installedVersion.StartsWith("12", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting .NET Framework version", ex);
                return;
            }

            try
            {
                await InstallVcredist(GetVcredist2013Url(), httpClient).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error installing Visual Studio C++ runtime", ex);
            }
        }

        private static string GetVcredist2013Url()
        {
            if (Environment.Is64BitProcess)
            {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_x64.exe";
            }

            // TODO: ARM url - https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_arm.exe

            return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2013/vcredist_x86.exe";
        }

        private static async Task InstallVcredist2015IfNeeded(IHttpClient httpClient, ILogger logger)
        {
            // Reference 
            // http://stackoverflow.com/questions/12206314/detect-if-visual-c-redistributable-for-visual-studio-2012-is-installed

            try
            {
                RegistryKey key;

                if (Environment.Is64BitProcess)
                {
                    key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                       .OpenSubKey("SOFTWARE\\Classes\\Installer\\Dependencies\\{d992c12e-cab2-426f-bde3-fb8c53950b0d}");

                    if (key == null)
                    {
                        key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                            .OpenSubKey("SOFTWARE\\WOW6432Node\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64");
                    }
                }
                else
                {
                    key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                        .OpenSubKey("SOFTWARE\\Classes\\Installer\\Dependencies\\{e2803110-78b3-4664-a479-3611a381656a}");

                    if (key == null)
                    {
                        key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                            .OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x86");
                    }
                }

                if (key != null)
                {
                    using (key)
                    {
                        var version = key.GetValue("Version");
                        if (version != null)
                        {
                            var installedVersion = ((string)version).TrimStart('v');
                            if (installedVersion.StartsWith("14", StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error getting .NET Framework version", ex);
                return;
            }

            try
            {
                await InstallVcredist(GetVcredist2015Url(), httpClient).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.ErrorException("Error installing Visual Studio C++ runtime", ex);
            }
        }

        private static string GetVcredist2015Url()
        {
            if (Environment.Is64BitProcess)
            {
                return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2015/vc_redist.x64.exe";
            }

            // TODO: ARM url - https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2015/vcredist_arm.exe

            return "https://github.com/MediaBrowser/Emby.Resources/raw/master/vcredist2015/vc_redist.x86.exe";
        }

        private async static Task InstallVcredist(string url, IHttpClient httpClient)
        {
            var tmp = await httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = url,
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
