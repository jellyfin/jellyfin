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
using Microsoft.Win32;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace MediaBrowser.Server.Mac
{
	class MainClass
	{
		private static ApplicationHost _appHost;

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
			var b = true;
		}

		public static void AddDependencies(AppController appController){
			appController.AppHost = _appHost;
			appController.Logger = _logger;
			appController.ConfigurationManager = _appHost.ServerConfigurationManager;
			appController.Localization = _appHost.LocalizationManager;
		}

		private static ServerApplicationPaths CreateApplicationPaths(string applicationPath, string programDataPath)
		{
			if (string.IsNullOrEmpty(programDataPath))
			{
				// TODO: Use CommonApplicationData? Will we always have write access?
				programDataPath = Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData), "mediabrowser-server");
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

			var fileSystem = new CommonFileSystem(logManager.GetLogger("FileSystem"), false, true);

			var nativeApp = new NativeApp();

			_appHost = new ApplicationHost(appPaths, logManager, options, fileSystem, "MBServer.Mono", false, nativeApp);

			if (options.ContainsOption("-v")) {
				Console.WriteLine (_appHost.ApplicationVersion.ToString());
				return;
			}

			Console.WriteLine ("appHost.Init");

			var initProgress = new Progress<double>();

			var task = _appHost.Init(initProgress);

			Task.WaitAll(task);

			Task.Run (() => _appHost.RunStartupTasks());
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
			_appHost.Dispose ();

			_logger.Info("AppController.Terminate");
			AppController.Instance.Terminate ();
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
	}

	class NoCheckCertificatePolicy : ICertificatePolicy
	{
		public bool CheckValidationResult (ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
		{
			return true;
		}
	}
}

