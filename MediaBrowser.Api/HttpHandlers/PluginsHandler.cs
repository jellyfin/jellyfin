using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides information about installed plugins
    /// </summary>
    public class PluginsHandler : BaseSerializationHandler<IEnumerable<PluginInfo>>
    {
        protected override Task<IEnumerable<PluginInfo>> GetObjectToSerialize()
        {
            var plugins = Kernel.Instance.Plugins.Select(p =>
            {
                return new PluginInfo()
                {
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

            return Task.FromResult<IEnumerable<PluginInfo>>(plugins);
        }
    }
}
