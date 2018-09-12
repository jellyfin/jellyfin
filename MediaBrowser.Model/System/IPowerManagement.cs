using System;

namespace MediaBrowser.Model.System
{
    public interface IPowerManagement
    {
        void PreventSystemStandby();
        void AllowSystemStandby();
        void ScheduleWake(DateTime wakeTimeUtc, string displayName);
    }
}
