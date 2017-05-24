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
        public event EventHandler SessionLogoff;
        public event EventHandler SystemShutdown;

        private readonly ILogger _logger;

        public SystemEvents(ILogger logger)
        {
            _logger = logger;
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            Microsoft.Win32.SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        private void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            switch (e.Reason)
            {
                case Microsoft.Win32.SessionEndReasons.Logoff:
                    EventHelper.FireEventIfNotNull(SessionLogoff, this, EventArgs.Empty, _logger);
                    break;
                case Microsoft.Win32.SessionEndReasons.SystemShutdown:
                    EventHelper.FireEventIfNotNull(SystemShutdown, this, EventArgs.Empty, _logger);
                    break;
            }
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
