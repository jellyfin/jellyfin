using System;
using MediaBrowser.Model.System;

namespace MediaBrowser.Server.Mono.Native
{
    public class PowerManagement : IPowerManagement
    {
        public void PreventSystemStandby()
        {
        }

        public void AllowSystemStandby()
        {
        }

        public void ScheduleWake(DateTime wakeTimeUtc, string displayName)
        {
            // nothing to Do
        }
    }
}
