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
    /// Gets the free space of a specific directory.
    /// </summary>
    /// <param name="path">Path to a folder.</param>
    /// <returns>The number of bytes available space.</returns>
    public static FolderStorageInfo GetFreeSpaceOf(string path)
    {
        try
        {
            var driveInfo = new DriveInfo(path);
            return new FolderStorageInfo()
            {
                Path = path,
                FreeSpace = driveInfo.AvailableFreeSpace,
                UsedSpace = driveInfo.TotalSize - driveInfo.AvailableFreeSpace,
                StorageType = driveInfo.DriveType.ToString(),
                DeviceId = driveInfo.Name,
            };
        }
        catch
        {
            return new FolderStorageInfo()
            {
                Path = path,
                FreeSpace = -1,
                UsedSpace = -1,
                StorageType = null,
                DeviceId = null
            };
        }
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
