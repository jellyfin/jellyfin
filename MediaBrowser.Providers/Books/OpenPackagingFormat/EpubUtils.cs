using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace MediaBrowser.Providers.Books.OpenPackagingFormat
{
    /// <summary>
    /// Utilities for EPUB files.
    /// </summary>
    public static class EpubUtils
    {
        /// <summary>
        /// Attempt to read content from ZIP archive.
        /// </summary>
        /// <param name="epub">The ZIP archive.</param>
        /// <returns>The content file path.</returns>
        public static string? ReadContentFilePath(ZipArchive epub)
        {
            var container = epub.GetEntry(Path.Combine("META-INF", "container.xml"));
            if (container == null)
            {
                return null;
            }

            using var containerStream = container.Open();

            XNamespace containerNamespace = "urn:oasis:names:tc:opendocument:xmlns:container";
            var containerDocument = XDocument.Load(containerStream);
            var element = containerDocument.Descendants(containerNamespace + "rootfile").FirstOrDefault();

            return element?.Attribute("full-path")?.Value;
        }
    }
}
