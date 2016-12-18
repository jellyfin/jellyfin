using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.System;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Common;

namespace Emby.Server.Implementations.EntryPoints
{
    public class SystemEvents : IServerEntryPoint
    {
        private readonly ISystemEvents _systemEvents;
        private readonly IApplicationHost _appHost;

        public SystemEvents(ISystemEvents systemEvents, IApplicationHost appHost)
        {
            _systemEvents = systemEvents;
            _appHost = appHost;
        }

        public void Run()
        {
            _systemEvents.SessionLogoff += _systemEvents_SessionLogoff;
            _systemEvents.SystemShutdown += _systemEvents_SystemShutdown;
        }

        private void _systemEvents_SessionLogoff(object sender, EventArgs e)
        {
            if (!_appHost.IsRunningAsService)
            {
                _appHost.Shutdown();
            }
        }

        private void _systemEvents_SystemShutdown(object sender, EventArgs e)
        {
            _appHost.Shutdown();
        }

        public void Dispose()
        {
            _systemEvents.SystemShutdown -= _systemEvents_SystemShutdown;
        }
    }
}
