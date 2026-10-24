using System.IO;
using System.IO.Compression;
using System.Text;
using MediaBrowser.Providers.Books.OpenPackagingFormat;
using Xunit;

namespace Jellyfin.Providers.Tests.Books;

public class EpubUtilsTests
{
    [Fact]
    public void ReadContentFilePath_ReadsRootFileFromContainer()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("META-INF/container.xml").Open(), Encoding.UTF8);
            writer.Write(
                """
                <?xml version="1.0"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
                  </rootfiles>
                </container>
                """);
        }

        stream.Position = 0;
        using var epub = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal("OEBPS/content.opf", EpubUtils.ReadContentFilePath(epub));
    }
}
