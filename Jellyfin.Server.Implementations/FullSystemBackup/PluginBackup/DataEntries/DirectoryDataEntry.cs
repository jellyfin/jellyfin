using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a raw stream that is serializable in a backup.
/// </summary>
public class DirectoryDataEntry : IPluginDataHandling
{
    private readonly string? _path;
    private ZipArchive? _zipArchive;
    private string? _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDataEntry"/> class.
    /// </summary>
    /// <param name="path">The path to an existing directory to backup.</param>
    public DirectoryDataEntry(string path)
    {
        _path = path;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectoryDataEntry"/> class.
    /// </summary>
    public DirectoryDataEntry()
    {
    }

    /// <summary>
    /// Restores a backuped directory to the provided path.
    /// </summary>
    /// <param name="path">The target path where to store the contents of the directory to.</param>
    /// <returns>A task that completes once the restore has been finished.</returns>
    public ValueTask RestoreDirectory(string path)
    {
        if (_zipArchive is null || string.IsNullOrWhiteSpace(_metadata))
        {
            throw new InvalidOperationException("Cannot restore a directory from this object.");
        }

        void CopyDirectory(string source, string target)
        {
            var fullSourcePath = NormalizePathSeparator(Path.GetFullPath(source) + Path.DirectorySeparatorChar);
            var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
            foreach (var item in _zipArchive!.Entries)
            {
                var sourcePath = NormalizePathSeparator(Path.GetFullPath(item.FullName));
                var targetPath = Path.GetFullPath(Path.Combine(target, Path.GetRelativePath(source, item.FullName)));

                if (!sourcePath.StartsWith(fullSourcePath, StringComparison.Ordinal)
                    || !targetPath.StartsWith(fullTargetRoot, StringComparison.Ordinal)
                    || Path.EndsInDirectorySeparator(item.FullName))
                {
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                item.ExtractToFile(targetPath, overwrite: true);
            }
        }

        CopyDirectory(_metadata, path);

        return ValueTask.CompletedTask;
    }

    ValueTask<string> IPluginDataHandling.BackupData(ZipArchive zipArchive, IPlugin plugin)
    {
        if (string.IsNullOrWhiteSpace(_path) || !Directory.Exists(_path))
        {
            throw new InvalidOperationException($"The backup of '{_path}' cannot be performed.");
        }

        var fileGuid = Guid.NewGuid().ToString("g");
        var metaReference = $"plugin/{plugin.Id}/{fileGuid}";

        void CopyDirectory(string source, string target, string filter = "*")
        {
            if (!Directory.Exists(source))
            {
                return;
            }

            foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
            {
                zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine(target, Path.GetRelativePath(source, item))));
            }
        }

        CopyDirectory(_path, metaReference);

        return ValueTask.FromResult(fileGuid);
    }

    ValueTask IPluginDataHandling.RestoreData(ZipArchive zipArchive, string metadata)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
        return ValueTask.CompletedTask;
    }

    private static string NormalizePathSeparator(string path)
        => path.Replace('\\', '/');
}
