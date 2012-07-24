using System.Linq;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides information about installed plugins
    /// </summary>
    public class PluginsHandler : JsonHandler
    {
        protected override object ObjectToSerialize
        {
            get
            {
                var plugins = Kernel.Instance.PluginController.Plugins.Select(p =>
                {
                    return new PluginInfo()
                    {
                        Path = p.Path,
                        Name = p.Name,
                        Enabled = p.Enabled,
                        DownloadToUI = p.DownloadToUI,
                        Version = p.Version
                    };
                });

                if (QueryString["uionly"] == "1")
                {
                    plugins = plugins.Where(p => p.DownloadToUI);
                }

                return plugins;
            }
        }
    }
}
