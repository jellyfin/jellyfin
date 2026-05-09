using System;
using System.Globalization;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.System;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.StorageHelpers;

/// <summary>
/// Contains methods to help with checking for storage and returning storage data for jellyfin folders.
/// </summary>
public static class StorageHelper
{
    private const long TwoGigabyte = 2_147_483_647L;
    private static readonly string[] _byteHumanizedSuffixes = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

    /// <summary>
    /// Tests the available storage capacity on the jellyfin paths with estimated minimum values.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">Logger.</param>
    public static void TestCommonPathsForStorageCapacity(IApplicationPaths applicationPaths, ILogger logger)
    {
        TestDataDirectorySize(applicationPaths.DataPath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.CachePath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.ProgramDataPath, logger, TwoGigabyte);
    }

    /// <summary>
    /// Gets the free space of the parent filesystem of a specific directory.
    /// </summary>
    /// <param name="path">Path to a folder.</param>
    /// <returns>Various details about the parent filesystem containing the directory.</returns>
    public static FolderStorageInfo GetFreeSpaceOf(string path)
    {
        try
        {
            // Fully resolve the given path to an actual filesystem target, in case it's a symlink or similar.
            var resolvedPath = ResolvePath(path);
            // We iterate all filesystems reported by GetDrives() here, and attempt to find the best
            // match that contains, as deep as possible, the given path.
            // This is required because simply calling `DriveInfo` on a path returns that path as
            // the Name and RootDevice, which is not at all how this should work.
            var allDrives = DriveInfo.GetDrives();
            DriveInfo? bestMatch = null;
            foreach (DriveInfo d in allDrives)
            {
                if (resolvedPath.StartsWith(d.RootDirectory.FullName, StringComparison.InvariantCultureIgnoreCase) &&
                    (bestMatch is null || d.RootDirectory.FullName.Length > bestMatch.RootDirectory.FullName.Length))
                {
                    bestMatch = d;
                }
            }

            if (bestMatch is null)
            {
                throw new InvalidOperationException($"The path `{path}` has no matching parent device. Space check invalid.");
            }

            return new FolderStorageInfo()
            {
                Path = path,
                ResolvedPath = resolvedPath,
                FreeSpace = bestMatch.AvailableFreeSpace,
                UsedSpace = bestMatch.TotalSize - bestMatch.AvailableFreeSpace,
                StorageType = bestMatch.DriveType.ToString(),
                DeviceId = bestMatch.Name,
            };
        }
        catch
        {
            return new FolderStorageInfo()
            {
                Path = path,
                ResolvedPath = path,
                FreeSpace = -1,
                UsedSpace = -1,
                StorageType = null,
                DeviceId = null
            };
        }
    }

    /// <summary>
    /// Walk a path and fully resolve any symlinks within it.
    /// </summary>
    private static string ResolvePath(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        var current = Path.DirectorySeparatorChar.ToString();
        foreach (var part in parts)
        {
            current = Path.Combine(current, part);
            var resolved = new DirectoryInfo(current).ResolveLinkTarget(returnFinalTarget: true);
            if (resolved is not null)
            {
                current = resolved.FullName;
            }
        }

        return current;
    }

    /// <summary>
    /// Gets the underlying drive data from a given path and checks if the available storage capacity matches the threshold.
    /// </summary>
    /// <param name="path">The path to a folder to evaluate.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="threshold">The threshold to check for or -1 to just log the data.</param>
    /// <exception cref="InvalidOperationException">Thrown when the threshold is not available on the underlying storage.</exception>
    private static void TestDataDirectorySize(string path, ILogger logger, long threshold = -1)
    {
        logger.LogDebug("Check path {TestPath} for storage capacity", path);
        Directory.CreateDirectory(path);

        var drive = new DriveInfo(path);
        if (threshold != -1 && drive.AvailableFreeSpace < threshold)
        {
            throw new InvalidOperationException($"The path `{path}` has insufficient free space. Available: {HumanizeStorageSize(drive.AvailableFreeSpace)}, Required: {HumanizeStorageSize(threshold)}.");
        }

        logger.LogInformation(
            "Storage path `{TestPath}` ({StorageType}) successfully checked with {FreeSpace} free which is over the minimum of {MinFree}.",
            path,
            drive.DriveType,
            HumanizeStorageSize(drive.AvailableFreeSpace),
            HumanizeStorageSize(threshold));
    }

    /// <summary>
    /// Formats a size in bytes into a common human readable form.
    /// </summary>
    /// <remarks>
    /// Taken and slightly modified from https://stackoverflow.com/a/4975942/1786007 .
    /// </remarks>
    /// <param name="byteCount">The size in bytes.</param>
    /// <returns>A human readable approximate representation of the argument.</returns>
    public static string HumanizeStorageSize(long byteCount)
    {
        if (byteCount == 0)
        {
            return $"0{_byteHumanizedSuffixes[0]}";
        }

        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + _byteHumanizedSuffixes[place];
    }
}
