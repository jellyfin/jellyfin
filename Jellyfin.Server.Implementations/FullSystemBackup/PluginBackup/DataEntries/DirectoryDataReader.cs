using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class DirectoryDataReader : IPluginDataReader
{
    private readonly Guid _pluginId;
    private ZipArchive? _zipArchive;
    private string? _metadata;

    public DirectoryDataReader(ZipArchive zipArchive, string metadata, Guid pluginId)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
        _pluginId = pluginId;
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
            var fullSourcePath = DirectoryDataWriter.NormalizePathSeparator(Path.GetFullPath(source) + Path.DirectorySeparatorChar);
            var fullTargetRoot = Path.GetFullPath(target) + Path.DirectorySeparatorChar;
            foreach (var item in _zipArchive!.Entries)
            {
                var sourcePath = DirectoryDataWriter.NormalizePathSeparator(Path.GetFullPath(item.FullName));
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

        CopyDirectory($"plugin/{_pluginId:N}/{_metadata}", path);

        return ValueTask.CompletedTask;
    }
}
