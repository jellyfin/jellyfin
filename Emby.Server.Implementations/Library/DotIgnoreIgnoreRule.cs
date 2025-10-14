using System;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Resolver rule class for ignoring files via .ignore.
/// </summary>
public class DotIgnoreIgnoreRule : IResolverIgnoreRule
{
    private static FileInfo? FindIgnoreFile(DirectoryInfo directory)
    {
        var ignoreFile = new FileInfo(Path.Join(directory.FullName, ".ignore"));
        if (ignoreFile.Exists)
        {
            return ignoreFile;
        }

        var parentDir = directory.Parent;
        if (parentDir is null)
        {
            return null;
        }

        return FindIgnoreFile(parentDir);
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        return IsIgnored(fileInfo, parent);
    }

    /// <summary>
    /// Checks whether or not the file is ignored.
    /// </summary>
    /// <param name="fileInfo">The file information.</param>
    /// <param name="parent">The parent BaseItem.</param>
    /// <returns>True if the file should be ignored.</returns>
    public static bool IsIgnored(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        if (fileInfo.IsDirectory)
        {
            var dirIgnoreFile = FindIgnoreFile(new DirectoryInfo(fileInfo.FullName));
            if (dirIgnoreFile is null)
            {
                return false;
            }

            // Fast path in case the ignore files isn't a symlink and is empty
            if ((dirIgnoreFile.Attributes & FileAttributes.ReparsePoint) == 0
                && dirIgnoreFile.Length == 0)
            {
                return true;
            }

            // ignore the directory only if the .ignore file is empty
            // evaluate individual files otherwise
            return string.IsNullOrWhiteSpace(GetFileContent(dirIgnoreFile));
        }

        var parentDirPath = Path.GetDirectoryName(fileInfo.FullName);
        if (string.IsNullOrEmpty(parentDirPath))
        {
            return false;
        }

        var folder = new DirectoryInfo(parentDirPath);
        var ignoreFile = FindIgnoreFile(folder);
        if (ignoreFile is null)
        {
            return false;
        }

        string ignoreFileString = GetFileContent(ignoreFile);

        if (string.IsNullOrWhiteSpace(ignoreFileString))
        {
            // Ignore directory if we just have the file
            return true;
        }

        // If file has content, base ignoring off the content .gitignore-style rules
        var ignoreRules = ignoreFileString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var ignore = new Ignore.Ignore();
        ignore.Add(ignoreRules);

        return ignore.IsIgnored(fileInfo.FullName);
    }

    private static string GetFileContent(FileInfo dirIgnoreFile)
    {
        using (var reader = dirIgnoreFile.OpenText())
        {
            return reader.ReadToEnd();
        }
    }
}
