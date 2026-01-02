using System;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

internal interface IPluginBackupAttribute
{
    Type LoaderType { get; }
}
