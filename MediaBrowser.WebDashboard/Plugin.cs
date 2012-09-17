using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using System.ComponentModel.Composition;

namespace MediaBrowser.WebDashboard
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BasePlugin
    {
        public override string Name
        {
            get { return "Dashboard"; }
        }
    }
}
