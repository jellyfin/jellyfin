#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Interface IZipClient
    /// </summary>
    public interface IZipClient
    {
        /// <summary>
        /// Extracts all.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAll(string sourceFile, string targetPath, bool overwriteExistingFiles);

        /// <summary>
        /// Extracts all.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAll(Stream source, string targetPath, bool overwriteExistingFiles);

        void ExtractAllFromGz(Stream source, string targetPath, bool overwriteExistingFiles);
        void ExtractFirstFileFromGz(Stream source, string targetPath, string defaultFileName);

        /// <summary>
        /// Extracts all from zip.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAllFromZip(Stream source, string targetPath, bool overwriteExistingFiles);

        /// <summary>
        /// Extracts all from7z.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAllFrom7z(string sourceFile, string targetPath, bool overwriteExistingFiles);

        /// <summary>
        /// Extracts all from7z.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAllFrom7z(Stream source, string targetPath, bool overwriteExistingFiles);

        /// <summary>
        /// Extracts all from tar.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAllFromTar(string sourceFile, string targetPath, bool overwriteExistingFiles);

        /// <summary>
        /// Extracts all from tar.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetPath">The target path.</param>
        /// <param name="overwriteExistingFiles">if set to <c>true</c> [overwrite existing files].</param>
        void ExtractAllFromTar(Stream source, string targetPath, bool overwriteExistingFiles);
    }
}
