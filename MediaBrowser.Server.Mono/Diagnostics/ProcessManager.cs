using MediaBrowser.Controller.Diagnostics;
using System;
using System.Diagnostics;

namespace MediaBrowser.Server.Mono.Diagnostics
{
    public class ProcessManager : IProcessManager
    {
        public void SuspendProcess(Process process)
        {
            throw new NotImplementedException();
        }

        public void ResumeProcess(Process process)
        {
            throw new NotImplementedException();
        }

        public bool SupportsSuspension
        {
            get { return false; }
        }
    }
}
