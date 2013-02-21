using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller.Entities;

namespace TestPlugin
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BasePlugin
    {
        public override string Name
        {
            get { return "Test MB3 Plug-in"; }
        }

        protected override void InitializeOnServer(bool isFirstRun)
        {
            base.InitializeOnServer(isFirstRun);
            Logger.LogDebugInfo("Test Plug-in Loaded.");
        }
    }
}
