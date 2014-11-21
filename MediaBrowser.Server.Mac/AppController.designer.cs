// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoMac.Foundation;

namespace MediaBrowser.Server.Mac
{
	partial class AppController
	{
		[Outlet]
		MonoMac.AppKit.NSMenu statusMenu { get; set; }

		[Action ("HelloWorld:")]
		partial void HelloWorld (MonoMac.Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (statusMenu != null) {
				statusMenu.Dispose ();
				statusMenu = null;
			}
		}
	}
}
