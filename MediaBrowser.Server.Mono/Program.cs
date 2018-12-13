using MediaBrowser.Server.Mono.Native;
using MediaBrowser.Server.Startup.Common;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.Drawing;
using Emby.Server.Implementations;
using Emby.Server.Implementations.EnvironmentInfo;
using Emby.Server.Implementations.IO;
using Emby.Server.Implementations.Networking;
using MediaBrowser.Controller;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;
using Mono.Unix.Native;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;
using System.Threading;
using InteropServices = System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Serilog;
using Serilog.AspNetCore;

namespace MediaBrowser.Server.Mono
{
    public class MainClass
    {
        private static ILogger _logger;
        private static IFileSystem FileSystem;
        private static IServerApplicationPaths _appPaths;
        private static ILoggerFactory _loggerFactory;

        private static readonly TaskCompletionSource<bool> ApplicationTaskCompletionSource = new TaskCompletionSource<bool>();
        private static bool _restartOnShutdown;

        public static void Main(string[] args)
        {
            var applicationPath = Assembly.GetEntryAssembly().Location;

            SetSqliteProvider();

            var options = new StartupOptions(Environment.GetCommandLineArgs());

            // Allow this to be specified on the command line.
            var customProgramDataPath = options.GetOption("-programdata");

            var appPaths = CreateApplicationPaths(applicationPath, customProgramDataPath);
            _appPaths = appPaths;

            var logger = new LoggerConfiguration()
                            .Enrich.FromLogContext()
                            .WriteTo.Console()
                            .CreateLogger();

            using (var loggerFactory = new SerilogLoggerFactory(logger))
            {
                _loggerFactory = loggerFactory;

                _logger = loggerFactory.CreateLogger("Main");

                ApplicationHost.LogEnvironmentInfo(_logger, appPaths, true);

                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                RunApplication(appPaths, loggerFactory, options);

                _logger.LogInformation("Disposing app host");

                if (_restartOnShutdown)
                {
                    StartNewInstance(options);
                }
            }
        }

        private static void SetSqliteProvider()
        {
            // SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            //SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
            SQLitePCL.Batteries_V2.Init();
        }

        private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, string programDataPath)
        {
            if (string.IsNullOrEmpty(programDataPath))
            {
                if (InteropServices.RuntimeInformation.IsOSPlatform(InteropServices.OSPlatform.Windows))
                {
                    programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                }
                else
                {
                    // $XDG_DATA_HOME defines the base directory relative to which user specific data files should be stored.
                    programDataPath = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                    // If $XDG_DATA_HOME is either not set or empty, $HOME/.local/share should be used.
                    if (string.IsNullOrEmpty(programDataPath)){
                        programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
                    }
                }
                programDataPath = Path.Combine(programDataPath, "jellyfin");
            }

            var appFolderPath = Path.GetDirectoryName(applicationPath);

            return new ServerApplicationPaths(programDataPath, appFolderPath, appFolderPath);
        }

        private static void RunApplication(ServerApplicationPaths appPaths, ILoggerFactory loggerFactory, StartupOptions options)
        {
            // Allow all https requests
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            var environmentInfo = GetEnvironmentInfo();

            var fileSystem = new ManagedFileSystem(loggerFactory.CreateLogger("FileSystem"), environmentInfo, null, appPaths.TempDirectory, true);

            FileSystem = fileSystem;

            using (var appHost = new MonoAppHost(appPaths,
                loggerFactory,
                options,
                fileSystem,
                new PowerManagement(),
                "embyserver-mono_{version}.zip",
                environmentInfo,
                new NullImageEncoder(),
                new SystemEvents(loggerFactory.CreateLogger("SystemEvents")),
                new NetworkManager(loggerFactory.CreateLogger("NetworkManager"), environmentInfo)))
            {
                if (options.ContainsOption("-v"))
                {
                    Console.WriteLine(appHost.ApplicationVersion.ToString());
                    return;
                }

                Console.WriteLine("appHost.Init");

                appHost.Init();

                appHost.ImageProcessor.ImageEncoder = ImageEncoderHelper.GetImageEncoder(_logger, fileSystem, options, () => appHost.HttpClient, appPaths, environmentInfo, appHost.LocalizationManager);

                Console.WriteLine("Running startup tasks");

                var task = appHost.RunStartupTasks();
                Task.WaitAll(task);

                task = ApplicationTaskCompletionSource.Task;

                Task.WaitAll(task);
            }
        }

