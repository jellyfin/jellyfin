using MediaBrowser.Model.Logging;
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
using Emby.Common.Implementations.EnvironmentInfo;
using Emby.Common.Implementations.Logging;
using Emby.Common.Implementations.Networking;
using Emby.Common.Implementations.Security;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Server.Implementations.IO;
using MediaBrowser.Model.System;
using MediaBrowser.Server.Startup.Common.IO;
using Mono.Unix.Native;
using NLog;
using ILogger = MediaBrowser.Model.Logging.ILogger;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace MediaBrowser.Server.Mono
{
    public class MainClass
    {
        private static ApplicationHost _appHost;

        private static ILogger _logger;

        public static void Main(string[] args)
        {
            var applicationPath = Assembly.GetEntryAssembly().Location;
            var appFolderPath = Path.GetDirectoryName(applicationPath);

            TryCopySqliteConfigFile(appFolderPath);
            SetSqliteProvider();

            var options = new StartupOptions(Environment.GetCommandLineArgs());

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

        private static void TryCopySqliteConfigFile(string appFolderPath)
        {
            try
            {
                File.Copy(Path.Combine(appFolderPath, "System.Data.SQLite.dll.config"),
                    Path.Combine(appFolderPath, "SQLitePCLRaw.provider.sqlite3.dll.config"),
                    true);
            }
            catch
            {
                
            }
        }

        private static void SetSqliteProvider()
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());
        }

        private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, string programDataPath)
        {
            if (string.IsNullOrEmpty(programDataPath))
            {
                programDataPath = ApplicationPathHelper.GetProgramDataPath(applicationPath);
            }

            var appFolderPath = Path.GetDirectoryName(applicationPath);

            return new ServerApplicationPaths(programDataPath, appFolderPath, Path.GetDirectoryName(applicationPath));
        }

        private static readonly TaskCompletionSource<bool> ApplicationTaskCompletionSource = new TaskCompletionSource<bool>();

        private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager, StartupOptions options)
        {
            // Allow all https requests
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

            var fileSystem = new MonoFileSystem(logManager.GetLogger("FileSystem"), false, false);
            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

            var environmentInfo = GetEnvironmentInfo();

            var imageEncoder = ImageEncoderHelper.GetImageEncoder(_logger, logManager, fileSystem, options, () => _appHost.HttpClient, appPaths);

            _appHost = new MonoAppHost(appPaths,
                logManager,
                options,
                fileSystem,
                new PowerManagement(),
                "emby.mono.zip",
                environmentInfo,
                imageEncoder,
                new Startup.Common.SystemEvents(logManager.GetLogger("SystemEvents")),
                new MemoryStreamProvider(),
                new NetworkManager(logManager.GetLogger("NetworkManager")),
                GenerateCertificate,
                () => Environment.UserName);

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

        private static void GenerateCertificate(string certPath, string certHost)
        {
            CertificateGenerator.CreateSelfSignCertificatePfx(certPath, certHost, _logger);
        }

        private static MonoEnvironmentInfo GetEnvironmentInfo()
        {
            var info = new MonoEnvironmentInfo();

            var uname = GetUnixName();

            var sysName = uname.sysname ?? string.Empty;

            if (string.Equals(sysName, "Darwin", StringComparison.OrdinalIgnoreCase))
            {
                //info.OperatingSystem = Startup.Common.OperatingSystem.Osx;
            }
            else if (string.Equals(sysName, "Linux", StringComparison.OrdinalIgnoreCase))
            {
                //info.OperatingSystem = Startup.Common.OperatingSystem.Linux;
            }
            else if (string.Equals(sysName, "BSD", StringComparison.OrdinalIgnoreCase))
            {
                //info.OperatingSystem = Startup.Common.OperatingSystem.Bsd;
                info.IsBsd = true;
            }

            var archX86 = new Regex("(i|I)[3-6]86");

            if (archX86.IsMatch(uname.machine))
            {
                info.CustomArchitecture = Architecture.X86;
            }
            else if (string.Equals(uname.machine, "x86_64", StringComparison.OrdinalIgnoreCase))
            {
                info.CustomArchitecture = Architecture.X64;
            }
            else if (uname.machine.StartsWith("arm", StringComparison.OrdinalIgnoreCase))
            {
                info.CustomArchitecture = Architecture.Arm;
            }
            else if (System.Environment.Is64BitOperatingSystem)
            {
                info.CustomArchitecture = Architecture.X64;
            }
            else
            {
                info.CustomArchitecture = Architecture.X86;
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
                    _logger.ErrorException("Error getting unix name", ex);
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

    public class MonoEnvironmentInfo : EnvironmentInfo
    {
        public bool IsBsd { get; set; }

        public override string GetUserId()
        {
            return Syscall.getuid().ToString(CultureInfo.InvariantCulture);
        }
    }
}
