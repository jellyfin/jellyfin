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
		public override void AwakeFromNib()
		{
			//new MenuBarIcon ().ShowIcon ();
		}
	}
}

