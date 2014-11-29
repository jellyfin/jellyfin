using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace MediaBrowser.Server.Mac
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		public AppDelegate ()
		{

		}

		public override void FinishedLaunching (NSObject notification)
		{
			new MenuBarIcon (MainClass.AppHost.LogManager.GetLogger("Tray"))
				.ShowIcon ();
		}
	}
}

