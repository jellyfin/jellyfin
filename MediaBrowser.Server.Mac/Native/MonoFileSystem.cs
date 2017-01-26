using Emby.Common.Implementations.IO;
using MediaBrowser.Model.Logging;
using Mono.Unix.Native;

namespace Emby.Server.Mac.Native
{
    public class MonoFileSystem : ManagedFileSystem
    {
        public MonoFileSystem(ILogger logger, bool supportsAsyncFileStreams, bool enableManagedInvalidFileNameChars, string tempPath) 
			: base(logger, supportsAsyncFileStreams, enableManagedInvalidFileNameChars, true, tempPath)
        {
        }

        public override void SetExecutable(string path)
        {
            // Linux: File permission to 666, and user's execute bit
            Logger.Info("Syscall.chmod {0} FilePermissions.DEFFILEMODE | FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH", path);

            Syscall.chmod(path, FilePermissions.DEFFILEMODE | FilePermissions.S_IRWXU | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH);
        }
    }
}
