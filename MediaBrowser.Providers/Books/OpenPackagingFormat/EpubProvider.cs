using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Books.OpenPackagingFormat
{
    /// <summary>
    /// Provides book metadata from OPF content in an EPUB item.
    /// </summary>
    public class EpubProvider : ILocalMetadataProvider<Book>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<EpubProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EpubProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{EpubProvider}"/> interface.</param>
        public EpubProvider(IFileSystem fileSystem, ILogger<EpubProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "EPUB Metadata";

        /// <inheritdoc />
        public Task<MetadataResult<Book>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetEpubFile(info.Path)?.FullName;

            if (path is null)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }

            var result = ReadEpubAsZip(path, cancellationToken);

            if (result is null)
            {
                return Task.FromResult(new MetadataResult<Book> { HasMetadata = false });
            }
            else
            {
                return Task.FromResult(result);
            }
        }

        private FileSystemMetadata? GetEpubFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.IsDirectory)
            {
                return null;
            }

            if (!string.Equals(Path.GetExtension(fileInfo.FullName), ".epub", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileInfo;
        }

        private MetadataResult<Book>? ReadEpubAsZip(string path, CancellationToken cancellationToken)
        {
            using var epub = ZipFile.OpenRead(path);

            var opfFilePath = EpubUtils.ReadContentFilePath(epub);
            if (opfFilePath == null)
            {
                return null;
            }

            var opf = epub.GetEntry(opfFilePath);
            if (opf == null)
            {
                return null;
            }

            using var opfStream = opf.Open();

            var opfDocument = new XmlDocument();
            opfDocument.Load(opfStream);

            var utilities = new OpfReader<EpubProvider>(opfDocument, _logger);
            return utilities.ReadOpfData(cancellationToken);
        }
    }
}
