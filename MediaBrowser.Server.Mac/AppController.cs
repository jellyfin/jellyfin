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
			//var statusItem = NSStatusBar.SystemStatusBar.CreateStatusItem(30);
			//statusItem.Menu = statusMenu;
			//statusItem.Image = NSImage.ImageNamed("f3bfd_Untitled-thumb");
			//statusItem.HighlightMode = true;
		}

		partial void HelloWorld(NSObject sender)
		{
			Console.WriteLine("hello world");
		}
	}
}

