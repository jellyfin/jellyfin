using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCifs.Smb;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;

namespace Emby.Common.Implementations.IO
{
    public class SharpCifsFileSystem
    {
        private readonly MediaBrowser.Model.System.OperatingSystem _operatingSystem;

        public SharpCifsFileSystem(MediaBrowser.Model.System.OperatingSystem operatingSystem)
        {
            _operatingSystem = operatingSystem;
        }

        public bool IsEnabledForPath(string path)
        {
            if (_operatingSystem == MediaBrowser.Model.System.OperatingSystem.Windows)
            {
                return false;
            }

            return path.StartsWith("smb://", StringComparison.OrdinalIgnoreCase) || IsUncPath(path);
        }

        public string NormalizePath(string path)
        {
            if (path.StartsWith("smb://", StringComparison.OrdinalIgnoreCase))
            {
                return path;
            }

            if (IsUncPath(path))
            {
                return ConvertUncToSmb(path);
            }

            return path;
        }

        public string GetDirectoryName(string path)
        {
            var separator = GetDirectorySeparatorChar(path);
            var result = Path.GetDirectoryName(path);

            if (separator == '/')
            {
                result = result.Replace('\\', '/');
            }

            return result;
        }

        public char GetDirectorySeparatorChar(string path)
        {
            if (path.IndexOf('/') != -1)
            {
                return '/';
            }

            return '\\';
        }

        public FileSystemMetadata GetFileSystemInfo(string path)
        {
            var file = CreateSmbFile(path);
            return ToMetadata(file);
        }

        public FileSystemMetadata GetFileInfo(string path)
        {
            var file = CreateSmbFile(path);
            return ToMetadata(file, false);
        }

        public FileSystemMetadata GetDirectoryInfo(string path)
        {
            var file = CreateSmbFile(path);
            return ToMetadata(file, true);
        }

        private bool IsUncPath(string path)
        {
            return path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase);
        }

        private string GetReturnPath(SmbFile file)
        {
            return file.GetCanonicalPath().TrimEnd('/');
            //return file.GetPath();
        }

        private string ConvertUncToSmb(string path)
        {
            if (IsUncPath(path))
            {
                path = path.Replace('\\', '/');
                path = "smb:" + path;
            }
            return path;
        }

        private string AddAuthentication(string path)
        {
            return path;
        }

        private SmbFile CreateSmbFile(string path)
        {
            path = ConvertUncToSmb(path);
            path = AddAuthentication(path);

            return new SmbFile(path);
        }

        DateTime baseDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private FileSystemMetadata ToMetadata(SmbFile info, bool? isDirectory = null)
        {
            var result = new FileSystemMetadata();

            result.Exists = info.Exists();
            result.FullName = GetReturnPath(info);
            result.Extension = Path.GetExtension(result.FullName);
            result.Name = info.GetName();

            if (result.Exists)
            {
                result.IsDirectory = info.IsDirectory();
                result.IsHidden = info.IsHidden();

                result.IsReadOnly = !info.CanWrite();

                if (info.IsFile())
                {
                    result.Length = info.Length();
                    result.DirectoryName = info.GetParent();
                }

                result.CreationTimeUtc = baseDate.AddMilliseconds(info.CreateTime());
                result.LastWriteTimeUtc = baseDate.AddMilliseconds(info.GetLastModified());
            }
            else
            {
                if (isDirectory.HasValue)
                {
                    result.IsDirectory = isDirectory.Value;
                }
            }

            return result;
        }

        public void SetHidden(string path, bool isHidden)
        {
            var file = CreateSmbFile(path);

            var isCurrentlyHidden = file.IsHidden();

            if (isCurrentlyHidden && !isHidden)
            {
                file.SetAttributes(file.GetAttributes() & ~SmbFile.AttrReadonly);
            }
            else if (!isCurrentlyHidden && isHidden)
            {
                file.SetAttributes(file.GetAttributes() | SmbFile.AttrReadonly);
            }
        }

        public void SetReadOnly(string path, bool isReadOnly)
        {
            var file = CreateSmbFile(path);

            var isCurrentlyReadOnly = !file.CanWrite();

            if (isCurrentlyReadOnly && !isReadOnly)
            {
                file.SetReadWrite();
            }
            else if (!isCurrentlyReadOnly && isReadOnly)
            {
                file.SetReadOnly();
            }
        }

        public void DeleteFile(string path)
        {
            var file = CreateSmbFile(path);

            AssertFileExists(file, path);

            file.Delete();
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            var file = CreateSmbFile(path);

            AssertDirectoryExists(file, path);

            file.Delete();
        }

        public void CreateDirectory(string path)
        {
        }

