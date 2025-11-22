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
        private const string ContainerNamespace = "urn:oasis:names:tc:opendocument:xmlns:container";

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

            var containerDocument = XDocument.Load(containerStream);
            var element = containerDocument.Descendants(ContainerNamespace + "rootfile").FirstOrDefault();

            return element?.Attribute("full-path")?.Value;
        }
    }
}
