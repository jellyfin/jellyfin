using System;
using MediaBrowser.Model.System;

namespace Emby.Server
{
    public class CoreSystemEvents : ISystemEvents
    {
        public event EventHandler Resume;
        public event EventHandler Suspend;
        public event EventHandler SessionLogoff;
        public event EventHandler SystemShutdown;
    }
}
