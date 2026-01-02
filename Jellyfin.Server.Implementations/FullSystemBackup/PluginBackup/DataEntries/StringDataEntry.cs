using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// This data entry is designed to be used for small strings from a plugin like configuration settings.
/// </summary>
public class StringDataEntry : IPluginDataHandling
{
    private string? _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="StringDataEntry"/> class.
    /// </summary>
    public StringDataEntry()
    {
    }

    /// <summary>
    /// Gets the backup value.
    /// </summary>
    public required string? Value
    {
        get => _metadata;
        init => _metadata = value;
    }

    ValueTask<string> IPluginDataHandling.BackupData(ZipArchive zipArchive, IPlugin plugin)
    {
        // this entry stores values on the plugin manifest and does not need special handling.
        return ValueTask.FromResult(_metadata)!;
    }

    ValueTask IPluginDataHandling.RestoreData(ZipArchive zipArchive, string metadata)
    {
        _metadata = metadata;
        return ValueTask.CompletedTask;
    }
}
