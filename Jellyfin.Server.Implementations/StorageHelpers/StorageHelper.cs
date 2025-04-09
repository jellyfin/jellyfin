using System;
using System.Globalization;
using System.IO;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.StorageHelpers;

/// <summary>
/// Contains methods to help with checking for storage and returning storage data for jellyfin folders.
/// </summary>
public static class StorageHelper
{
    private const long TwoGigabyte = 2147483647L;
    private const long FiveHundredAndTwelveMegaByte = 536_870_911L;

    /// <summary>
    /// Tests the available storage capacity on the jellyfin paths with estimated minimum values.
    /// </summary>
    /// <param name="applicationPaths">The application paths.</param>
    /// <param name="logger">Logger.</param>
    public static void TestCommonJfPathsForStorageCapacity(IApplicationPaths applicationPaths, ILogger logger)
    {
        TestDataDirectorySize(applicationPaths.DataPath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.LogDirectoryPath, logger, FiveHundredAndTwelveMegaByte);
        TestDataDirectorySize(applicationPaths.CachePath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.ProgramDataPath, logger, TwoGigabyte);
        TestDataDirectorySize(applicationPaths.TempDirectory, logger, TwoGigabyte);
    }

    private static void TestDataDirectorySize(string path, ILogger logger, long threshold = -1)
    {
        logger.LogDebug("Check path {TestPath} for storage capacity", path);
        var drive = new DriveInfo(path);
        if (threshold != -1 && drive.AvailableFreeSpace < threshold)
        {
            throw new InvalidOperationException($"The path `{path}` exceeds the minimum required free capacity of {BytesToString(threshold)}");
        }

        logger.LogInformation(
            "Storage path `{TestPath}` ({StorageType}) successfully tested with {FreeSpace} left free which is over the minimum of {MinFree}.",
            path,
            drive.DriveType,
            BytesToString(drive.AvailableFreeSpace),
            BytesToString(threshold));
    }

    /// <summary>
    /// Formats a size in bytes into a common human readable form.
    /// </summary>
    /// <remarks>
    /// Taken and slightly modified from https://stackoverflow.com/a/4975942/1786007 .
    /// </remarks>
    /// <param name="byteCount">The size in bytes.</param>
    /// <returns>A human readable approximate representation of the argument.</returns>
    public static string BytesToString(long byteCount)
    {
        string[] suf = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];

        if (byteCount == 0)
        {
            return $"0{suf[0]}";
        }

        var bytes = Math.Abs(byteCount);
        var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        var num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
    }
}
