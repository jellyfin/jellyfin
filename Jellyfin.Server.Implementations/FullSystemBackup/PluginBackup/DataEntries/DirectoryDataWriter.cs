using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a raw stream that is serializable in a backup.
/// </summary>
internal class DirectoryDataWriter : IPluginDataWriter
{
    private readonly string? _path;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDataWriter"/> class.
    /// </summary>
    /// <param name="path">The path to an existing directory to backup.</param>
    public DirectoryDataWriter(string path)
    {
        _path = path;
    }

    Type IPluginDataWriter.ReaderType => typeof(DirectoryDataReader);

    public Func<string, bool>? Filter { get; internal set; }

    ValueTask<string> IPluginDataWriter.BackupData(ZipArchive zipArchive, IPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(_path) || !Directory.Exists(_path))
        {
            throw new InvalidOperationException($"The backup of '{_path}' cannot be performed.");
        }

        var fileGuid = Guid.NewGuid().ToString("N");
        var metaReference = $"plugin/{plugin.Id:N}/{fileGuid}";

        void CopyDirectory(string source, string target, string filter = "*")
        {
            if (!Directory.Exists(source))
            {
                return;
            }

            foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
            {
                if (Filter is not null && Filter(item))
                {
                    continue;
                }

                zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine(target, Path.GetRelativePath(source, item))));
            }
        }

        CopyDirectory(_path, metaReference);

        return ValueTask.FromResult(fileGuid);
    }

    internal static string NormalizePathSeparator(string path)
        => path.Replace('\\', '/');
}
