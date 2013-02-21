using Ionic.Zip;
using MediaBrowser.Model.IO;
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
                using (var zipFile = ZipFile.Read(fileStream))
                {
                    zipFile.ExtractAll(targetPath, overwriteExistingFiles ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.DoNotOverwrite);
                }
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
            using (var zipFile = ZipFile.Read(source))
            {
                zipFile.ExtractAll(targetPath, overwriteExistingFiles ? ExtractExistingFileAction.OverwriteSilently : ExtractExistingFileAction.DoNotOverwrite);
            }
        }
    }
}
