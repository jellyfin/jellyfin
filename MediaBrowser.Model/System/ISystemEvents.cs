using System;

namespace MediaBrowser.Model.System
{
    public interface ISystemEvents
    {
        event EventHandler Resume;
        event EventHandler Suspend;
        event EventHandler SessionLogoff;
        event EventHandler SystemShutdown;
    }
}
