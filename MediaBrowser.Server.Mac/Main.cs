using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.Server.Startup.Common.IO;
using MediaBrowser.Server.Implementations;
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using Emby.Server.Core;
using Emby.Server.Implementations;
using Emby.Common.Implementations.Logging;
using Emby.Common.Implementations.EnvironmentInfo;
using Emby.Server.Mac.Native;
using Emby.Server.Implementations.IO;
using Emby.Common.Implementations.Networking;
using Emby.Common.Implementations.Security;
using Mono.Unix.Native;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Mac
{
	class MainClass
	{
		internal static MacAppHost AppHost;

		private static ILogger _logger;

		static void Main (string[] args)
		{
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_sqlite3());

            var applicationPath = Assembly.GetEntryAssembly().Location;

			var options = new StartupOptions(Environment.GetCommandLineArgs());

			// Allow this to be specified on the command line.
			var customProgramDataPath = options.GetOption("-programdata");

			var appFolderPath = Path.GetDirectoryName(applicationPath);

			var appPaths = CreateApplicationPaths(appFolderPath, customProgramDataPath);

			var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
			logManager.ReloadLogger(LogSeverity.Info);
			logManager.AddConsoleOutput();

			var logger = _logger = logManager.GetLogger("Main");

			ApplicationHost.LogEnvironmentInfo(logger, appPaths, true);

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			StartApplication(appPaths, logManager, options);
			NSApplication.Init ();
			NSApplication.Main (args);
		}

		private static ServerApplicationPaths CreateApplicationPaths(string appFolderPath, string programDataPath)
		{
			if (string.IsNullOrEmpty(programDataPath))
			{
				// TODO: Use CommonApplicationData? Will we always have write access?
				programDataPath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "mediabrowser-server");

				if (!Directory.Exists (programDataPath)) {
					programDataPath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "emby-server");
				}
			}

			// Within the mac bundle, go uo two levels then down into Resources folder
			var resourcesPath = Path.Combine(Path.GetDirectoryName(appFolderPath), "Resources");

			return new ServerApplicationPaths(programDataPath, appFolderPath, resourcesPath);
		}

		/// <summary>
		/// Runs the application.
		/// </summary>
		/// <param name="appPaths">The app paths.</param>
		/// <param name="logManager">The log manager.</param>
		/// <param name="options">The options.</param>
		private static void StartApplication(ServerApplicationPaths appPaths, 
			ILogManager logManager, 
			StartupOptions options)
		{
			// Allow all https requests
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

			var fileSystem = new MonoFileSystem(logManager.GetLogger("FileSystem"), false, false);
            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

			var environmentInfo = GetEnvironmentInfo();

			var imageEncoder = ImageEncoderHelper.GetImageEncoder(_logger, 
			                                                      logManager, 
			                                                      fileSystem, 
			                                                      options, 
			                                                      () => AppHost.HttpClient, 
			                                                      appPaths);

			AppHost = new MacAppHost(appPaths,
									 logManager,
									 options,
									 fileSystem,
									 new PowerManagement(),
									 "Emby.Server.Mac.pkg",
									 environmentInfo,
									 imageEncoder,
									 new Startup.Common.SystemEvents(logManager.GetLogger("SystemEvents")),
									 new MemoryStreamProvider(),
			                         new NetworkManager(logManager.GetLogger("NetworkManager")),
									 GenerateCertificate,
									 () => Environment.UserName);

			if (options.ContainsOption("-v")) {
				Console.WriteLine (AppHost.ApplicationVersion.ToString());
				return;
			}

			Console.WriteLine ("appHost.Init");

			Task.Run (() => StartServer(CancellationToken.None));
        }

        private static void GenerateCertificate(string certPath, string certHost)
        {
            CertificateGenerator.CreateSelfSignCertificatePfx(certPath, certHost, _logger);
        }

        private static EnvironmentInfo GetEnvironmentInfo()
        {
            var info = new EnvironmentInfo()
            {
                CustomOperatingSystem = MediaBrowser.Model.System.OperatingSystem.OSX
            };

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
                //info.IsBsd = true;
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

        private static async void StartServer(CancellationToken cancellationToken) 
		{
			var initProgress = new Progress<double>();

			await AppHost.Init (initProgress).ConfigureAwait (false);

			await AppHost.RunStartupTasks ().ConfigureAwait (false);

			if (MenuBarIcon.Instance != null) 
			{
				MenuBarIcon.Instance.Localize ();
			}
		}

		public static void Shutdown()
		{
			ShutdownApp();
		}

		private static void ShutdownApp()
		{
			_logger.Info ("Calling ApplicationHost.Dispose");
			AppHost.Dispose ();

			_logger.Info("AppController.Terminate");
			MenuBarIcon.Instance.Terminate ();
		}

        public static void Restart()
        {
            _logger.Info("Disposing app host");
            AppHost.Dispose();

            _logger.Info("Starting new instance");

            var args = Environment.GetCommandLineArgs()
				.Skip(1)
                .Select(NormalizeCommandLineArgument);

            var commandLineArgsString = string.Join(" ", args.ToArray());
			var module = Environment.GetCommandLineArgs().First();

			_logger.Info ("Executable: {0}", module);
			_logger.Info ("Arguments: {0}", commandLineArgsString);

            Process.Start(module, commandLineArgsString);

            _logger.Info("AppController.Terminate");
            MenuBarIcon.Instance.Terminate();
        }

        private static string NormalizeCommandLineArgument(string arg)
        {
            if (arg.IndexOf(" ", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return arg;
            }

            return "\"" + arg + "\"";
        }

		/// <summary>
		/// Handles the UnhandledException event of the CurrentDomain control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = (Exception)e.ExceptionObject;

			new UnhandledExceptionWriter(AppHost.ServerConfigurationManager.ApplicationPaths, _logger, AppHost.LogManager).Log(exception);

			if (!Debugger.IsAttached)
			{
				Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
			}
		}
	}

	class NoCheckCertificatePolicy : ICertificatePolicy
	{
		public bool CheckValidationResult (ServicePoint srvPoint, 
		                                   System.Security.Cryptography.X509Certificates.X509Certificate certificate, 
		                                   WebRequest request, 
		                                   int certificateProblem)
		{
			return true;
		}
    }
}

