using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Gets the target of the specified file link.
    /// </summary>
    /// <remarks>
    /// This helper exists because of this upstream runtime issue; https://github.com/dotnet/runtime/issues/92128.
    /// </remarks>
    /// <param name="linkPath">The path of the file link.</param>
    /// <param name="returnFinalTarget">true to follow links to the final target; false to return the immediate next link.</param>
    /// <returns>
    /// A <see cref="FileInfo"/> if the <paramref name="linkPath"/> is a link, regardless of if the target exists; otherwise, <c>null</c>.
    /// </returns>
    public static FileInfo? ResolveLinkTarget(string linkPath, bool returnFinalTarget = false)
    {
        // Check if the file exists so the native resolve handler won't throw at us.
        if (!File.Exists(linkPath))
        {
            return null;
        }

        if (!returnFinalTarget)
        {
            return File.ResolveLinkTarget(linkPath, returnFinalTarget: false) as FileInfo;
        }

        if (File.ResolveLinkTarget(linkPath, returnFinalTarget: false) is not FileInfo targetInfo)
        {
            return null;
        }

        if (!targetInfo.Exists)
        {
            return targetInfo;
        }

        var currentPath = targetInfo.FullName;
        var visited = new HashSet<string>(StringComparer.Ordinal) { linkPath, currentPath };
        while (File.ResolveLinkTarget(currentPath, returnFinalTarget: false) is FileInfo linkInfo)
        {
            var targetPath = linkInfo.FullName;

            // If an infinite loop is detected, return the file info for the
            // first link in the loop we encountered.
            if (!visited.Add(targetPath))
            {
                return new FileInfo(targetPath);
            }

            targetInfo = linkInfo;
            currentPath = targetPath;

            // Exit if the target doesn't exist, so the native resolve handler won't throw at us.
            if (!targetInfo.Exists)
            {
                break;
            }
        }

        return targetInfo;
    }

    /// <summary>
    /// Gets the target of the specified file link.
    /// </summary>
    /// <remarks>
    /// This helper exists because of this upstream runtime issue; https://github.com/dotnet/runtime/issues/92128.
    /// </remarks>
    /// <param name="fileInfo">The file info of the file link.</param>
    /// <param name="returnFinalTarget">true to follow links to the final target; false to return the immediate next link.</param>
    /// <returns>
    /// A <see cref="FileInfo"/> if the <paramref name="fileInfo"/> is a link, regardless of if the target exists; otherwise, <c>null</c>.
    /// </returns>
    public static FileInfo? ResolveLinkTarget(FileInfo fileInfo, bool returnFinalTarget = false)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);

        return ResolveLinkTarget(fileInfo.FullName, returnFinalTarget);
    }
}
