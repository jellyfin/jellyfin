using Emby.Common.Implementations.IO;
using MediaBrowser.Model.Logging;
using Mono.Unix.Native;
using MediaBrowser.Model.System;

namespace Emby.Server.Mac.Native
{
    public class MonoFileSystem : ManagedFileSystem
    {
        public MonoFileSystem(ILogger logger, IEnvironmentInfo environmentInfo, string tempPath) 
			: base(logger, environmentInfo, tempPath)
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
