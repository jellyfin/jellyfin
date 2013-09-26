using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Gtk;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Mono
{
	public class MainClass
	{
		private static ApplicationHost _appHost;

		private static Mutex _singleInstanceMutex;

		private static ILogger _logger;

		public static void Main (string[] args)
		{
			Application.Init ();

			var appPaths = CreateApplicationPaths();

			var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
			logManager.ReloadLogger(LogSeverity.Info);

			var logger = _logger = logManager.GetLogger("Main");

			BeginLog(logger);

			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			bool createdNew;

			var runningPath = Process.GetCurrentProcess().MainModule.FileName.Replace(Path.DirectorySeparatorChar.ToString(), string.Empty);

			_singleInstanceMutex = new Mutex(true, @"Local\" + runningPath, out createdNew);

			if (!createdNew)
			{
				_singleInstanceMutex = null;
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
				RunApplication(appPaths, logManager);
			}
			finally
			{
				logger.Info("Shutting down");

				ReleaseMutex(logger);

				_appHost.Dispose();
			}
		}

		private static ServerApplicationPaths CreateApplicationPaths()
		{
			return new ServerApplicationPaths("D:\\MonoTest");
		}

		private static void RunApplication(ServerApplicationPaths appPaths, ILogManager logManager)
		{
			// TODO: Show splash here

			SystemEvents.SessionEnding += SystemEvents_SessionEnding;

			_appHost = new ApplicationHost(appPaths, logManager);

			var task = _appHost.Init();
			Task.WaitAll (task);

			task = _appHost.RunStartupTasks();
			Task.WaitAll (task);

			// TODO: Hide splash here
			MainWindow win = new MainWindow ();

			win.Show ();

			Application.Run ();
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
		private static void BeginLog(ILogger logger)
		{
			logger.Info("Media Browser Server started");
			logger.Info("Command line: {0}", string.Join(" ", Environment.GetCommandLineArgs()));

			logger.Info("Server: {0}", Environment.MachineName);
			logger.Info("Operating system: {0}", Environment.OSVersion.ToString());
		}

		/// <summary>
		/// Handles the UnhandledException event of the CurrentDomain control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exception = (Exception)e.ExceptionObject;

			_logger.ErrorException("UnhandledException", exception);

			if (!Debugger.IsAttached)
			{
				Environment.Exit(System.Runtime.InteropServices.Marshal.GetHRForException(exception));
			}
		}

		/// <summary>
		/// Releases the mutex.
		/// </summary>
		internal static void ReleaseMutex(ILogger logger)
		{
			if (_singleInstanceMutex == null)
			{
				return;
			}

			logger.Debug("Releasing mutex");

			_singleInstanceMutex.ReleaseMutex();
			_singleInstanceMutex.Close();
			_singleInstanceMutex.Dispose();
			_singleInstanceMutex = null;
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
			Application.Quit ();
		}

		public static void Restart()
		{
			// Second instance will start first, so release the mutex and dispose the http server ahead of time
			ReleaseMutex (_logger);

			_appHost.Dispose();

			Application.Quit ();
		}
	}
}
