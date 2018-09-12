using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Naming.Video
{
    public class FileStack
    {
        public string Name { get; set; }
        public List<string> Files { get; set; }
        public bool IsDirectoryStack { get; set; }

        public FileStack()
        {
            Files = new List<string>();
        }

        public bool ContainsFile(string file, bool IsDirectory)
        {
            if (IsDirectoryStack == IsDirectory)
            {
                return Files.Contains(file, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
