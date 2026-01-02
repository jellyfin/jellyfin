using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines a single data entry from a plugin.
/// </summary>
public interface IPluginDataEntry
{
}

internal interface IPluginDataHandling : IPluginDataEntry
{
    ValueTask<string> BackupData(ZipArchive zipArchive, IPlugin plugin);

    ValueTask RestoreData(ZipArchive zipArchive, string metadata);
}
