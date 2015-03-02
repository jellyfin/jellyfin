using MediaBrowser.Controller.Diagnostics;
using System.Diagnostics;

namespace MediaBrowser.Server.Mono.Diagnostics
{
    public class ProcessManager : IProcessManager
    {
        public void SuspendProcess(Process process)
        {
            process.PriorityClass = ProcessPriorityClass.Idle;
        }

        public void ResumeProcess(Process process)
        {
            process.PriorityClass = ProcessPriorityClass.Normal;
        }

        public bool SupportsSuspension
        {
            get { return true; }
        }
    }
}
