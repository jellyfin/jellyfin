using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Mono.Native;
using MediaBrowser.Server.Startup.Common;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Server.Implementations.Logging;

namespace MediaBrowser.Server.Mono
{
    public class MainClass
    {
        private static ApplicationHost _appHost;

        private static ILogger _logger;

        public static void Main(string[] args)
        {
            var applicationPath = Assembly.GetEntryAssembly().Location;

            var options = new StartupOptions();

            // Allow this to be specified on the command line.
            var customProgramDataPath = options.GetOption("-programdata");

            var appPaths = CreateApplicationPaths(applicationPath, customProgramDataPath);

            var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
            logManager.ReloadLogger(LogSeverity.Info);
            logManager.AddConsoleOutput();

            var logger = _logger = logManager.GetLogger("Main");

            ApplicationHost.LogEnvironmentInfo(logger, appPaths, true);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                RunApplication(appPaths, logManager, options);
            }
            finally
            {
                logger.Info("Shutting down");

                _appHost.Dispose();
            }
        }

        private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, string programDataPath)
        {
            if (string.IsNullOrEmpty(programDataPath))
            {
                programDataPath = ApplicationPathHelper.GetProgramDataPath(applicationPath);
            }

            return new ServerApplicationPaths(programDataPath, applicationPath, Path.GetDirectoryName(applicationPath));
        }

        private static readonly TaskCompletionSource<bool> ApplicationTaskCompletionSource = new TaskCompletionSource<bool>();

        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, StartupOptions options)
        {
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;

            // Allow all https requests
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            var fileSystem = new ManagedFileSystem(new PatternsLogger(logManager.GetLogger("FileSystem")), false, false);
            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

            var nativeApp = new NativeApp(options, logManager.GetLogger("App"));

            _appHost = new ApplicationHost(appPaths, logManager, options, fileSystem, "emby.mono.zip", nativeApp);

            if (options.ContainsOption("-v"))
            {
                Console.WriteLine(_appHost.ApplicationVersion.ToString());
                return;
            }

            Console.WriteLine("appHost.Init");

            var initProgress = new Progress<double>();

            var task = _appHost.Init(initProgress);
            Task.WaitAll(task);

            Console.WriteLine("Running startup tasks");

            task = _appHost.RunStartupTasks();
            Task.WaitAll(task);

            task = ApplicationTaskCompletionSource.Task;

            Task.WaitAll(task);
        }

        /// <summary>
        /// Handles the SessionEnding event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SessionEndingEventArgs"/> instance containing the event data.</param>
        static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason == SessionEndReasons.SystemShutdown)
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

            if (!Debugger.IsAttached)
            {
                Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
            }
        }

        public static void Shutdown()
        {
            ApplicationTaskCompletionSource.SetResult(true);
        }

        public static void Restart(StartupOptions startupOptions)
        {
            _logger.Info("Disposing app host");
            _appHost.Dispose();

            _logger.Info("Starting new instance");

            string module = startupOptions.GetOption("-restartpath");
            string commandLineArgsString = startupOptions.GetOption("-restartargs") ?? string.Empty;

            if (string.IsNullOrWhiteSpace(module))
            {
                module = Environment.GetCommandLineArgs().First();
            }
            if (!startupOptions.ContainsOption("-restartargs"))
            {
                var args = Environment.GetCommandLineArgs()
                                .Skip(1)
                                .Select(NormalizeCommandLineArgument);

                commandLineArgsString = string.Join(" ", args.ToArray());
            }

            _logger.Info("Executable: {0}", module);
            _logger.Info("Arguments: {0}", commandLineArgsString);

            Process.Start(module, commandLineArgsString);

            _logger.Info("Calling Environment.Exit");
            Environment.Exit(0);
        }

        private static string NormalizeCommandLineArgument(string arg)
        {
            if (arg.IndexOf(" ", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return arg;
            }

            return "\"" + arg + "\"";
        }
    }

    class NoCheckCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }
    }
}
