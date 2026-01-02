using System;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

internal interface IPluginDataWriter : IPluginDataEntry
{
    Type ReaderType { get; }

    ValueTask<string> BackupData(ZipArchive zipArchive, IPlugin plugin);
}

internal interface IPluginDataReader : IPluginDataEntry
{
}
