using System;
using System.IO;
using BDInfo.IO;
using DiscUtils;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Provides BDInfo access to a file in a UDF filesystem.
/// </summary>
public class BdInfoUdfFileInfo : IFileInfo
{
    private readonly DiscFileInfo _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BdInfoUdfFileInfo" /> class.
    /// </summary>
    /// <param name="impl">The UDF file.</param>
    public BdInfoUdfFileInfo(DiscFileInfo impl)
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
    /// Gets the extension.
    /// </summary>
    public string Extension => _impl.Extension;

    /// <summary>
    /// Gets the length.
    /// </summary>
    public long Length => _impl.Length;

    /// <summary>
    /// Gets a value indicating whether this is a directory.
    /// </summary>
    public bool IsDir => false;

    /// <summary>
    /// Gets a file as a read-only stream.
    /// </summary>
    /// <returns>A <see cref="Stream" /> for the file.</returns>
    public Stream OpenRead() => _impl.OpenRead();

    /// <summary>
    /// Gets a file's content with a stream reader.
    /// </summary>
    /// <returns>A <see cref="StreamReader" /> for the file.</returns>
    public StreamReader OpenText() => _impl.OpenText();

    private static string NormalizeVirtualPath(string path)
    {
        var trimmed = path.Trim('\\', '/');
        return trimmed.Length == 0 ? "/" : "/" + trimmed.Replace('\\', '/');
    }
}
