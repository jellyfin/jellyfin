
namespace MediaBrowser.Common.Plugins
{
    public abstract class BaseTheme : BasePlugin
    {
        public sealed override bool DownloadToUi
        {
            get
            {
                return true;
            }
        }
    }
}
