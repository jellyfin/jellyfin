using MediaBrowser.Model.IO;
using SharpCompress.Archive.SevenZip;
using SharpCompress.Archive.Tar;
using SharpCompress.Common;
using SharpCompress.Reader;
using System.IO;

namespace MediaBrowser.Common.Implementations.Archiving
{
    /// <summary>
    /// Class DotNetZipClient
    /// </summary>
    public class ZipClient : IZipClient
    {
        /// <summary>
        /// Extracts all.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAll(string sourceFile, string targetPath, bool overwriteExistingFiles)
        {
            using (var fileStream = File.OpenRead(sourceFile))
            {
                ExtractAll(fileStream, targetPath, overwriteExistingFiles);
            }
        }

        /// <summary>
        /// Extracts all.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAll(Stream source, string targetPath, bool overwriteExistingFiles)
        {
            using (var reader = ReaderFactory.Open(source))
            {
                var options = ExtractOptions.ExtractFullPath;

                if (overwriteExistingFiles)
                {
                    options = options | ExtractOptions.Overwrite;
                }

                reader.WriteAllToDirectory(targetPath, options);
            }
        }

        /// <summary>
        /// Extracts all from7z.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAllFrom7z(string sourceFile, string targetPath, bool overwriteExistingFiles)
        {
            using (var fileStream = File.OpenRead(sourceFile))
            {
                ExtractAllFrom7z(fileStream, targetPath, overwriteExistingFiles);
            }
        }

        /// <summary>
        /// Extracts all from7z.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAllFrom7z(Stream source, string targetPath, bool overwriteExistingFiles)
        {
            using (var archive = SevenZipArchive.Open(source))
            {
                using (var reader = archive.ExtractAllEntries())
                {
                    var options = ExtractOptions.ExtractFullPath;

                    if (overwriteExistingFiles)
                    {
                        options = options | ExtractOptions.Overwrite;
                    }

                    reader.WriteAllToDirectory(targetPath, options);
                }
            }
        }


        /// <summary>
        /// Extracts all from tar.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAllFromTar(string sourceFile, string targetPath, bool overwriteExistingFiles)
        {
            using (var fileStream = File.OpenRead(sourceFile))
            {
                ExtractAllFromTar(fileStream, targetPath, overwriteExistingFiles);
            }
        }

        /// <summary>
        /// Extracts all from tar.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        public void ExtractAllFromTar(Stream source, string targetPath, bool overwriteExistingFiles)
        {
            using (var archive = TarArchive.Open(source))
            {
                using (var reader = archive.ExtractAllEntries())
                {
                    var options = ExtractOptions.ExtractFullPath;

                    if (overwriteExistingFiles)
                    {
                        options = options | ExtractOptions.Overwrite;
                    }

                    reader.WriteAllToDirectory(targetPath, options);
                }
            }
        }
    }
}
