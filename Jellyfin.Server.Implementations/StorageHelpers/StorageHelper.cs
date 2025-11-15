using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
    private const long FiveHundredAndTwelveMegaByte = 536_870_911L;
    private static readonly string[] _byteHumanizedSuffixes = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

    private static readonly ConcurrentDictionary<string, (long Size, DateTime Expires)> _sizeCache =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Tests the available storage capacity on the jellyfin paths with estimated minimum values.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">Logger.</param>
    public static void TestCommonPathsForStorageCapacity(
        IApplicationPaths applicationPaths,
        ILogger logger)
    {
        TestDataDirectorySize(applicationPaths.DataPath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.LogDirectoryPath, logger, FiveHundredAndTwelveMegaByte);
        TestDataDirectorySize(applicationPaths.CachePath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.ProgramDataPath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.TempDirectory, logger, FiveHundredAndTwelveMegaByte);
    }

    /// <summary>
    /// Gets the free space of a specific directory.
    /// </summary>
    /// <param name="path">Path to a folder.</param>
    /// <returns>The folder storage info with free space, used space, and drive info.</returns>
    public static FolderStorageInfo GetFreeSpaceOf(string path)
    {
        try
        {
            var driveInfo = new DriveInfo(path);
            return new FolderStorageInfo
            {
                Path = path,
                FreeSpace = driveInfo.AvailableFreeSpace,
                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                StorageType = driveInfo.DriveType.ToString(),
                DeviceId = driveInfo.Name,
                DriveTotalBytes = driveInfo.IsReady ? driveInfo.TotalSize : (long?)null
            };
        }
        catch
        {
            return new FolderStorageInfo
            {
                Path = path,
                FreeSpace = -1,
                UsedSpace = -1,
                StorageType = null,
                DeviceId = null,
                DriveTotalBytes = null
            };
        }
    }

    /// <summary>
    /// Asynchronously computes and caches the folder size.
    /// </summary>
    /// <param name="path">The folder path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The folder size in bytes, or null if failed.</returns>
    public static async Task<long?> ComputeAndCacheFolderSizeAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        try
        {
            var size = await GetDirectorySizeAsync(path, cancellationToken).ConfigureAwait(false);
            _sizeCache[path] = (size, DateTime.UtcNow.Add(_cacheTtl));
            return size;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the cached folder size, if available.
    /// </summary>
    /// <param name="path">The folder path.</param>
    /// <returns>The cached folder size in bytes, or null if unavailable.</returns>
    public static long? GetCachedFolderSize(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }

        if (_sizeCache.TryGetValue(path, out var entry) && entry.Expires > DateTime.UtcNow)
        {
            return entry.Size;
        }

        return null;
    }

    /// <summary>
    /// Gets the total drive size for a given path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The drive total bytes or null if unavailable.</returns>
    public static long? GetDriveTotalBytes(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var root = Path.GetPathRoot(path);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var drive = new DriveInfo(root);
            return drive.IsReady ? drive.TotalSize : (long?)null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Recursively calculates the total folder size asynchronously.
    /// </summary>
    /// <param name="path">Folder path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total size in bytes.</returns>
    private static Task<long> GetDirectorySizeAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () =>
            {
                long size = 0;
                var stack = new Stack<string>();
                stack.Push(path);

                while (stack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var current = stack.Pop();

                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(current))
                        {
                            try
                            {
                                var fi = new FileInfo(file);
                                size += fi.Length;
                            }
                            catch
                            {
                                // Skip unreadable file.
                            }
                        }

                        foreach (var dir in Directory.EnumerateDirectories(current))
                        {
                            stack.Push(dir);
                        }
                    }
                    catch
                    {
                        // Skip unreadable directory.
                    }
                }

                return size;
            },
            cancellationToken);
    }

    /// <summary>
    /// Gets the underlying drive data from a given path and checks if the available storage capacity matches the threshold.
    /// </summary>
    /// <param name="path">The path to a folder to evaluate.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="threshold">The threshold to check for or -1 to just log the data.</param>
    /// <exception cref="InvalidOperationException">Thrown when the threshold is not available on the underlying storage.</exception>
    private static void TestDataDirectorySize(
        string path,
        ILogger logger,
        long threshold = -1)
    {
        logger.LogDebug("Check path {TestPath} for storage capacity", path);
        Directory.CreateDirectory(path);

        var drive = new DriveInfo(path);
        if (threshold != -1 && drive.AvailableFreeSpace < threshold)
        {
            throw new InvalidOperationException(
                $"The path `{path}` has insufficient free space. " +
                $"Available: {HumanizeStorageSize(drive.AvailableFreeSpace)}, " +
                $"Required: {HumanizeStorageSize(threshold)}.");
        }

        logger.LogInformation(
            "Storage path `{TestPath}` ({StorageType}) successfully checked with {FreeSpace} free which is over the minimum of {MinFree}.",
            path,
            drive.DriveType,
            HumanizeStorageSize(drive.AvailableFreeSpace),
            HumanizeStorageSize(threshold));
    }

    /// <summary>
    /// Formats a size in bytes into a human-readable form.
    /// </summary>
    /// <param name="byteCount">The size in bytes.</param>
    /// <returns>A human-readable approximate representation of the argument.</returns>
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
