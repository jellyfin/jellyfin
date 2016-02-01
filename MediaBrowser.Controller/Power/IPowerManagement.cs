using System;

namespace MediaBrowser.Controller.Power
{
    public interface IPowerManagement
    {
        /// <summary>
        /// Schedules the wake.
        /// </summary>
        /// <param name="utcTime">The UTC time.</param>
        void ScheduleWake(DateTime utcTime);
    }
}
