using MediaBrowser.Model.IO;
using SharpCompress.Common;
using SharpCompress.Reader;
using System.IO;

namespace MediaBrowser.ServerApplication.Implementations
{
    /// <summary>
    /// Class DotNetZipClient
    /// </summary>
    public class DotNetZipClient : IZipClient
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
    }
}
