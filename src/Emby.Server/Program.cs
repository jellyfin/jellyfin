using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Common.Implementations.EnvironmentInfo;
using Emby.Common.Implementations.IO;
using Emby.Common.Implementations.Logging;
using Emby.Common.Implementations.Networking;
using Emby.Drawing;
using Emby.Server.Core;
using Emby.Server.Core.Browser;
using Emby.Server.Implementations.IO;
using MediaBrowser.Common.Net;
using Emby.Server.IO;

namespace Emby.Server
{
    public class Program
    {
        private static ApplicationHost _appHost;

        private static ILogger _logger;

        private static bool _appHostDisposed;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        public static void Main(string[] args)
        {
            var options = new StartupOptions();

            var currentProcess = Process.GetCurrentProcess();
            
            var baseDirectory = System.AppContext.BaseDirectory;
            //var architecturePath = Path.Combine(Path.GetDirectoryName(applicationPath), Environment.Is64BitProcess ? "x64" : "x86");

            //Wand.SetMagickCoderModulePath(architecturePath);

            //var success = SetDllDirectory(architecturePath);

            var appPaths = CreateApplicationPaths(baseDirectory);
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());

            var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
            logManager.ReloadLogger(LogSeverity.Debug);
            logManager.AddConsoleOutput();

            var logger = _logger = logManager.GetLogger("Main");
            
            ApplicationHost.LogEnvironmentInfo(logger, appPaths, true);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            //if (IsAlreadyRunning(applicationPath, currentProcess))
            //{
            //    logger.Info("Shutting down because another instance of Emby Server is already running.");
            //    return;
            //}

            if (PerformUpdateIfNeeded(appPaths, logger))
            {
                logger.Info("Exiting to perform application update.");
                return;
            }

            RunApplication(appPaths, logManager, options);
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

                if (!duplicate.WaitForExit(30000))
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
        private static ServerApplicationPaths CreateApplicationPaths(string appDirectory)
        {
            var resourcesPath = appDirectory;

            return new ServerApplicationPaths(ApplicationPathHelper.GetProgramDataPath(appDirectory), appDirectory, resourcesPath);
        }

        /// <summary>
        /// Gets a value indicating whether this instance can self restart.
        /// </summary>
        /// <value><c>true</c> if this instance can self restart; otherwise, <c>false</c>.</value>
        public static bool CanSelfRestart
        {
            get
            {
                return true;
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
                return false;
            }
        }

        private static readonly TaskCompletionSource<bool> ApplicationTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="options">The options.</param>
        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, StartupOptions options)
        {
            var fileSystem = new ManagedFileSystem(logManager.GetLogger("FileSystem"), true, true, true);

            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

            var imageEncoder = new NullImageEncoder();

            _appHost = new CoreAppHost(appPaths,
                logManager,
                options,
                fileSystem,
                new PowerManagement(),
                "emby.windows.zip",
                new EnvironmentInfo(),
                imageEncoder,
                new CoreSystemEvents(),
                new MemoryStreamFactory(),
                new NetworkManager(logManager.GetLogger("NetworkManager")),
                GenerateCertificate,
                () => "EmbyUser");

            var initProgress = new Progress<double>();

            // Not crazy about this but it's the only way to suppress ffmpeg crash dialog boxes
            SetErrorMode(ErrorModes.SEM_FAILCRITICALERRORS | ErrorModes.SEM_NOALIGNMENTFAULTEXCEPT |
                         ErrorModes.SEM_NOGPFAULTERRORBOX | ErrorModes.SEM_NOOPENFILEERRORBOX);

            var task = _appHost.Init(initProgress);
            Task.WaitAll(task);

            task = task.ContinueWith(new Action<Task>(a => _appHost.RunStartupTasks()), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.AttachedToParent);

            Task.WaitAll(task);

            task = ApplicationTaskCompletionSource.Task;
            Task.WaitAll(task);
        }

        private static void GenerateCertificate(string certPath, string certHost)
        {
            //CertificateGenerator.CreateSelfSignCertificatePfx(certPath, certHost, _logger);
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

            ShowMessageBox("Unhandled exception: " + exception.Message);

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
            return false;
        }

        private static void ShowMessageBox(string msg)
        {

        }

        public static void Shutdown()
        {
            DisposeAppHost();

            //_logger.Info("Calling Application.Exit");
            //Application.Exit();

            _logger.Info("Calling Environment.Exit");
            Environment.Exit(0);

            _logger.Info("Calling ApplicationTaskCompletionSource.SetResult");
            ApplicationTaskCompletionSource.SetResult(true);
        }

        public static void Restart()
        {
            DisposeAppHost();

            // todo: start new instance

            Shutdown();
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
