using System.ServiceProcess;

namespace MediaBrowser.ServerApplication
{
    public class BackgroundService : ServiceBase
    {
        public BackgroundService()
        {
            CanPauseAndContinue = false;
            CanHandleSessionChangeEvent = true;
            CanStop = false;
            CanShutdown = true;
            ServiceName = "Media Browser";
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }

        protected override void OnStart(string[] args)
        {
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
    }
}
