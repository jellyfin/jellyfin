using System;
using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Resolver rule class for ignoring files via .ignore.
/// </summary>
public class DotIgnoreIgnoreRule : IResolverIgnoreRule
{
    private static readonly bool IsWindows = OperatingSystem.IsWindows();

    private static FileInfo? FindIgnoreFile(DirectoryInfo directory)
    {
        for (var current = directory; current is not null; current = current.Parent)
        {
            var ignorePath = Path.Join(current.FullName, ".ignore");
            if (File.Exists(ignorePath))
            {
                return new FileInfo(ignorePath);
            }
        }

        return null;
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent) => IsIgnored(fileInfo, parent);

    /// <summary>
    /// Checks whether or not the file is ignored.
    /// </summary>
    /// <param name="fileInfo">The file information.</param>
    /// <param name="parent">The parent BaseItem.</param>
    /// <returns>True if the file should be ignored.</returns>
    public static bool IsIgnored(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var searchDirectory = fileInfo.IsDirectory
            ? new DirectoryInfo(fileInfo.FullName)
            : new DirectoryInfo(Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty);

        if (string.IsNullOrEmpty(searchDirectory.FullName))
        {
            return false;
        }

        var ignoreFile = FindIgnoreFile(searchDirectory);
        if (ignoreFile is null)
        {
            return false;
        }

        // Fast path in case the ignore files isn't a symlink and is empty
        if (ignoreFile.LinkTarget is null && ignoreFile.Length == 0)
        {
            // Ignore directory if we just have the file
            return true;
        }

        var content = GetFileContent(ignoreFile);
        return string.IsNullOrWhiteSpace(content)
            || CheckIgnoreRules(fileInfo.FullName, content, fileInfo.IsDirectory);
    }

    private static bool CheckIgnoreRules(string path, string ignoreFileContent, bool isDirectory)
    {
        // If file has content, base ignoring off the content .gitignore-style rules
        var rules = ignoreFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ignore = new Ignore.Ignore();
        ignore.Add(rules);

         // Mitigate the problem of the Ignore library not handling Windows paths correctly.
         // See https://github.com/jellyfin/jellyfin/issues/15484
        var pathToCheck = IsWindows ? path.NormalizePath('/') : path;

        // Add trailing slash for directories to match "folder/"
        if (isDirectory)
        {
            pathToCheck = string.Concat(pathToCheck.AsSpan().TrimEnd('/'), "/");
        }

        return ignore.IsIgnored(pathToCheck);
    }

    private static string GetFileContent(FileInfo ignoreFile)
    {
        ignoreFile = FileSystemHelper.ResolveLinkTarget(ignoreFile, returnFinalTarget: true) ?? ignoreFile;
        return ignoreFile.Exists
            ? File.ReadAllText(ignoreFile.FullName)
            : string.Empty;
    }
}
