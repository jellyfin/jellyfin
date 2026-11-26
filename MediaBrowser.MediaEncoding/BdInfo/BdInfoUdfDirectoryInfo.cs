using System;
using System.IO;
using System.Linq;
using BDInfo.IO;
using DiscUtils;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Provides BDInfo access to a directory in a UDF filesystem.
/// </summary>
public class BdInfoUdfDirectoryInfo : IDirectoryInfo
{
    private readonly DiscDirectoryInfo _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BdInfoUdfDirectoryInfo" /> class.
    /// </summary>
    /// <param name="impl">The UDF directory.</param>
    public BdInfoUdfDirectoryInfo(DiscDirectoryInfo impl)
    {
        ArgumentNullException.ThrowIfNull(impl);
        _impl = impl;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => _impl.Name;

    /// <summary>
    /// Gets the full name, normalized to a POSIX-style virtual path.
    /// </summary>
    public string FullName => NormalizeVirtualPath(_impl.FullName);

    /// <summary>
    /// Gets the parent directory information.
    /// </summary>
    public IDirectoryInfo? Parent => _impl.Parent is null ? null : new BdInfoUdfDirectoryInfo(_impl.Parent);

    private static string NormalizeVirtualPath(string path)
    {
        var trimmed = path.Trim('\\', '/');
        return trimmed.Length == 0 ? "/" : "/" + trimmed.Replace('\\', '/');
    }

    private static bool IsHidden(ReadOnlySpan<char> name) => name.StartsWith('.');

    /// <summary>
    /// Gets the directories.
    /// </summary>
    /// <returns>An array with all directories.</returns>
    public IDirectoryInfo[] GetDirectories()
        => _impl.GetDirectories()
            .Where(d => !IsHidden(d.Name))
            .Select(d => new BdInfoUdfDirectoryInfo(d))
            .ToArray();

    /// <summary>
    /// Gets the files.
    /// </summary>
    /// <returns>All files of the directory.</returns>
    public IFileInfo[] GetFiles()
        => _impl.GetFiles()
            .Where(f => !IsHidden(f.Name))
            .Select(f => new BdInfoUdfFileInfo(f))
            .ToArray();

    /// <summary>
    /// Gets the files matching a pattern.
    /// </summary>
    /// <param name="searchPattern">The search pattern.</param>
    /// <returns>All files of the directory matching the pattern.</returns>
    public IFileInfo[] GetFiles(string searchPattern)
        => GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

    /// <summary>
    /// Gets the files matching a pattern and search options.
    /// </summary>
    /// <param name="searchPattern">The search pattern.</param>
    /// <param name="searchOption">The search option.</param>
    /// <returns>All matching files.</returns>
    public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        => _impl.GetFiles(NormalizeSearchPattern(searchPattern), searchOption)
            .Where(f => !IsHidden(f.Name))
            .Select(f => new BdInfoUdfFileInfo(f))
            .ToArray();

    private static string NormalizeSearchPattern(string searchPattern)
    {
        if (string.IsNullOrEmpty(searchPattern))
        {
            return searchPattern;
        }

        return searchPattern.StartsWith('.') ? "*" + searchPattern : searchPattern;
    }
}
