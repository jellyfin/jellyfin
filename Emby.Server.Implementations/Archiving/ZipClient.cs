using System.IO;
using MediaBrowser.Model.IO;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Readers.GZip;

namespace Emby.Server.Implementations.Archiving
{
    /// <summary>
    /// Class DotNetZipClient.
    /// </summary>
    public class ZipClient : IZipClient
    {
        /// <inheritdoc />
        public void ExtractAllFromGz(Stream source, string targetPath, bool overwriteExistingFiles)
        {
            using var reader = GZipReader.Open(source);
            var options = new ExtractionOptions
            {
                ExtractFullPath = true,
                Overwrite = overwriteExistingFiles
            };

            Directory.CreateDirectory(targetPath);
            reader.WriteAllToDirectory(targetPath, options);
        }

        /// <inheritdoc />
        public void ExtractFirstFileFromGz(Stream source, string targetPath, string defaultFileName)
        {
            using var reader = GZipReader.Open(source);
            if (reader.MoveToNextEntry())
            {
                var entry = reader.Entry;

                var filename = entry.Key;
                if (string.IsNullOrWhiteSpace(filename))
                {
                    filename = defaultFileName;
                }

                reader.WriteEntryToFile(Path.Combine(targetPath, filename));
            }
        }
    }
}
