using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// This data entry is designed to be used for small strings from a plugin like configuration settings.
/// </summary>
internal class StringDataEntry : IPluginDataReader, IPluginDataWriter
{
    private readonly ZipArchive? _zipArchive;
    private string? _metadata;

    public StringDataEntry(ZipArchive zipArchive, string metadata, Guid pluginId)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
    }

    [SetsRequiredMembers]
    internal StringDataEntry(string value)
    {
        _metadata = value;
    }

    /// <summary>
    /// Gets the backup value.
    /// </summary>
    public required string? Value
    {
        get => _metadata;
        init => _metadata = value;
    }

    Type IPluginDataWriter.ReaderType => typeof(StringDataEntry);

    ValueTask<string> IPluginDataWriter.BackupData(ZipArchive zipArchive, IPlugin plugin)
    {
        // this entry stores values on the plugin manifest and does not need special handling.
        return ValueTask.FromResult(_metadata)!;
    }
}
