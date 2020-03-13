#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Naming.Video
{
    public class FileStack
    {
        public FileStack()
        {
            Files = new List<string>();
        }

        public string Name { get; set; }

        public List<string> Files { get; set; }

        public bool IsDirectoryStack { get; set; }

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
