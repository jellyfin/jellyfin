
namespace MediaBrowser.Model.Plugins
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
