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
		private NSMenuItem developerMenuItem;
		private NSMenuItem quitMenuItem;
		private NSMenuItem githubMenuItem;
		private NSMenuItem apiMenuItem;
		private NSMenuItem communityMenuItem;

		public static MenuBarIcon Instance;

		public MenuBarIcon ()
		{
			Instance = this;
			//MainClass.AddDependencies (this);
		}

		public void ShowIcon() {

			NSApplication.SharedApplication.BeginInvokeOnMainThread (ShowIconInternal);
		}

		private void ShowIconInternal() {

			var menu = new NSMenu ();

			var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
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

			developerMenuItem = new NSMenuItem ("Developer Resources");
			menu.AddItem (developerMenuItem);

			var developerMenu = new NSMenu ();
			developerMenuItem.Submenu = developerMenu;

			apiMenuItem = new NSMenuItem ("Api Documentation", "a", delegate {
				ApiDocs (NSApplication.SharedApplication);
			});
			developerMenu.AddItem (apiMenuItem);

			githubMenuItem = new NSMenuItem ("Github", "g", delegate {
				Github (NSApplication.SharedApplication);
			});
			developerMenu.AddItem (githubMenuItem);

			communityMenuItem = new NSMenuItem ("Visit Community", "v", delegate {
				Community (NSApplication.SharedApplication);
			});
			menu.AddItem (communityMenuItem);

			quitMenuItem = new NSMenuItem ("Quit", "q", delegate {
				Quit (NSApplication.SharedApplication);
			});
			menu.AddItem (quitMenuItem);

			NSApplication.SharedApplication.MainMenu = menu;

			//ConfigurationManager.ConfigurationUpdated -= Instance_ConfigurationUpdated;
			//LocalizeText ();
			//ConfigurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;
		}

		public IServerApplicationHost AppHost{ get; set;}
		public IServerConfigurationManager ConfigurationManager { get; set;}
		public ILogger Logger{ get; set;}
		public ILocalizationManager Localization { get; set;}

		private void Quit(NSObject sender)
		{
			AppHost.Shutdown();
		}

		private void Community(NSObject sender)
		{
			BrowserLauncher.OpenCommunity(Logger);
		}

		private void Configure(NSObject sender)
		{
			BrowserLauncher.OpenDashboard(AppHost, Logger);
		}

		private void Browse(NSObject sender)
		{
			BrowserLauncher.OpenWebClient(AppHost, Logger);
		}

		private void Github(NSObject sender)
		{
			BrowserLauncher.OpenGithub(Logger);
		}

		private void ApiDocs(NSObject sender)
		{
			BrowserLauncher.OpenSwagger(AppHost, Logger);
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
			if (!string.Equals(ConfigurationManager.Configuration.UICulture, _uiCulture,
				StringComparison.OrdinalIgnoreCase))
			{
				LocalizeText();
			}
		}

		private void LocalizeText()
		{
			_uiCulture = ConfigurationManager.Configuration.UICulture;

			NSApplication.SharedApplication.BeginInvokeOnMainThread (LocalizeInternal);
		}

		private void LocalizeInternal() {

			quitMenuItem.Title = Localization.GetLocalizedString("LabelExit");
			communityMenuItem.Title = Localization.GetLocalizedString("LabelVisitCommunity");
			githubMenuItem.Title = Localization.GetLocalizedString("LabelGithub");
			apiMenuItem.Title = Localization.GetLocalizedString("LabelApiDocumentation");
			developerMenuItem.Title = Localization.GetLocalizedString("LabelDeveloperResources");
			browseMenuItem.Title = Localization.GetLocalizedString("LabelBrowseLibrary");
			configureMenuItem.Title = Localization.GetLocalizedString("LabelConfigureMediaBrowser");
		}
	}
}

