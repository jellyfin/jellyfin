using MediaBrowser.Controller.Diagnostics;
using System.Diagnostics;

namespace MediaBrowser.Server.Mono.Diagnostics
{
    public class LinuxProcessManager : IProcessManager
    {
        public bool SupportsSuspension
        {
            get { return true; }
        }

        public void SuspendProcess(Process process)
        {
            // http://jumptuck.com/2011/11/23/quick-tip-pause-process-linux/
            process.StandardInput.WriteLine("^Z");
        }

        public void ResumeProcess(Process process)
        {
            // http://jumptuck.com/2011/11/23/quick-tip-pause-process-linux/
            process.StandardInput.WriteLine("fg");
        }
    }
}