        public string[] ReadAllLines(string path)
        {
            var lines = new List<string>();

            using (var stream = OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        lines.Add(reader.ReadLine());
                    }
                }
            }

            return lines.ToArray();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines)
        {
            using (var stream = GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
            {
                using (var writer = new StreamWriter(stream))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        private void AssertFileExists(SmbFile file, string path)
        {
            if (!file.Exists())
            {
                throw new FileNotFoundException("File not found.", path);
            }
        }

        private void AssertDirectoryExists(SmbFile file, string path)
        {
            if (!file.Exists())
            {
                throw new FileNotFoundException("File not found.", path);
            }
        }

        public Stream OpenRead(string path)
        {
            var file = CreateSmbFile(path);

            AssertFileExists(file, path);

            return file.GetInputStream();
        }

        private Stream OpenWrite(string path)
        {
            var file = CreateSmbFile(path);

            AssertFileExists(file, path);

            return file.GetInputStream();
        }

        public void CopyFile(string source, string target, bool overwrite)
        {
            if (string.Equals(source, target, StringComparison.Ordinal))
            {
                throw new ArgumentException("Cannot CopyFile when source and target are the same");
            }

            using (var input = OpenRead(source))
            {
                using (var output = GetFileStream(target, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
                {
                    input.CopyTo(output);
                }
            }
        }

        public void MoveFile(string source, string target)
        {
            if (string.Equals(source, target, StringComparison.Ordinal))
            {
                throw new ArgumentException("Cannot MoveFile when source and target are the same");
            }

            using (var input = OpenRead(source))
            {
                using (var output = GetFileStream(target, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
                {
                    input.CopyTo(output);
                }
            }

            DeleteFile(source);
        }

        public void MoveDirectory(string source, string target)
        {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path)
        {
            var dir = CreateSmbFile(path);

            return dir.Exists() && dir.IsDirectory();
        }

        public bool FileExists(string path)
        {
            var file = CreateSmbFile(path);
            return file.Exists();
        }

        public string ReadAllText(string path, Encoding encoding)
        {
            using (var stream = OpenRead(path))
            {
                using (var reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public Stream GetFileStream(string path, FileOpenMode mode, FileAccessMode access, FileShareMode share)
        {
            if (mode == FileOpenMode.OpenOrCreate)
            {
                var file = CreateSmbFile(path);
                if (!file.Exists())
                {
                    file.CreateNewFile();
                }

                mode = FileOpenMode.Open;
            }

            if (mode == FileOpenMode.CreateNew)
            {
                var file = CreateSmbFile(path);
                if (file.Exists())
                {
                    throw new IOException("File already exists");
                }

                file.CreateNewFile();

                mode = FileOpenMode.Open;
            }

            if (mode == FileOpenMode.Create)
            {
                var file = CreateSmbFile(path);
                if (file.Exists())
                {
                    if (file.IsHidden())
                    {
                        throw new UnauthorizedAccessException(string.Format("File {0} already exists and is hidden", path));
                    }

                    file.Delete();
                    file.CreateNewFile();
                }
                else
                {
                    file.CreateNewFile();
                }

                mode = FileOpenMode.Open;
            }

            if (mode == FileOpenMode.Open)
            {
                if (access == FileAccessMode.Read)
                {
                    return OpenRead(path);
                }
                if (access == FileAccessMode.Write)
                {
                    return OpenWrite(path);
                }
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }

        public void WriteAllBytes(string path, byte[] bytes)
        {
            using (var stream = GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        public void WriteAllText(string path, string text)
        {
            using (var stream = GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(text);
                }
            }
        }

        public void WriteAllText(string path, string text, Encoding encoding)
        {
            using (var stream = GetFileStream(path, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.None))
            {
                using (var writer = new StreamWriter(stream, encoding))
                {
                    writer.Write(text);
                }
            }
        }

        public string ReadAllText(string path)
        {
            using (var stream = OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        public byte[] ReadAllBytes(string path)
        {
            using (var stream = OpenRead(path))
            {
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    ms.Position = 0;
                    return ms.ToArray();
                }
            }
        }

        private SmbFile CreateSmbDirectoryForListFiles(string path)
        {
            // In order to call ListFiles, it has to end with the separator

            return CreateSmbFile(path.TrimEnd('/') + '/');
        }

        public IEnumerable<FileSystemMetadata> GetDirectories(string path, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                if (file.IsDirectory())
                {
                    yield return ToMetadata(file);
                }
            }
        }

        public IEnumerable<FileSystemMetadata> GetFiles(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                if (file.IsFile())
                {
                    var filePath = GetReturnPath(file);
                    var extension = Path.GetExtension(filePath);

                    if (extensions == null || extensions.Length == 0 || extensions.Contains(extension ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return ToMetadata(file);
                    }
                }
            }
        }

        public IEnumerable<FileSystemMetadata> GetFileSystemEntries(string path, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                yield return ToMetadata(file);
            }
        }

        public IEnumerable<string> GetFileSystemEntryPaths(string path, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                yield return GetReturnPath(file);
            }
        }

        public IEnumerable<string> GetFilePaths(string path, string[] extensions, bool enableCaseSensitiveExtensions, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                if (file.IsFile())
                {
                    var filePath = GetReturnPath(file);
                    var extension = Path.GetExtension(filePath);

                    if (extensions == null || extensions.Length == 0 || extensions.Contains(extension ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        public IEnumerable<string> GetDirectoryPaths(string path, bool recursive = false)
        {
            var dir = CreateSmbDirectoryForListFiles(path);
            AssertDirectoryExists(dir, path);

            var list = ListFiles(dir, recursive);

            foreach (var file in list)
            {
                if (file.IsDirectory())
                {
                    yield return GetReturnPath(file);
                }
            }
        }

        private IEnumerable<SmbFile> ListFiles(SmbFile dir, bool recursive)
        {
            var list = dir.ListFiles();

            foreach (var file in list)
            {
                yield return file;

                if (recursive && file.IsDirectory())
                {
                    foreach (var subFile in ListFiles(file, recursive))
                    {
                        yield return subFile;
                    }
                }
            }
        }
    }
}
