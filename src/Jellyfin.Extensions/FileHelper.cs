using System.IO;

namespace Jellyfin.Extensions;

/// <summary>
/// Provides helper functions for <see cref="File" />.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Creates, or truncates a file in the specified path.
    /// </summary>
    /// <param name="path">The path and name of the file to create.</param>
    public static void CreateEmpty(string path)
    {
        using (File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
        }
    }
}
