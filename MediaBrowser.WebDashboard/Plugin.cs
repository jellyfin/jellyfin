using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.WebDashboard
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "Dashboard"; }
        }
    }
}
