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
        
        private BasePlugin _Plugin = null;
        private BasePlugin Plugin
        {
            get
            {
                if (_Plugin == null)
                {
                    string name = QueryString["assemblyfilename"];

                    _Plugin = Kernel.Instance.Plugins.First(p => p.AssemblyFileName.Equals(name, StringComparison.OrdinalIgnoreCase));
                }

                return _Plugin;
            }
        }

        protected override Task<BasePluginConfiguration> GetObjectToSerialize()
        {
            return Task.FromResult<BasePluginConfiguration>(Plugin.Configuration);
        }

        public override TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromDays(7);
            }
        }

        protected override Task<DateTime?> GetLastDateModified()
        {
            return Task.FromResult<DateTime?>(Plugin.ConfigurationDateLastModified);
        }

    }
}
