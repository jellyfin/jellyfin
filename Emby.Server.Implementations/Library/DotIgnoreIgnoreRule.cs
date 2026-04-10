using System;
using System.IO;
using System.Text.RegularExpressions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Resolver rule class for ignoring files via .jellyignore and .jellyforceignore.
/// </summary>
public class DotIgnoreIgnoreRule : IResolverIgnoreRule
{
    /// <summary>
    /// Filename for user-configurable ignore rules (can be disabled via ServerConfiguration).
    /// </summary>
    private const string JellyignoreFilename = ".jellyignore";

    /// <summary>
    /// Filename for hard-block rules (always applied, cannot be disabled).
    /// </summary>
    private const string JellyforceIgnoreFilename = ".jellyforceignore";

    private static readonly bool IsWindows = OperatingSystem.IsWindows();

    private readonly IServerConfigurationManager? _configurationManager;
    private readonly ILogger? _instanceLogger;

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
    /// <param name="logger">The logger.</param>
    public DotIgnoreIgnoreRule(IServerConfigurationManager configurationManager, ILogger? logger = null)
    {
        _configurationManager = configurationManager;
        _instanceLogger = logger;
    }

    /// <summary>
    /// Finds an ignore file walking up the directory tree.
    /// </summary>
    /// <param name="directory">Starting directory.</param>
    /// <param name="filename">Name of the ignore file to look for.</param>
    /// <returns>FileInfo if found, otherwise null.</returns>
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
    /// Instance method to check if a file is ignored by .jellyforceignore or .jellyignore.
    /// .jellyforceignore is always checked and takes precedence.
    /// This method respects the ServerConfiguration.EnableJellyignore setting.
    /// </summary>
    /// <param name="fileInfo">The file information.</param>
    /// <param name="parent">The parent BaseItem.</param>
    /// <returns>True if the file should be ignored.</returns>
    public bool IsIgnoredInstance(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var searchDirectory = fileInfo.IsDirectory
            ? new DirectoryInfo(fileInfo.FullName)
            : new DirectoryInfo(Path.GetDirectoryName(fileInfo.FullName) ?? string.Empty);

        if (string.IsNullOrEmpty(searchDirectory.FullName))
        {
            return false;
        }

        // Check .jellyforceignore first (hard block, always applied)
        if (IsIgnoredByFile(searchDirectory, fileInfo, JellyforceIgnoreFilename))
        {
            return true;
        }

        // Check .jellyignore (user-configurable, respects server settings)
        bool enableJellyignore = _configurationManager?.Configuration.EnableJellyignore ?? true;
        if (enableJellyignore && IsIgnoredByFile(searchDirectory, fileInfo, JellyignoreFilename))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Static method to check if a file is ignored (for backward compatibility).
    /// Always enables .jellyignore and .jellyforceignore (ignores EnableJellyignore setting).
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
    /// Instance method to check if a file is ignored by .jellyforceignore or .jellyignore.
    /// .jellyforceignore is always checked and takes precedence.
    /// This method respects the ServerConfiguration.EnableJellyignore setting.
    /// </summary>
    /// <param name="fileInfo">The file information.</param>
    /// <param name="parent">The parent BaseItem.</param>
    /// <returns>True if the file should be ignored.</returns>
    bool IResolverIgnoreRule.ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent) => IsIgnoredInstance(fileInfo, parent);

    /// <summary>
    /// Checks if a file is ignored by a specific ignore file.
    /// </summary>
    private static bool IsIgnoredByFile(DirectoryInfo searchDirectory, FileSystemMetadata fileInfo, string ignoreFilename)
    {
        var ignoreFile = FindIgnoreFile(searchDirectory, ignoreFilename);
        if (ignoreFile is null)
        {
            return false;
        }

        // Get file content (empty file = ignore nothing, not everything - FIX for the bug!)
        var content = GetFileContent(ignoreFile);
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        return CheckIgnoreRules(fileInfo.FullName, content, fileInfo.IsDirectory);
    }

    private static bool CheckIgnoreRules(string path, string ignoreFileContent, bool isDirectory)
    {
        // If file has content, base ignoring off the content .gitignore-style rules
        var rules = ignoreFileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return CheckIgnoreRules(path, rules, isDirectory);
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
    /// Invalid patterns are skipped with a warning log.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="rules">The array of ignore rules.</param>
    /// <param name="isDirectory">Whether the path is a directory.</param>
    /// <param name="normalizePath">Whether to normalize backslashes to forward slashes (for Windows paths).</param>
    /// <returns>True if the path should be ignored.</returns>
    internal static bool CheckIgnoreRules(string path, string[] rules, bool isDirectory, bool normalizePath)
    {
        if (rules.Length == 0)
        {
            return false;
        }

        var ignore = new Ignore.Ignore();
        var validRulesAdded = 0;
        var invalidRuleCount = 0;

        // Add each rule individually to catch and skip invalid patterns
        foreach (var rule in rules)
        {
            // Skip comments and empty lines
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
                // Log warning about invalid pattern
                invalidRuleCount++;
                // Note: In static context, we can't log. Consider injecting logger if this becomes critical.
            }
        }

        // If no valid rules were added, don't ignore anything (FIX: was incorrectly ignoring everything)
        if (validRulesAdded == 0)
        {
            return false;
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
            // Silently return empty string on IO errors
            return string.Empty;
        }
    }
}
