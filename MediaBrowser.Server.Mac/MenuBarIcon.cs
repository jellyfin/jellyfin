using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Startup.Common.Browser;
using System;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace MediaBrowser.Server.Mac
{
	public class MenuBarIcon
	{
		private NSMenuItem browseMenuItem;
		private NSMenuItem configureMenuItem;
		private NSMenuItem quitMenuItem;
		private NSMenuItem communityMenuItem;

		public static MenuBarIcon Instance;

		public MenuBarIcon (ILogger logger)
		{
			Instance = this;
			Logger = logger;
		}

		public void ShowIcon() {

			NSApplication.SharedApplication.BeginInvokeOnMainThread (CreateMenus);
		}

		private void CreateMenus() {

			CreateNsMenu ();
		}

		public void Localize() 
		{
			NSApplication.SharedApplication.BeginInvokeOnMainThread (() => {

				var configManager = MainClass.AppHost.ServerConfigurationManager;

				configManager.ConfigurationUpdated -= Instance_ConfigurationUpdated;
				LocalizeText ();
				configManager.ConfigurationUpdated += Instance_ConfigurationUpdated;
			});
		}

		private NSStatusItem statusItem;
		private void CreateNsMenu() {

			var menu = new NSMenu ();

			statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			statusItem.Menu = menu;
			statusItem.Image = NSImage.ImageNamed("statusicon");
			statusItem.HighlightMode = true;

			menu.RemoveAllItems ();

			browseMenuItem = new NSMenuItem ("Browse Media Library", "b", delegate {
				Browse (NSApplication.SharedApplication);
			});
			menu.AddItem (browseMenuItem);

			configureMenuItem = new NSMenuItem ("Configure Media Browser", "c", delegate {
				Configure (NSApplication.SharedApplication);
			});
			menu.AddItem (configureMenuItem);

			communityMenuItem = new NSMenuItem ("Visit Community", "v", delegate {
				Community (NSApplication.SharedApplication);
			});
			menu.AddItem (communityMenuItem);

			quitMenuItem = new NSMenuItem ("Quit", "q", delegate {
				Quit (NSApplication.SharedApplication);
			});
			menu.AddItem (quitMenuItem);
		}

		private ILogger Logger{ get; set;}

		private void Quit(NSObject sender)
		{
			MainClass.AppHost.Shutdown();
		}

		private void Community(NSObject sender)
		{
			BrowserLauncher.OpenCommunity(MainClass.AppHost);
		}

		private void Configure(NSObject sender)
		{
			BrowserLauncher.OpenDashboard(MainClass.AppHost);
		}

		private void Browse(NSObject sender)
		{
			BrowserLauncher.OpenWebClient(MainClass.AppHost);
		}

		public void Terminate() 
		{
			NSApplication.SharedApplication.InvokeOnMainThread (() => NSApplication.SharedApplication.Terminate(NSApplication.SharedApplication));
		}

		private string _uiCulture;
		/// <summary>
		/// Handles the ConfigurationUpdated event of the Instance control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
		void Instance_ConfigurationUpdated(object sender, EventArgs e)
		{
			var configManager = MainClass.AppHost.ServerConfigurationManager;

			if (!string.Equals(configManager.Configuration.UICulture, _uiCulture,
				StringComparison.OrdinalIgnoreCase))
			{
				LocalizeText();
			}
		}

		private void LocalizeText()
		{
			var configManager = MainClass.AppHost.ServerConfigurationManager;

			_uiCulture = configManager.Configuration.UICulture;

			NSApplication.SharedApplication.BeginInvokeOnMainThread (LocalizeInternal);
		}

		private void LocalizeInternal() {

			var localization = MainClass.AppHost.LocalizationManager;

			quitMenuItem.Title = localization.GetLocalizedString("LabelExit");
			communityMenuItem.Title = localization.GetLocalizedString("LabelVisitCommunity");
			browseMenuItem.Title = localization.GetLocalizedString("LabelBrowseLibrary");
			configureMenuItem.Title = localization.GetLocalizedString("LabelConfigureServer");
		}
	}
}

