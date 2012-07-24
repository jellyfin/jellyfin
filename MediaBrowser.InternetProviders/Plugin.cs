using MediaBrowser.Common.Plugins;

namespace MediaBrowser.InternetProviders
{
    public class Plugin : BaseGenericPlugin<PluginConfiguration>
    {
        public override string Name
        {
            get { return "Internet Providers"; }
        }

        public override void InitInServer()
        {
        }
    }
}
