using Jellyfin.Model.Diagnostics;

namespace Jellyfin.Server.Implementations.Diagnostics
{
    public class ProcessFactory : IProcessFactory
    {
        public IProcess Create(ProcessOptions options)
        {
            return new CommonProcess(options);
        }
    }
}
