using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides information about installed plugins
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class PluginsHandler : BaseSerializationHandler<IEnumerable<PluginInfo>>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("plugins", request);
        }

        protected override Task<IEnumerable<PluginInfo>> GetObjectToSerialize()
        {
            var plugins = Kernel.Instance.Plugins.Select(p => new PluginInfo
            {
                Name = p.Name,
                Enabled = p.Enabled,
                DownloadToUI = p.DownloadToUi,
                Version = p.Version.ToString(),
                AssemblyFileName = p.AssemblyFileName,
                ConfigurationDateLastModified = p.ConfigurationDateLastModified
            });

            return Task.FromResult(plugins);
        }
    }
}
