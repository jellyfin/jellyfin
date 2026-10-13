using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private static readonly StringComparison PathComparison = IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly FastConcurrentLru<string, IgnoreFileCacheEntry> _directoryCache;
    private readonly FastConcurrentLru<string, ParsedIgnoreCacheEntry> _rulesCache;

    private Func<IEnumerable<string>>? _libraryRootsProvider;
    private volatile string[]? _libraryRoots;

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
    /// Sets the provider of library root paths. The search for a .ignore file stops at the
    /// outermost library root containing the path instead of walking up to the filesystem root.
    /// </summary>
    /// <param name="libraryRootsProvider">The library root paths provider.</param>
    public void SetLibraryRootsProvider(Func<IEnumerable<string>> libraryRootsProvider)
    {
        _libraryRootsProvider = libraryRootsProvider;
        ClearDirectoryCache();
    }

    /// <summary>
    /// Clears the directory lookup cache and the library roots. The parsed rules cache is not cleared
    /// as it validates file modification time on each access.
    /// </summary>
    public void ClearDirectoryCache()
    {
        _libraryRoots = null;
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

        var ignoreFile = FindIgnoreFileCached(searchDirectory);
        if (ignoreFile is null)
        {
            return false;
        }

        var parsedEntry = GetParsedRules(ignoreFile);
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

    private FileInfo? FindIgnoreFileCached(string directory)
    {
        // Check if we have a cached result for this directory
        if (_directoryCache.TryGet(directory, out var cached))
        {
            return cached.IgnoreFileDirectory is null
                ? null
                : new FileInfo(Path.Join(cached.IgnoreFileDirectory, ".ignore"));
        }

        DirectoryInfo startDir;
        try
        {
            startDir = new DirectoryInfo(directory);
        }
        catch (ArgumentException)
        {
            return null;
        }

        // Walk up the directory tree to find .ignore file using DirectoryInfo.Parent,
        // never leaving the library: probing above it can hit autofs or network mounts (#17364)
        var stopDirectory = GetOutermostLibraryRoot(startDir.FullName);
        var checkedDirs = new List<string> { directory };

        for (var current = startDir; current is not null; current = current.Parent)
        {
            var currentPath = current.FullName;

            // Check if this intermediate directory is cached
            if (current != startDir && _directoryCache.TryGet(currentPath, out var parentCached))
            {
                // Cache the result for all directories we checked
                var entry = new IgnoreFileCacheEntry(parentCached.IgnoreFileDirectory);
                foreach (var dir in checkedDirs)
                {
                    _directoryCache.AddOrUpdate(dir, entry);
                }

                return parentCached.IgnoreFileDirectory is null
                    ? null
                    : new FileInfo(Path.Join(parentCached.IgnoreFileDirectory, ".ignore"));
            }

            var ignoreFile = new FileInfo(Path.Join(currentPath, ".ignore"));
            if (ignoreFile.Exists)
            {
                // Cache for all directories we checked
                var entry = new IgnoreFileCacheEntry(currentPath);
                foreach (var dir in checkedDirs)
                {
                    _directoryCache.AddOrUpdate(dir, entry);
                }

                return ignoreFile;
            }

            if (current != startDir)
            {
                checkedDirs.Add(currentPath);
            }

            if (stopDirectory is not null && IsSamePath(currentPath, stopDirectory))
            {
                break;
            }
        }

        // No .ignore file found - cache null result for all directories
        var nullEntry = new IgnoreFileCacheEntry((string?)null);
        foreach (var dir in checkedDirs)
        {
            _directoryCache.AddOrUpdate(dir, nullEntry);
        }

        return null;
    }

    private string? GetOutermostLibraryRoot(string path)
    {
        string? outermost = null;
        foreach (var root in GetLibraryRoots())
        {
            if ((outermost is null || root.Length < outermost.Length)
                && IsSameOrChildPath(path, root))
            {
                outermost = root;
            }
        }

        return outermost;
    }

    private string[] GetLibraryRoots()
    {
        var roots = _libraryRoots;
        if (roots is not null)
        {
            return roots;
        }

        var provider = _libraryRootsProvider;
        if (provider is null)
        {
            return [];
        }

        roots = provider()
            .Where(r => !string.IsNullOrEmpty(r))
            .Select(r => Path.TrimEndingDirectorySeparator(Path.GetFullPath(r)))
            .ToArray();
        _libraryRoots = roots;
        return roots;
    }

    private static bool IsSameOrChildPath(string path, string root)
    {
        if (IsSamePath(path, root))
        {
            return true;
        }

        // A filesystem root such as "/" keeps its trailing separator after trimming
        return path.StartsWith(root, PathComparison)
            && (Path.EndsInDirectorySeparator(root) || path[root.Length] == Path.DirectorySeparatorChar);
    }

    private static bool IsSamePath(string path1, string path2)
        => Path.TrimEndingDirectorySeparator(path1.AsSpan()).Equals(Path.TrimEndingDirectorySeparator(path2.AsSpan()), PathComparison);

    private ParsedIgnoreCacheEntry? GetParsedRules(FileInfo ignoreFile)
    {
        if (!ignoreFile.Exists)
        {
            _rulesCache.TryRemove(ignoreFile.FullName, out _);
            return null;
        }

        var lastModified = ignoreFile.LastWriteTimeUtc;
        var fileLength = ignoreFile.Length;
        var key = ignoreFile.FullName;

        // Check cache
        if (_rulesCache.TryGet(key, out var cached))
        {
            if (cached.FileLastModified == lastModified && cached.FileLength == fileLength)
            {
                return cached;
            }

            // Stale - need to reparse
            _rulesCache.TryRemove(key, out _);
        }

        // Parse the file
        var parsedEntry = ParseIgnoreFile(ignoreFile, lastModified, fileLength);
        _rulesCache.AddOrUpdate(key, parsedEntry);
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

    private readonly record struct IgnoreFileCacheEntry(string? IgnoreFileDirectory);

    private sealed class ParsedIgnoreCacheEntry
    {
        public required Ignore.Ignore Rules { get; init; }

        public required DateTime FileLastModified { get; init; }

        public required long FileLength { get; init; }

        public required bool IsEmpty { get; init; }
    }
}
