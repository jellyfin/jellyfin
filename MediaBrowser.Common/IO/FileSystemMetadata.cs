using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.IO
{
    public class FileSystemMetadata
    {
        public FileAttributes Attributes { get; set; }

        public bool Exists { get; set; }
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long Length { get; set; }
        public string DirectoryName { get; set; }

        public DateTime LastWriteTimeUtc { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        
        public bool IsDirectory
        {
            get
            {
                return (Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            }
        }
    }
}
