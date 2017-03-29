using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Interface IFileSystem
    /// </summary>
    public interface IFileSystem
    {
        void AddShortcutHandler(IShortcutHandler handler);

        /// <summary>
        /// Determines whether the specified filename is shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename is shortcut; otherwise, <c>false</c>.</returns>
        bool IsShortcut(string filename);

        /// <summary>
        /// Resolves the shortcut.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        string ResolveShortcut(string filename);

        /// <summary>
        /// Creates the shortcut.
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <param name="target">The target.</param>
        void CreateShortcut(string shortcutPath, string target);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified file or directory path.
        /// </summary>
        /// <param name="path">A path to a file or directory.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks>If the specified path points to a directory, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property will be set to true and all other properties will reflect the properties of the directory.</remarks>
        FileSystemMetadata GetFileSystemInfo(string path);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified file path.
        /// </summary>
        /// <param name="path">A path to a file.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks><para>If the specified path points to a directory, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property and the <see cref="FileSystemMetadata.Exists"/> property will both be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="GetFileSystemInfo"/>.</para></remarks>
        FileSystemMetadata GetFileInfo(string path);

        /// <summary>
        /// Returns a <see cref="FileSystemMetadata"/> object for the specified directory path.
        /// </summary>
        /// <param name="path">A path to a directory.</param>
        /// <returns>A <see cref="FileSystemMetadata"/> object.</returns>
        /// <remarks><para>If the specified path points to a file, the returned <see cref="FileSystemMetadata"/> object's
        /// <see cref="FileSystemMetadata.IsDirectory"/> property will be set to true and the <see cref="FileSystemMetadata.Exists"/> property will be set to false.</para>
        /// <para>For automatic handling of files <b>and</b> directories, use <see cref="GetFileSystemInfo"/>.</para></remarks>
        FileSystemMetadata GetDirectoryInfo(string path);

        /// <summary>
        /// Gets the valid filename.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        string GetValidFilename(string filename);

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCreationTimeUtc(FileSystemMetadata info);

        /// <summary>
        /// Gets the creation time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetCreationTimeUtc(string path);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(FileSystemMetadata info);

        /// <summary>
        /// Gets the last write time UTC.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>DateTime.</returns>
        DateTime GetLastWriteTimeUtc(string path);

        /// <summary>
        /// Gets the file stream.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="access">The access.</param>
        /// <param name="share">The share.</param>
        /// <param name="isAsync">if set to <c>true</c> [is asynchronous].</param>
        /// <returns>FileStream.</returns>
        Stream GetFileStream(string path, FileOpenMode mode, FileAccessMode access, FileShareMode share, bool isAsync = false);

        /// <summary>
        /// Opens the read.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Stream.</returns>
        Stream OpenRead(String path);

        /// <summary>
        /// Swaps the files.
        /// </summary>
        /// <param name="file1">The file1.</param>
        /// <param name="file2">The file2.</param>
        void SwapFiles(string file1, string file2);

        bool AreEqual(string path1, string path2);

        /// <summary>
        /// Determines whether [contains sub path] [the specified parent path].
        /// </summary>
        /// <param name="parentPath">The parent path.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [contains sub path] [the specified parent path]; otherwise, <c>false</c>.</returns>
        bool ContainsSubPath(string parentPath, string path);

        /// <summary>
        /// Determines whether [is root path] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is root path] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsRootPath(string path);

        /// <summary>
        /// Normalizes the path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string NormalizePath(string path);

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>System.String.</returns>
        string GetFileNameWithoutExtension(FileSystemMetadata info);

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string GetFileNameWithoutExtension(string path);

        /// <summary>
        /// Determines whether [is path file] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is path file] [the specified path]; otherwise, <c>false</c>.</returns>
        bool IsPathFile(string path);

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        void DeleteFile(string path);

        /// <summary>
        /// Deletes the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        void DeleteDirectory(string path, bool recursive);

        /// <summary>
        /// Gets the directories.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;DirectoryInfo&gt;.</returns>
        IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false);

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;FileInfo&gt;.</returns>
        IEnumerable<FileSystemMetadata> GetFiles(string path, bool recursive = false);

        /// <summary>
        /// Gets the file system entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;FileSystemMetadata&gt;.</returns>
        IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false);

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        void CreateDirectory(string path);

        /// <summary>
        /// Copies the file.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="overwrite">if set to <c>true</c> [overwrite].</param>
        void CopyFile(string source, string target, bool overwrite);

        /// <summary>
        /// Moves the file.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        void MoveFile(string source, string target);

        /// <summary>
        /// Moves the directory.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        void MoveDirectory(string source, string target);

        /// <summary>
        /// Directories the exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Files the exists.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Reads all text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string ReadAllText(string path);

        byte[] ReadAllBytes(string path);

        void WriteAllBytes(string path, byte[] bytes);

        /// <summary>
        /// Writes all text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="text">The text.</param>
        void WriteAllText(string path, string text);

        /// <summary>
        /// Writes all text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="text">The text.</param>
        /// <param name="encoding">The encoding.</param>
        void WriteAllText(string path, string text, Encoding encoding);

        /// <summary>
        /// Reads all text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns>System.String.</returns>
        string ReadAllText(string path, Encoding encoding);

        string[] ReadAllLines(string path);

        void WriteAllLines(string path, IEnumerable<string> lines);

        /// <summary>
        /// Gets the directory paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false);

        /// <summary>
        /// Gets the file paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetFilePaths(string path, bool recursive = false);

        /// <summary>
        /// Gets the file system entry paths.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="recursive">if set to <c>true</c> [recursive].</param>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false);

        void SetHidden(string path, bool isHidden);
        void SetReadOnly(string path, bool isHidden);

        char DirectorySeparatorChar { get; }
        char PathSeparator { get; }

        string GetFullPath(string path);

        List<FileSystemMetadata> GetDrives();

        void SetExecutable(string path);
    }

    public enum FileOpenMode
    {
        //
        // Summary:
        //     Specifies that the operating system should create a new file. This requires System.Security.Permissions.FileIOPermissionAccess.Write
        //     permission. If the file already exists, an System.IO.IOException exception is
        //     thrown.
        CreateNew = 1,
        //
        // Summary:
        //     Specifies that the operating system should create a new file. If the file already
        //     exists, it will be overwritten. This requires System.Security.Permissions.FileIOPermissionAccess.Write
        //     permission. FileMode.Create is equivalent to requesting that if the file does
        //     not exist, use System.IO.FileMode.CreateNew; otherwise, use System.IO.FileMode.Truncate.
        //     If the file already exists but is a hidden file, an System.UnauthorizedAccessException
        //     exception is thrown.
        Create = 2,
        //
        // Summary:
        //     Specifies that the operating system should open an existing file. The ability
        //     to open the file is dependent on the value specified by the System.IO.FileAccess
        //     enumeration. A System.IO.FileNotFoundException exception is thrown if the file
        //     does not exist.
        Open = 3,
        //
        // Summary:
        //     Specifies that the operating system should open a file if it exists; otherwise,
        //     a new file should be created. If the file is opened with FileAccess.Read, System.Security.Permissions.FileIOPermissionAccess.Read
        //     permission is required. If the file access is FileAccess.Write, System.Security.Permissions.FileIOPermissionAccess.Write
        //     permission is required. If the file is opened with FileAccess.ReadWrite, both
        //     System.Security.Permissions.FileIOPermissionAccess.Read and System.Security.Permissions.FileIOPermissionAccess.Write
        //     permissions are required.
        OpenOrCreate = 4,
        //
        // Summary:
        //     Specifies that the operating system should open an existing file. When the file
        //     is opened, it should be truncated so that its size is zero bytes. This requires
        //     System.Security.Permissions.FileIOPermissionAccess.Write permission. Attempts
        //     to read from a file opened with FileMode.Truncate cause an System.ArgumentException
        //     exception.
        Truncate = 5,
        //
        // Summary:
        //     Opens the file if it exists and seeks to the end of the file, or creates a new
        //     file. This requires System.Security.Permissions.FileIOPermissionAccess.Append
        //     permission. FileMode.Append can be used only in conjunction with FileAccess.Write.
        //     Trying to seek to a position before the end of the file throws an System.IO.IOException
        //     exception, and any attempt to read fails and throws a System.NotSupportedException
        //     exception.
        Append = 6
    }

    public enum FileAccessMode
    {
        //
        // Summary:
        //     Read access to the file. Data can be read from the file. Combine with Write for
        //     read/write access.
        Read = 1,
        //
        // Summary:
        //     Write access to the file. Data can be written to the file. Combine with Read
        //     for read/write access.
        Write = 2,
        //
        // Summary:
        //     Read and write access to the file. Data can be written to and read from the file.
        ReadWrite = 3
    }

    public enum FileShareMode
    {
        //
        // Summary:
        //     Declines sharing of the current file. Any request to open the file (by this process
        //     or another process) will fail until the file is closed.
        None = 0,
        //
        // Summary:
        //     Allows subsequent opening of the file for reading. If this flag is not specified,
        //     any request to open the file for reading (by this process or another process)
        //     will fail until the file is closed. However, even if this flag is specified,
        //     additional permissions might still be needed to access the file.
        Read = 1,
        //
        // Summary:
        //     Allows subsequent opening of the file for writing. If this flag is not specified,
        //     any request to open the file for writing (by this process or another process)
        //     will fail until the file is closed. However, even if this flag is specified,
        //     additional permissions might still be needed to access the file.
        Write = 2,
        //
        // Summary:
        //     Allows subsequent opening of the file for reading or writing. If this flag is
        //     not specified, any request to open the file for reading or writing (by this process
        //     or another process) will fail until the file is closed. However, even if this
        //     flag is specified, additional permissions might still be needed to access the
        //     file.
        ReadWrite = 3
    }

}
