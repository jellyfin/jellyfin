using MediaBrowser.Model.Diagnostics;

namespace Emby.Common.Implementations.Diagnostics
{
    public class ProcessFactory : IProcessFactory
    {
        public IProcess Create(ProcessOptions options)
        {
            return new CommonProcess(options);
        }
    }
}
