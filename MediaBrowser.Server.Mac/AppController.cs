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

		public AppController()
		{

		}

		public override void AwakeFromNib()
		{
			var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			statusItem.Menu = statusMenu;
			statusItem.Image = NSImage.ImageNamed("touchicon");
			statusItem.HighlightMode = true;

			statusItem.Menu.RemoveAllItems ();

			browseMenuItem = new NSMenuItem ("Browse Media Library", "b", delegate {
				Browse (this);
			});
			statusItem.Menu.AddItem (browseMenuItem);

			configureMenuItem = new NSMenuItem ("Configure Media Browser", "c", delegate {
				Configure (this);
			});
			statusItem.Menu.AddItem (configureMenuItem);

			developerMenuItem = new NSMenuItem ("Developer Resources");
			statusItem.Menu.AddItem (developerMenuItem);

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
			statusItem.Menu.AddItem (communityMenuItem);

			quitMenuItem = new NSMenuItem ("Quit", "q", delegate {
				Quit (this);
			});
			statusItem.Menu.AddItem (quitMenuItem);
		}

		private IServerApplicationHost AppHost{ get; set;}
		private ILogger Logger{ get; set;}

		private void Quit(NSObject sender)
		{
			NSApplication.SharedApplication.Terminate(this);
			//AppHost.Shutdown();
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
	}
}

