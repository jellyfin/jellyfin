using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Common.IO
{
    /// <summary>
    /// Interface IFileSystem
    /// </summary>
    public interface IFileSystem
    {
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
        /// Gets the file system info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemInfo.</returns>
        FileSystemMetadata GetFileSystemInfo(string path);

        /// <summary>
        /// Gets the file information.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemMetadata.</returns>
        FileSystemMetadata GetFileInfo(string path);

        /// <summary>
        /// Gets the directory information.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileSystemMetadata.</returns>
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
        Stream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share, bool isAsync = false);

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
    }
}
