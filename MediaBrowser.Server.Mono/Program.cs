using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using MediaBrowser.ServerApplication;
using MediaBrowser.ServerApplication.Native;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Gtk;
using Gdk;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Mono
{
	public class MainClass
	{
		private static ApplicationHost _appHost;

		private static ILogger _logger;

		private static MainWindow _mainWindow;

		// The tray Icon
		private static StatusIcon trayIcon;

		public static void Main (string[] args)
		{
			Application.Init ();

			var appPaths = CreateApplicationPaths();

			var logManager = new NlogManager(appPaths.LogDirectoryPath, "server");
			logManager.ReloadLogger(LogSeverity.Info);

			var logger = _logger = logManager.GetLogger("Main");

			BeginLog(logger);

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

		private static ServerApplicationPaths CreateApplicationPaths()
		{
			return new ServerApplicationPaths();
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
			_mainWindow = new MainWindow ();

			// Creation of the Icon
			// Creation of the Icon
			trayIcon = new StatusIcon(new Pixbuf ("tray.png"));
			trayIcon.Visible = true;

			// When the TrayIcon has been clicked.
			trayIcon.Activate += delegate { };
			// Show a pop up menu when the icon has been right clicked.
			trayIcon.PopupMenu += OnTrayIconPopup;

			// A Tooltip for the Icon
			trayIcon.Tooltip = "Media Browser Server";

			_mainWindow.ShowAll ();
			_mainWindow.Visible = false;

			Application.Run ();
		}

		// Create the popup menu, on right click.
		static void OnTrayIconPopup (object o, EventArgs args) {

			Menu popupMenu = new Menu();

			var menuItemBrowse = new ImageMenuItem ("Browse Library");
			menuItemBrowse.Image = new Gtk.Image(Stock.MediaPlay, IconSize.Menu);
			popupMenu.Add(menuItemBrowse);
			menuItemBrowse.Activated += delegate { 
				BrowserLauncher.OpenWebClient(_appHost.UserManager, _appHost.ServerConfigurationManager, _appHost, _logger);
			};

			var menuItemConfigure = new ImageMenuItem ("Configure Media Browser");
			menuItemConfigure.Image = new Gtk.Image(Stock.Edit, IconSize.Menu);
			popupMenu.Add(menuItemConfigure);
			menuItemConfigure.Activated += delegate { 
				BrowserLauncher.OpenDashboard(_appHost.UserManager, _appHost.ServerConfigurationManager, _appHost, _logger);
			};

			var menuItemApi = new ImageMenuItem ("View Api Docs");
			menuItemApi.Image = new Gtk.Image(Stock.Network, IconSize.Menu);
			popupMenu.Add(menuItemApi);
			menuItemApi.Activated += delegate { 
				BrowserLauncher.OpenSwagger(_appHost.ServerConfigurationManager, _appHost, _logger);
			};

			var menuItemCommunity = new ImageMenuItem ("Visit Community");
			menuItemCommunity.Image = new Gtk.Image(Stock.Help, IconSize.Menu);
			popupMenu.Add(menuItemCommunity);
			menuItemCommunity.Activated += delegate { BrowserLauncher.OpenCommunity(_logger); };

			var menuItemGithub = new ImageMenuItem ("Visit Github");
			menuItemGithub.Image = new Gtk.Image(Stock.Network, IconSize.Menu);
			popupMenu.Add(menuItemGithub);
			menuItemGithub.Activated += delegate { BrowserLauncher.OpenGithub(_logger); };

			var menuItemQuit = new ImageMenuItem ("Exit");
			menuItemQuit.Image = new Gtk.Image(Stock.Quit, IconSize.Menu);
			popupMenu.Add(menuItemQuit);
			menuItemQuit.Activated += delegate { Shutdown(); };

			popupMenu.ShowAll();
			popupMenu.Popup();
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
			if (trayIcon != null) {
				trayIcon.Visible = false;
				trayIcon.Dispose ();
				trayIcon = null;
			}

			if (_mainWindow != null) {
				_mainWindow.HideAll ();
				_mainWindow.Dispose ();
				_mainWindow = null;
			}

			Application.Quit ();
		}

		public static void Restart()
		{
			// Second instance will start first, so dispose so that the http ports will be available to the new instance
			_appHost.Dispose();

			// Right now this method will just shutdown, but not restart
			Shutdown ();
		}
	}
}
