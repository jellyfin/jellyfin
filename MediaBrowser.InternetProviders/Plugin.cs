using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;

namespace MediaBrowser.InternetProviders
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseGenericPlugin<PluginConfiguration>
    {
        public override string Name
        {
            get { return "Internet Providers"; }
        }
    }
}
