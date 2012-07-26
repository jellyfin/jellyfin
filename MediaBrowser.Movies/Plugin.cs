using System.ComponentModel.Composition;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Movies
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "Movies"; }
        }
    }
}
