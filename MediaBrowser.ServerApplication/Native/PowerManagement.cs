using MediaBrowser.Model.System;

namespace MediaBrowser.ServerApplication.Native
{
    public class PowerManagement : IPowerManagement
    {
        public void PreventSystemStandby()
        {
            MainStartup.Invoke(Standby.PreventSleep);
        }

        public void AllowSystemStandby()
        {
            MainStartup.Invoke(Standby.AllowSleep);
        }
    }
}
