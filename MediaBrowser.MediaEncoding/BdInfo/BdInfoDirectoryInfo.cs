using System.IO;
using System.Linq;
using BDInfo.IO;
using MediaBrowser.Model.IO;

namespace MediaBrowser.MediaEncoding.BdInfo;

/// <summary>
/// Class BdInfoDirectoryInfo.
/// </summary>
public class BdInfoDirectoryInfo : IDirectoryInfo
{
    private readonly IFileSystem _fileSystem;

    private readonly FileSystemMetadata _impl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BdInfoDirectoryInfo" /> class.
    /// </summary>
    /// <param name="fileSystem">The filesystem.</param>
    /// <param name="path">The path.</param>
    public BdInfoDirectoryInfo(IFileSystem fileSystem, string path)
    {
        _fileSystem = fileSystem;
        _impl = _fileSystem.GetDirectoryInfo(path);
    }

    private BdInfoDirectoryInfo(IFileSystem fileSystem, FileSystemMetadata impl)
    {
        _fileSystem = fileSystem;
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
    /// Gets the parent directory information.
    /// </summary>
    public IDirectoryInfo? Parent
    {
        get
        {
            var parentFolder = Path.GetDirectoryName(_impl.FullName);
            if (parentFolder is not null)
            {
                return new BdInfoDirectoryInfo(_fileSystem, parentFolder);
            }

            return null;
        }
    }

    /// <summary>
    /// Gets the directories.
    /// </summary>
    /// <returns>An array with all directories.</returns>
    public IDirectoryInfo[] GetDirectories()
    {
        return _fileSystem.GetDirectories(_impl.FullName)
            .Select(x => new BdInfoDirectoryInfo(_fileSystem, x))
            .ToArray();
    }

    /// <summary>
    /// Gets the files.
    /// </summary>
    /// <returns>All files of the directory.</returns>
    public IFileInfo[] GetFiles()
    {
        return _fileSystem.GetFiles(_impl.FullName)
            .Select(x => new BdInfoFileInfo(x))
            .ToArray();
    }

    /// <summary>
    /// Gets the files matching a pattern.
    /// </summary>
    /// <param name="searchPattern">The search pattern.</param>
    /// <returns>All files of the directory matching the search pattern.</returns>
    public IFileInfo[] GetFiles(string searchPattern)
    {
        return _fileSystem.GetFiles(_impl.FullName, new[] { searchPattern }, false, false)
            .Select(x => new BdInfoFileInfo(x))
            .ToArray();
    }

    /// <summary>
    /// Gets the files matching a pattern and search options.
    /// </summary>
    /// <param name="searchPattern">The search pattern.</param>
    /// <param name="searchOption">The search option.</param>
    /// <returns>All files of the directory matching the search pattern and options.</returns>
    public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
    {
        return _fileSystem.GetFiles(
                _impl.FullName,
                new[] { searchPattern },
                false,
                searchOption == SearchOption.AllDirectories)
            .Select(x => new BdInfoFileInfo(x))
            .ToArray();
    }

    /// <summary>
    /// Gets the bdinfo of a file system path.
    /// </summary>
    /// <param name="fs">The file system.</param>
    /// <param name="path">The path.</param>
    /// <returns>The BD directory information of the path on the file system.</returns>
    public static IDirectoryInfo FromFileSystemPath(IFileSystem fs, string path)
    {
        return new BdInfoDirectoryInfo(fs, path);
    }
}
