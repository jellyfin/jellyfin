using System;
using System.IO;
using System.Text.RegularExpressions;
using BitFaster.Caching.Lru;
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

    private readonly FastConcurrentLru<string, IgnoreFileCacheEntry> _directoryCache;
    private readonly FastConcurrentLru<string, ParsedIgnoreCacheEntry> _rulesCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotIgnoreIgnoreRule"/> class.
    /// </summary>
    public DotIgnoreIgnoreRule()
    {
        var cacheSize = Math.Max(100, Environment.ProcessorCount * 100);
        _directoryCache = new FastConcurrentLru<string, IgnoreFileCacheEntry>(
            Environment.ProcessorCount,
            cacheSize,
            StringComparer.Ordinal);
        _rulesCache = new FastConcurrentLru<string, ParsedIgnoreCacheEntry>(
            Environment.ProcessorCount,
            Math.Max(32, cacheSize / 4),
            StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent) => IsIgnoredInternal(fileInfo, parent);

    /// <summary>
    /// Clears the directory lookup cache. The parsed rules cache is not cleared
    /// as it validates file modification time on each access.
    /// </summary>
    public void ClearDirectoryCache()
    {
        _directoryCache.Clear();
    }

    /// <summary>
    /// Checks whether or not the file is ignored.
    /// </summary>
    /// <param name="fileInfo">The file information.</param>
    /// <param name="parent">The parent BaseItem.</param>
    /// <returns>True if the file should be ignored.</returns>
    public bool IsIgnoredInternal(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var searchDirectory = fileInfo.IsDirectory
            ? fileInfo.FullName
            : Path.GetDirectoryName(fileInfo.FullName);

        if (string.IsNullOrEmpty(searchDirectory))
        {
            return false;
        }

        var ignoreFilePath = FindIgnoreFileCached(searchDirectory);
        if (ignoreFilePath is null)
        {
            return false;
        }

        var parsedEntry = GetParsedRules(ignoreFilePath);
        if (parsedEntry is null)
        {
            // File was deleted after we cached the path - clear the directory cache entry and return false
            _directoryCache.TryRemove(searchDirectory, out _);
            return false;
        }

        // Empty file means ignore everything
        if (parsedEntry.IsEmpty)
        {
            return true;
        }

        return parsedEntry.Rules.IsIgnored(GetPathToCheck(fileInfo.FullName, fileInfo.IsDirectory));
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

        // If no valid rules were added, fall back to ignoring everything (like an empty .ignore file)
        if (validRulesAdded == 0)
        {
            return true;
        }

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

    private string? FindIgnoreFileCached(string directory)
    {
        // Check if we have a cached result for this directory
        if (_directoryCache.TryGet(directory, out var cached))
        {
            return cached.IgnoreFilePath;
        }

        // Walk up the directory tree to find .ignore file
        var current = directory;
        var checkedDirs = new System.Collections.Generic.List<string> { directory };

        while (!string.IsNullOrEmpty(current))
        {
            // Check if this intermediate directory is cached
            if (current != directory && _directoryCache.TryGet(current, out var parentCached))
            {
                // Cache the result for all directories we checked
                var entry = new IgnoreFileCacheEntry(parentCached.IgnoreFilePath);
                foreach (var dir in checkedDirs)
                {
                    _directoryCache.AddOrUpdate(dir, entry);
                }

                return parentCached.IgnoreFilePath;
            }

            var ignorePath = Path.Join(current, ".ignore");
            if (File.Exists(ignorePath))
            {
                // Cache for all directories we checked
                var entry = new IgnoreFileCacheEntry(ignorePath);
                foreach (var dir in checkedDirs)
                {
                    _directoryCache.AddOrUpdate(dir, entry);
                }

                return ignorePath;
            }

            var parent = Path.GetDirectoryName(current);
            if (parent == current || string.IsNullOrEmpty(parent))
            {
                break;
            }

            current = parent;
            checkedDirs.Add(current);
        }

        // No .ignore file found - cache null result for all directories
        var nullEntry = new IgnoreFileCacheEntry(null);
        foreach (var dir in checkedDirs)
        {
            _directoryCache.AddOrUpdate(dir, nullEntry);
        }

        return null;
    }

    private ParsedIgnoreCacheEntry? GetParsedRules(string ignoreFilePath)
    {
        FileInfo fileInfo;
        try
        {
            fileInfo = new FileInfo(ignoreFilePath);
            if (!fileInfo.Exists)
            {
                _rulesCache.TryRemove(ignoreFilePath, out _);
                return null;
            }
        }
        catch
        {
            _rulesCache.TryRemove(ignoreFilePath, out _);
            return null;
        }

        var lastModified = fileInfo.LastWriteTimeUtc;
        var fileLength = fileInfo.Length;

        // Check cache
        if (_rulesCache.TryGet(ignoreFilePath, out var cached))
        {
            if (cached.FileLastModified == lastModified && cached.FileLength == fileLength)
            {
                return cached;
            }

            // Stale - need to reparse
            _rulesCache.TryRemove(ignoreFilePath, out _);
        }

        // Parse the file
        var parsedEntry = ParseIgnoreFile(fileInfo, lastModified, fileLength);
        _rulesCache.AddOrUpdate(ignoreFilePath, parsedEntry);
        return parsedEntry;
    }

    private static ParsedIgnoreCacheEntry ParseIgnoreFile(FileInfo ignoreFile, DateTime lastModified, long fileLength)
    {
        if (ignoreFile.LinkTarget is null && fileLength == 0)
        {
            return new ParsedIgnoreCacheEntry
            {
                Rules = new Ignore.Ignore(),
                FileLastModified = lastModified,
                FileLength = fileLength,
                IsEmpty = true
            };
        }

        // Resolve symlinks
        var resolvedFile = FileSystemHelper.ResolveLinkTarget(ignoreFile, returnFinalTarget: true) ?? ignoreFile;
        if (!resolvedFile.Exists)
        {
            return new ParsedIgnoreCacheEntry
            {
                Rules = new Ignore.Ignore(),
                FileLastModified = lastModified,
                FileLength = fileLength,
                IsEmpty = true
            };
        }

        var content = File.ReadAllText(resolvedFile.FullName);
        if (string.IsNullOrWhiteSpace(content))
        {
            return new ParsedIgnoreCacheEntry
            {
                Rules = new Ignore.Ignore(),
                FileLastModified = lastModified,
                FileLength = fileLength,
                IsEmpty = true
            };
        }

        var rules = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ignore = new Ignore.Ignore();
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

        // No valid rules means treat as empty (ignore all)
        return new ParsedIgnoreCacheEntry
        {
            Rules = ignore,
            FileLastModified = lastModified,
            FileLength = fileLength,
            IsEmpty = validRulesAdded == 0
        };
    }

    private static string GetPathToCheck(string path, bool isDirectory)
    {
        // Normalize Windows paths
        var pathToCheck = IsWindows ? path.NormalizePath('/') : path;

        // Add trailing slash for directories to match "folder/"
        if (isDirectory)
        {
            pathToCheck = string.Concat(pathToCheck.AsSpan().TrimEnd('/'), "/");
        }

        return pathToCheck;
    }

    private readonly record struct IgnoreFileCacheEntry(string? IgnoreFilePath);

    private sealed class ParsedIgnoreCacheEntry
    {
        public required Ignore.Ignore Rules { get; init; }

        public required DateTime FileLastModified { get; init; }

        public required long FileLength { get; init; }

        public required bool IsEmpty { get; init; }
    }
}
