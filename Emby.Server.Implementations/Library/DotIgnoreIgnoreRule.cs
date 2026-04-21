using System;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Resolver rule class for ignoring files via .ignore and .jellyignore files.
/// .ignore files are always applied (forced ignores).
/// .jellyignore files are optional and controlled by the EnableJellyignore setting.
/// Empty ignore file ignores the entire directory.
/// </summary>
public class DotIgnoreIgnoreRule : IResolverIgnoreRule
{
    private const string ForceIgnoreFilename = ".ignore";
    private const string JellyignoreFilename = ".jellyignore";

    private static readonly bool IsWindows = OperatingSystem.IsWindows();

    private readonly IServerConfigurationManager? _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotIgnoreIgnoreRule"/> class.
    /// </summary>
    public DotIgnoreIgnoreRule()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotIgnoreIgnoreRule"/> class with dependencies.
    /// </summary>
    /// <param name="configurationManager">The server configuration manager.</param>
    public DotIgnoreIgnoreRule(IServerConfigurationManager configurationManager)
    {
        _configurationManager = configurationManager;
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent) => IsIgnored(fileInfo, parent);

    /// <summary>
    /// Static method to check if a file is ignored.
    /// </summary>
    /// <param name="fileInfo">The file information to check.</param>
    /// <param name="parent">The parent BaseItem context.</param>
    /// <returns>True if the file should be ignored.</returns>
    public static bool IsIgnored(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var rule = new DotIgnoreIgnoreRule();
        return rule.IsIgnoredInstance(fileInfo, parent);
    }

    /// <summary>
    /// Instance method to check if a file is ignored by .ignore or .jellyignore files.
    /// .ignore files are always applied (forced ignores).
    /// .jellyignore files are optionally applied based on EnableJellyignore setting.
    /// </summary>
    private bool IsIgnoredInstance(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var searchDirectory = fileInfo.IsDirectory
            ? new DirectoryInfo(fileInfo.FullName)
            : new DirectoryInfo(Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty);

        if (string.IsNullOrEmpty(searchDirectory.FullName))
        {
            return false;
        }

        // Always check .ignore files (forced ignores)
        if (IsIgnoredByFile(searchDirectory, fileInfo, ForceIgnoreFilename))
        {
            return true;
        }

        // Check .jellyignore files (optional ignores)
        // Get the per-library EnableJellyignore setting if available, otherwise use global setting
        bool enableJellyignore = GetEnableJellyignoreSetting(parent);
        if (enableJellyignore && IsIgnoredByFile(searchDirectory, fileInfo, JellyignoreFilename))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the EnableJellyignore setting from the parent library if available, otherwise uses global setting.
    /// </summary>
    private bool GetEnableJellyignoreSetting(BaseItem? parent)
    {
        // Try to get per-library setting from parent
        if (parent is not null)
        {
            var collectionFolder = GetCollectionFolderParent(parent);
            if (collectionFolder is not null)
            {
                try
                {
                    var libraryOptions = collectionFolder.GetLibraryOptions();
                    return libraryOptions.EnableJellyignore;
                }
                catch
                {
                    // If we can't get library options, fall back to global setting
                }
            }
        }

        // Fall back to global setting
        return _configurationManager?.Configuration.EnableJellyignore ?? true;
    }

    /// <summary>
    /// Gets the collection folder parent of the given item.
    /// </summary>
    private static CollectionFolder? GetCollectionFolderParent(BaseItem item)
    {
        var current = item;
        while (current is not null)
        {
            if (current is CollectionFolder collectionFolder)
            {
                return collectionFolder;
            }

            current = current.GetParent();
        }

        return null;
    }

    private static FileInfo? FindIgnoreFile(DirectoryInfo directory, string filename)
    {
        for (var current = directory; current is not null; current = current.Parent)
        {
            var ignorePath = Path.Join(current.FullName, filename);
            if (File.Exists(ignorePath))
            {
                return new FileInfo(ignorePath);
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a file is ignored by the specified ignore file.
    /// Empty ignore file = ignore entire directory.
    /// </summary>
    private static bool IsIgnoredByFile(DirectoryInfo searchDirectory, FileSystemMetadata fileInfo, string ignoreFilename)
    {
        var ignoreFile = FindIgnoreFile(searchDirectory, ignoreFilename);
        if (ignoreFile is null)
        {
            return false;
        }

        var content = GetFileContent(ignoreFile);

        // Empty file = ignore everything in this directory
        if (string.IsNullOrWhiteSpace(content))
        {
            return true;
        }

        return CheckIgnoreRules(fileInfo.FullName, content, fileInfo.IsDirectory);
    }

    private static bool CheckIgnoreRules(string path, string ignoreFileContent, bool isDirectory)
    {
        var rules = ignoreFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return CheckIgnoreRules(path, rules, isDirectory);
    }

    internal static bool CheckIgnoreRules(string path, string[] rules, bool isDirectory)
        => CheckIgnoreRules(path, rules, isDirectory, IsWindows);

    internal static bool CheckIgnoreRules(string path, string[] rules, bool isDirectory, bool normalizePath)
    {
        if (rules.Length == 0)
        {
            return false;
        }

        var ignore = new Ignore.Ignore();
        var validRulesAdded = 0;

        foreach (var rule in rules)
        {
            var trimmedRule = rule.Trim();
            if (trimmedRule.StartsWith('#') || string.IsNullOrEmpty(trimmedRule))
            {
                continue;
            }

            try
            {
                ignore.Add(rule);
                validRulesAdded++;
            }
            catch (RegexParseException)
            {
                // Skip invalid patterns
            }
        }

        // All invalid patterns = ignore everything
        if (validRulesAdded == 0)
        {
            return true;
        }

        var pathToCheck = normalizePath ? path.NormalizePath('/') : path;

        if (isDirectory)
        {
            pathToCheck = string.Concat(pathToCheck.AsSpan().TrimEnd('/'), "/");
        }

        return ignore.IsIgnored(pathToCheck);
    }

    private static string GetFileContent(FileInfo ignoreFile)
    {
        try
        {
            ignoreFile = FileSystemHelper.ResolveLinkTarget(ignoreFile, returnFinalTarget: true) ?? ignoreFile;
            return ignoreFile.Exists
                ? File.ReadAllText(ignoreFile.FullName)
                : string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
