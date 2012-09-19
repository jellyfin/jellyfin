using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    public class PluginConfigurationHandler : BaseSerializationHandler<BasePluginConfiguration>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("pluginconfiguration", request);
        }

        private BasePlugin _plugin;
        private BasePlugin Plugin
        {
            get
            {
                if (_plugin == null)
                {
                    string name = QueryString["assemblyfilename"];

                    _plugin = Kernel.Instance.Plugins.First(p => p.AssemblyFileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                }

                return _plugin;
            }
        }

        protected override Task<BasePluginConfiguration> GetObjectToSerialize()
        {
            return Task.FromResult(Plugin.Configuration);
        }

        protected override async Task<ResponseInfo> GetResponseInfo()
        {
            var info = await base.GetResponseInfo().ConfigureAwait(false);

            info.DateLastModified = Plugin.ConfigurationDateLastModified;

            info.CacheDuration = TimeSpan.FromDays(7);

            return info;
        }
    }
}
