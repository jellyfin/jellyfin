
namespace MediaBrowser.Common.Plugins
{
    public class BasePluginConfiguration
    {
        public bool Enabled { get; set; }

        public BasePluginConfiguration()
        {
            Enabled = true;
        }
    }
}
