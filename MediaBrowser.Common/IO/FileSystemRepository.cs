using MediaBrowser.Common.Extensions;
using System;
using System.IO;

namespace MediaBrowser.Common.IO
{
    /// <summary>
    /// This is a wrapper for storing large numbers of files within a directory on a file system.
    /// Simply pass a filename into GetResourcePath and it will return a full path location of where the file should be stored.
    /// </summary>
    public class FileSystemRepository
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        protected string Path { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemRepository" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public FileSystemRepository(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }

            Path = path;
        }

        /// <summary>
        /// Gets the full path of where a resource should be stored within the repository
        /// </summary>
        /// <param name="uniqueName">Name of the unique.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public string GetResourcePath(string uniqueName, string fileExtension)
        {
            if (string.IsNullOrEmpty(uniqueName))
            {
                throw new ArgumentNullException("uniqueName");
            }

            if (string.IsNullOrEmpty(fileExtension))
            {
                throw new ArgumentNullException("fileExtension");
            }
            
            var filename = uniqueName.GetMD5() + fileExtension;

            return GetResourcePath(filename);
        }

        /// <summary>
        /// Gets the resource path.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public string GetResourcePath(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            
            var prefix = filename.Substring(0, 1);

            var path = System.IO.Path.Combine(Path, prefix);
            
            return System.IO.Path.Combine(path, filename);
        }

        /// <summary>
        /// Determines if a resource is present in the repository
        /// </summary>
        /// <param name="uniqueName">Name of the unique.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns><c>true</c> if the specified unique name contains resource; otherwise, <c>false</c>.</returns>
        public bool ContainsResource(string uniqueName, string fileExtension)
        {
            return ContainsFilePath(GetResourcePath(uniqueName, fileExtension));
        }

        /// <summary>
        /// Determines if a file with a given name is present in the repository
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns><c>true</c> if the specified filename contains filename; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool ContainsFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException();
            }

            return ContainsFilePath(GetResourcePath(filename));
        }

        /// <summary>
        /// Determines if a file is present in the repository
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [contains file path] [the specified path]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool ContainsFilePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            
            return File.Exists(path);
        }
    }
}
