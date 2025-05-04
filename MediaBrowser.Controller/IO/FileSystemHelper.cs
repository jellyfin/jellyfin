using System;
using System.IO;
using System.Linq;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.IO;

/// <summary>
/// Helper methods for file system management.
/// </summary>
public static class FileSystemHelper
{
    /// <summary>
    /// Deletes the file.
    /// </summary>
    /// <param name="fileSystem">The fileSystem.</param>
    /// <param name="path">The path.</param>
    /// <param name="logger">The logger.</param>
    public static void DeleteFile(IFileSystem fileSystem, string path, ILogger logger)
    {
        try
        {
            fileSystem.DeleteFile(path);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Error deleting file {Path}", path);
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Error deleting file {Path}", path);
        }
    }

    /// <summary>
    /// Recursively delete empty folders.
    /// </summary>
    /// <param name="fileSystem">The fileSystem.</param>
    /// <param name="path">The path.</param>
    /// <param name="logger">The logger.</param>
    public static void DeleteEmptyFolders(IFileSystem fileSystem, string path, ILogger logger)
    {
        foreach (var directory in fileSystem.GetDirectoryPaths(path))
        {
            DeleteEmptyFolders(fileSystem, directory, logger);
            if (!fileSystem.GetFileSystemEntryPaths(directory).Any())
            {
                try
                {
                    Directory.Delete(directory, false);
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogError(ex, "Error deleting directory {Path}", directory);
                }
                catch (IOException ex)
                {
                    logger.LogError(ex, "Error deleting directory {Path}", directory);
                }
            }
        }
    }
}
