using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides information about installed plugins
    /// </summary>
    public class PluginsHandler : BaseJsonHandler<IEnumerable<PluginInfo>>
    {
        protected override IEnumerable<PluginInfo> GetObjectToSerialize()
        {
            var plugins = Kernel.Instance.Plugins.Select(p =>
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
