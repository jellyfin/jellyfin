using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
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

    private static readonly ConcurrentDictionary<string, DotIgnoreFile> _ignoreFileCache = new(StringComparer.Ordinal);

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

        // Check if ignore file is cached and if it has changed (based on change date)
        _ignoreFileCache.TryGetValue(ignoreFile.FullName, out DotIgnoreFile? cachedIgnoreFile);

        Ignore.Ignore? ignoreRules;
        if (cachedIgnoreFile is not null && cachedIgnoreFile.ChangedDate.Equals(ignoreFile.LastWriteTimeUtc))
        {
            ignoreRules = cachedIgnoreFile.IgnoreRules;
        }
        else
        {
            // If file has content, check for .gitignore-style ignore rules
            ignoreRules = GetIgnoreRules(GetFileContent(ignoreFile));
            DotIgnoreFile ignoreFileInfo = new(ignoreFile.FullName, ignoreFile.LastWriteTimeUtc, ignoreRules);
            _ignoreFileCache[ignoreFile.FullName] = ignoreFileInfo;
        }

        // If no valid rules exist, fall back to ignoring everything (like an empty .ignore file)
        if (ignoreRules is null)
        {
            return true;
        }

        return CheckIgnoreRules(fileInfo.FullName, ignoreRules, fileInfo.IsDirectory, IsWindows);
    }

    /// <summary>
    /// Checks whether a path should be ignored based on an array of ignore rules.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="rules">The array of ignore rules.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <returns>True if the path should be ignored.</returns>
    internal static bool CheckIgnoreRules(string path, string[] rules, bool isDirectory)
        => CheckIgnoreRules(path, rules, isDirectory, IsWindows);

    /// <summary>
    /// Checks whether a path should be ignored based on an array of ignore rules.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="rules">The array of ignore rules.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <param name="normalizePath">Whether to normalize backslashes to forward slashes (for Windows paths).</param>
    /// <returns>True if the path should be ignored.</returns>
    internal static bool CheckIgnoreRules(string path, string[] rules, bool isDirectory, bool normalizePath)
    {
        var ignore = GetIgnoreRules(rules);

        // If no valid rules were added, fall back to ignoring everything (like an empty .ignore file)
        if (ignore is null)
        {
            return true;
        }

        return CheckIgnoreRules(path, ignore, isDirectory, IsWindows);
    }

    /// <summary>
    /// Checks whether a path should be ignored based on an array of ignore rules.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="ignore">The ignore rules.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <param name="normalizePath">Whether to normalize backslashes to forward slashes (for Windows paths).</param>
    /// <returns>True if the path should be ignored.</returns>
    internal static bool CheckIgnoreRules(string path, Ignore.Ignore ignore, bool isDirectory, bool normalizePath)
    {
        // Mitigate the problem of the Ignore library not handling Windows paths correctly.
        // See https://github.com/jellyfin/jellyfin/issues/15484
        var pathToCheck = normalizePath ? path.NormalizePath('/') : path;

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

    private static Ignore.Ignore? GetIgnoreRules(string ignoreFileContent)
    {
        if (string.IsNullOrWhiteSpace(ignoreFileContent))
        {
            return null;
        }
        else
        {
            var rules = ignoreFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return GetIgnoreRules(rules);
        }
    }

    private static Ignore.Ignore? GetIgnoreRules(string[] rules)
    {
        var ignore = new Ignore.Ignore();

        // Add each rule individually to catch and skip invalid patterns
        var validRulesAdded = 0;
        foreach (var rule in rules)
        {
            try
            {
                ignore.Add(rule);
                validRulesAdded++;
            }
            catch (RegexParseException)
            {
                // Ignore invalid patterns
            }
        }

        return validRulesAdded > 0
            ? ignore
            : null;
    }
}
