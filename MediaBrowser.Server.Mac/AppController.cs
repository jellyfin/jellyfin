using System;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace MediaBrowser.Server.Mac
{
	[Register("AppController")]
	public partial class AppController : NSObject
	{
		public AppController()
		{

		}

		public override void AwakeFromNib()
		{
			var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			statusItem.Menu = statusMenu;
			statusItem.Image = NSImage.ImageNamed("touchicon");
			statusItem.HighlightMode = true;
		}

		partial void HelloWorld(NSObject sender)
		{

		}

		partial void Quit(NSObject sender)
		{

		}

		partial void Configure(NSObject sender)
		{

		}

		partial void Browse(NSObject sender)
		{

		}

		partial void Github(NSObject sender)
		{

		}

		partial void ApiDocs(NSObject sender)
		{

		}
	}
}

