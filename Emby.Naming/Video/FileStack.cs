using System;
using System.Collections.Generic;
using System.Linq;

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
        public FileStack()
        {
            Files = new List<string>();
        }

        /// <summary>
        /// Gets or sets name of file stack.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets list of paths in stack.
        /// </summary>
        public List<string> Files { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether stack is directory stack.
        /// </summary>
        public bool IsDirectoryStack { get; set; }

        /// <summary>
        /// Helper function to determine if path is in the stack.
        /// </summary>
        /// <param name="file">Path of desired file.</param>
        /// <param name="isDirectory">Requested type of stack.</param>
        /// <returns>True if file is in the stack.</returns>
        public bool ContainsFile(string file, bool isDirectory)
        {
            if (IsDirectoryStack == isDirectory)
            {
                return Files.Contains(file, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