        private static MonoEnvironmentInfo GetEnvironmentInfo()
        {
            var info = new MonoEnvironmentInfo();

            var uname = GetUnixName();

            var sysName = uname.sysname ?? string.Empty;

            if (string.Equals(sysName, "Darwin", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Model.System.OperatingSystem.OSX;
            }
            else if (string.Equals(sysName, "Linux", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Model.System.OperatingSystem.Linux;
            }
            else if (string.Equals(sysName, "BSD", StringComparison.OrdinalIgnoreCase))
            {
                info.OperatingSystem = Model.System.OperatingSystem.BSD;
            }

            var archX86 = new Regex("(i|I)[3-6]86");

            if (archX86.IsMatch(uname.machine))
            {
                info.SystemArchitecture = Architecture.X86;
            }
            else if (string.Equals(uname.machine, "x86_64", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.X64;
            }
            else if (uname.machine.StartsWith("arm", StringComparison.OrdinalIgnoreCase))
            {
                info.SystemArchitecture = Architecture.Arm;
            }
            else if (System.Environment.Is64BitOperatingSystem)
            {
                info.SystemArchitecture = Architecture.X64;
            }
            else
            {
                info.SystemArchitecture = Architecture.X86;
            }

            return info;
        }

        private static Uname _unixName;

        private static Uname GetUnixName()
        {
            if (_unixName == null)
            {
                var uname = new Uname();
                try
                {
                    Utsname utsname;
                    var callResult = Syscall.uname(out utsname);
                    if (callResult == 0)
                    {
                        uname.sysname = utsname.sysname ?? string.Empty;
                        uname.machine = utsname.machine ?? string.Empty;
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError("Error getting unix name", ex);
                }
                _unixName = uname;
            }
            return _unixName;
        }

        public class Uname
        {
            public string sysname = string.Empty;
            public string machine = string.Empty;
        }

        /// <summary>
        /// Handles the UnhandledException event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception)e.ExceptionObject;

            // TODO
            /*
            new UnhandledExceptionWriter(_appPaths, _logger, _logManager, FileSystem, new ConsoleLogger()).Log(exception);

            if (!Debugger.IsAttached)
            {
                var message = LogHelper.GetLogMessage(exception).ToString();

                if (message.IndexOf("InotifyWatcher", StringComparison.OrdinalIgnoreCase) == -1 &&
                    message.IndexOf("_IOCompletionCallback", StringComparison.OrdinalIgnoreCase) == -1)
                {
                    Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
                }
            }*/
        }

        public static void Shutdown()
        {
            ApplicationTaskCompletionSource.SetResult(true);
        }

        public static void Restart()
        {
            _restartOnShutdown = true;

            Shutdown();
        }

        private static void StartNewInstance(StartupOptions startupOptions)
        {
            _logger.LogInformation("Starting new instance");

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
                    .Select(NormalizeCommandLineArgument)
                    .ToArray();

                commandLineArgsString = string.Join(" ", args);
            }

            _logger.LogInformation("Executable: {0}", module);
            _logger.LogInformation("Arguments: {0}", commandLineArgsString);

            Process.Start(module, commandLineArgsString);
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

    // class NoCheckCertificatePolicy : ICertificatePolicy
    // {
    //     public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
    //     {
    //         return true;
    //     }
    // }
    
    public class MonoEnvironmentInfo : EnvironmentInfo
    {
        
        //public override string GetUserId()
        //{
        //    return Syscall.getuid().ToString(CultureInfo.InvariantCulture);
        //}
    }
}
