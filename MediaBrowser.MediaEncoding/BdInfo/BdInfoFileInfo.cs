using System.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Class BdInfoFileInfo.
/// </summary>
public class BdInfoFileInfo : BDInfo.IO.IFileInfo
{
    private readonly FileSystemMetadata _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BdInfoFileInfo" /> class.
    /// </summary>
    /// <param name="impl">The <see cref="FileSystemMetadata" />.</param>
    public BdInfoFileInfo(FileSystemMetadata impl)
    {
        _impl = impl;
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    public string Name => _impl.Name;

    /// <summary>
    /// Gets the full name.
    /// </summary>
    public string FullName => _impl.FullName;

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
    public bool IsDir => _impl.IsDirectory;

    /// <summary>
    /// Gets a file as file stream.
    /// </summary>
    /// <returns>A <see cref="FileStream" /> for the file.</returns>
    public Stream OpenRead()
    {
        return new FileStream(
            FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
    }

    /// <summary>
    /// Gets a files's content with a stream reader.
    /// </summary>
    /// <returns>A <see cref="StreamReader" /> for the file's content.</returns>
    public StreamReader OpenText()
    {
        return new StreamReader(OpenRead());
    }
}
