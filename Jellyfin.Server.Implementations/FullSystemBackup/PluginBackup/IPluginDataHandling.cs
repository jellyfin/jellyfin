using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

internal interface IPluginDataHandling : IPluginDataEntry
{
    ValueTask<string> BackupData(ZipArchive zipArchive, IPlugin plugin);

    ValueTask RestoreData(ZipArchive zipArchive, string metadata);
}
