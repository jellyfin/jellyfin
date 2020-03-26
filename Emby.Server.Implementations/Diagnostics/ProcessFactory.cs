#pragma warning disable CS1591

using System.Diagnostics;
using MediaBrowser.Model.Diagnostics;

namespace Emby.Server.Implementations.Diagnostics
{
    public class ProcessFactory : IProcessFactory
    {
        public Process Create(ProcessStartInfo startInfo)
        {
            return new Process { StartInfo = startInfo };
        }
    }
}
