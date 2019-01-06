using System;
using MediaBrowser.Model.System;

namespace Jellyfin.Server.Native
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

        }
    }
}
