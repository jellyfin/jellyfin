using MediaBrowser.Model.Logging;

namespace MediaBrowser.Common.Implementations.IO
{
    public class WindowsFileSystem : ManagedFileSystem
    {
        public WindowsFileSystem(ILogger logger)
            : base(logger, true, true)
        {
            EnableFileSystemRequestConcat = false;
        }
    }
}
