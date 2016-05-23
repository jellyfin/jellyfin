using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Implementations.IO;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.Server.Startup.Common;
using MediaBrowser.Server.Startup.Common.Browser;
using Microsoft.Win32;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;
using CommonIO;
using MediaBrowser.Server.Implementations.Logging;

namespace MediaBrowser.Server.Mac
{
	class MainClass
	{
		internal static ApplicationHost AppHost;

		private static ILogger _logger;

		static void Main (string[] args)
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

			StartApplication(appPaths, logManager, options);
			NSApplication.Init ();
			NSApplication.Main (args);
		}

		private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, string programDataPath)
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
			var resourcesPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName (applicationPath)), "Resources");

			return new ServerApplicationPaths(programDataPath, applicationPath, resourcesPath);
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
			SystemEvents.SessionEnding += SystemEvents_SessionEnding;

			// Allow all https requests
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(delegate { return true; });

			var fileSystem = new ManagedFileSystem(new PatternsLogger(logManager.GetLogger("FileSystem")), false, true);
            fileSystem.AddShortcutHandler(new MbLinkShortcutHandler(fileSystem));

			var nativeApp = new NativeApp(logManager.GetLogger("App"));

			AppHost = new ApplicationHost(appPaths, logManager, options, fileSystem, "Emby.Server.Mac.pkg", nativeApp);

			if (options.ContainsOption("-v")) {
				Console.WriteLine (AppHost.ApplicationVersion.ToString());
				return;
			}

			Console.WriteLine ("appHost.Init");

			Task.Run (() => StartServer(CancellationToken.None));
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
		public bool CheckValidationResult (ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
		{
			return true;
		}
	}
}

