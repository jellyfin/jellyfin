using System;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.EntryPoints
{
    public class SystemEvents : IServerEntryPoint
    {
        private readonly ISystemEvents _systemEvents;
        private readonly IServerApplicationHost _appHost;

        public SystemEvents(ISystemEvents systemEvents, IServerApplicationHost appHost)
        {
            _systemEvents = systemEvents;
            _appHost = appHost;
        }

        public void Run()
        {
            _systemEvents.SystemShutdown += _systemEvents_SystemShutdown;
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
