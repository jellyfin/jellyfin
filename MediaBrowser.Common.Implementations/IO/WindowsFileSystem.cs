using Patterns.Logging;

namespace MediaBrowser.Common.Implementations.IO
{
    public class WindowsFileSystem : ManagedFileSystem
    {
        public WindowsFileSystem(ILogger logger)
            : base(logger, true, true)
        {
            AddShortcutHandler(new LnkShortcutHandler());
            EnableFileSystemRequestConcat = false;
        }
    }
}
