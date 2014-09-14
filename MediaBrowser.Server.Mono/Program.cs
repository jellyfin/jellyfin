using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication;
using MediaBrowser.ServerApplication.Native;
using MediaBrowser.ServerApplication.IO;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
// MONOMKBUNDLE: For the embedded version, mkbundle tool
#if MONOMKBUNDLE
using Mono.Unix;
using Mono.Unix.Native;
using System.Text;
#endif

namespace MediaBrowser.Server.Mono
{
	public class MainClass
	{
		private static ApplicationHost _appHost;

		private static ILogger _logger;

		public static void Main (string[] args)
		{
			//GetEntryAssembly is empty when running from a mkbundle package
			#if MONOMKBUNDLE
			var applicationPath = GetExecutablePath();
			#else
			var applicationPath = Assembly.GetEntryAssembly ().Location;
			#endif
			
			var options = new StartupOptions();

			// Allow this to be specified on the command line.
			var customProgramDataPath = options.GetOption("-programdata");

			var appPaths = CreateApplicationPaths(applicationPath, customProgramDataPath);

			var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
			logManager.ReloadLogger(LogSeverity.Info);
			logManager.AddConsoleOutput();

			var logger = _logger = logManager.GetLogger("Main");

			BeginLog(logger, appPaths);

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			if (PerformUpdateIfNeeded(appPaths, logger))
			{
				logger.Info("Exiting to perform application update.");
				return;
			}

			try
			{
				RunApplication(appPaths, logManager);
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
				return new ServerApplicationPaths(applicationPath);
			}
			
			return new ServerApplicationPaths(programDataPath, applicationPath);
		}

		/// <summary>
		/// Determines whether this instance [can self restart].
		/// </summary>
		/// <returns><c>true</c> if this instance [can self restart]; otherwise, <c>false</c>.</returns>
		public static bool CanSelfRestart
		{
			get
			{
				return false;
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

		private static RemoteCertificateValidationCallback _ignoreCertificates = new RemoteCertificateValidationCallback(delegate { return true; });

		private static TaskCompletionSource<bool> _applicationTaskCompletionSource = new TaskCompletionSource<bool>();

		private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager)
		{
			SystemEvents.SessionEnding += SystemEvents_SessionEnding;

			// Allow all https requests
			ServicePointManager.ServerCertificateValidationCallback = _ignoreCertificates;

			_appHost = new ApplicationHost(appPaths, logManager, false, false);

			Console.WriteLine ("appHost.Init");

			var initProgress = new Progress<double>();

			var task = _appHost.Init(initProgress);
			Task.WaitAll (task);

			Console.WriteLine ("Running startup tasks");

			task = _appHost.RunStartupTasks();
			Task.WaitAll (task);

			task = _applicationTaskCompletionSource.Task;

			Task.WaitAll (task);
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
		/// Begins the log.
		/// </summary>
		/// <param name="logger">The logger.</param>
		private static void BeginLog(ILogger logger, IApplicationPaths appPaths)
        {
            logger.Info("Media Browser Server started");
            ApplicationHost.LogEnvironmentInfo(logger, appPaths);
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

			if (!Debugger.IsAttached)
			{
				Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
			}
		}

		private static void LogUnhandledException(Exception ex)
		{
			_logger.ErrorException("UnhandledException", ex);

			_appHost.LogManager.Flush ();

			var path = Path.Combine(_appHost.ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, "crash_" + Guid.NewGuid() + ".txt");

			var builder = LogHelper.GetLogMessage(ex);

			Console.WriteLine ("UnhandledException");
			Console.WriteLine (builder.ToString());

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
			return false;
		}

		public static void Shutdown()
		{
			_applicationTaskCompletionSource.SetResult (true);
		}

		public static void Restart()
		{
			// Second instance will start first, so dispose so that the http ports will be available to the new instance
			_appHost.Dispose();

			// Right now this method will just shutdown, but not restart
			Shutdown ();
		}

		// Return the running process path
		#if MONOMKBUNDLE
		public static string GetExecutablePath() 
		{ 
			var builder = new StringBuilder (8192); 
			if (Syscall.readlink("/proc/self/exe", builder) >= 0)
				return builder.ToString (); 
			else 
				return null; 
		}
		#endif

	}

	class NoCheckCertificatePolicy : ICertificatePolicy
	{
		public bool CheckValidationResult (ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
		{
			return true;
		}
	}
}
