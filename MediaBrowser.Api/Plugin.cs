using MediaBrowser.Common.Plugins;
using System.ComponentModel.Composition;

namespace MediaBrowser.Api
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BasePlugin
    {
        public override string Name
        {
            get { return "Media Browser API"; }
        }
    }
}
