using System;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Startup.Common
{
    public class SystemEvents : ISystemEvents
    {
        public event EventHandler Resume;
        public event EventHandler Suspend;

        private readonly ILogger _logger;

        public SystemEvents(ILogger logger)
        {
            _logger = logger;
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case Microsoft.Win32.PowerModes.Resume:
                    EventHelper.FireEventIfNotNull(Resume, this, EventArgs.Empty, _logger);
                    break;
                case Microsoft.Win32.PowerModes.Suspend:
                    EventHelper.FireEventIfNotNull(Suspend, this, EventArgs.Empty, _logger);
                    break;
            }
        }
    }
}
