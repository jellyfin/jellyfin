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
	[Register("AppController")]
	public partial class AppController : NSObject
	{
		private NSMenuItem browseMenuItem;
		private NSMenuItem configureMenuItem;
		private NSMenuItem developerMenuItem;
		private NSMenuItem quitMenuItem;
		private NSMenuItem githubMenuItem;
		private NSMenuItem apiMenuItem;
		private NSMenuItem communityMenuItem;

		public static AppController Instance;

		public AppController()
		{
			Instance = this;
			MainClass.AddDependencies (this);
		}

		public override void AwakeFromNib()
		{
			var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			statusItem.Menu = statusMenu;
			statusItem.Image = NSImage.ImageNamed("statusicon");
			statusItem.HighlightMode = true;

			statusMenu.RemoveAllItems ();

			browseMenuItem = new NSMenuItem ("Browse Media Library", "b", delegate {
				Browse (this);
			});
			statusMenu.AddItem (browseMenuItem);

			configureMenuItem = new NSMenuItem ("Configure Media Browser", "c", delegate {
				Configure (this);
			});
			statusMenu.AddItem (configureMenuItem);

			developerMenuItem = new NSMenuItem ("Developer Resources");
			statusMenu.AddItem (developerMenuItem);

			var developerMenu = new NSMenu ();
			developerMenuItem.Submenu = developerMenu;

			apiMenuItem = new NSMenuItem ("Api Documentation", "a", delegate {
				ApiDocs (this);
			});
			developerMenu.AddItem (apiMenuItem);

			githubMenuItem = new NSMenuItem ("Github", "g", delegate {
				Github (this);
			});
			developerMenu.AddItem (githubMenuItem);

			communityMenuItem = new NSMenuItem ("Visit Community", "v", delegate {
				Community (this);
			});
			statusMenu.AddItem (communityMenuItem);

			quitMenuItem = new NSMenuItem ("Quit", "q", delegate {
				Quit (this);
			});
			statusMenu.AddItem (quitMenuItem);

			ConfigurationManager.ConfigurationUpdated -= Instance_ConfigurationUpdated;
			LocalizeText ();
			ConfigurationManager.ConfigurationUpdated += Instance_ConfigurationUpdated;
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
			InvokeOnMainThread (() => NSApplication.SharedApplication.Terminate(this));
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

			BeginInvokeOnMainThread (LocalizeInternal);
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

