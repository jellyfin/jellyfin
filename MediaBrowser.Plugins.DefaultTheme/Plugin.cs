using MediaBrowser.Common.Plugins;
using System.ComponentModel.Composition;

namespace MediaBrowser.Plugins.DefaultTheme
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseTheme
    {
        public override string Name
        {
            get { return "Default Theme"; }
        }

        protected override void InitializeInUi()
        {
            base.InitializeInUi();
        }
    }
}
