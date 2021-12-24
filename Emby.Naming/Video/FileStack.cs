using System;
using System.Collections.Generic;
using Jellyfin.Extensions;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Object holding list of files paths with additional information.
    /// </summary>
    public class FileStack
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileStack"/> class.
        /// </summary>
        /// <param name="name">The stack name.</param>
        /// <param name="isDirectory">Whether the stack files are directories.</param>
        /// <param name="files">The stack files.</param>
        public FileStack(string name, bool isDirectory, IReadOnlyList<string> files)
        {
            Name = name;
            IsDirectoryStack = isDirectory;
            Files = files;
        }

        /// <summary>
        /// Gets the name of file stack.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the list of paths in stack.
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Gets a value indicating whether stack is directory stack.
        /// </summary>
        public bool IsDirectoryStack { get; }

        /// <summary>
        /// Helper function to determine if path is in the stack.
        /// </summary>
        /// <param name="file">Path of desired file.</param>
        /// <param name="isDirectory">Requested type of stack.</param>
        /// <returns>True if file is in the stack.</returns>
        public bool ContainsFile(string file, bool isDirectory)
        {
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }

            return IsDirectoryStack == isDirectory && Files.Contains(file, StringComparison.OrdinalIgnoreCase);
        }
    }
}
