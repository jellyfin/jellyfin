using MediaBrowser.Model.Diagnostics;

namespace Emby.Server.Implementations.Diagnostics
{
    public class ProcessFactory : IProcessFactory
    {
        public IProcess Create(ProcessOptions options)
        {
            return new CommonProcess(options);
        }
    }
}
