using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Audiobooks
{
    /// <summary>
    /// Provides audiobook metadata from embedded tags in audio files.
    /// </summary>
    public class AudiobookProvider : ILocalMetadataProvider<AudioBook>
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger<AudiobookProvider> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudiobookProvider"/> class.
        /// </summary>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{AudiobookProvider}"/> interface.</param>
        public AudiobookProvider(IFileSystem fileSystem, ILogger<AudiobookProvider> logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
        }

        /// <inheritdoc />
        public string Name => "Audiobook Metadata";

        /// <inheritdoc />
        public Task<MetadataResult<AudioBook>> GetMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            var path = GetAudiobookFile(info.Path)?.FullName;

            if (path is null)
            {
                return Task.FromResult(new MetadataResult<AudioBook> { HasMetadata = false });
            }

            try
            {
                var result = ExtractMetadata(path, cancellationToken);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audiobook metadata for {Path}", path);
                return Task.FromResult(new MetadataResult<AudioBook> { HasMetadata = false });
            }
        }

        private FileSystemMetadata? GetAudiobookFile(string path)
        {
            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo == null || fileInfo.IsDirectory)
            {
                return null;
            }

            if (!AudiobookUtils.SupportedExtensions.Contains(
                    Path.GetExtension(fileInfo.FullName),
                    StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileInfo;
        }

        private MetadataResult<AudioBook> ExtractMetadata(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var file = TagLib.File.Create(path);
                var tagReader = new AudiobookTagReader<AudiobookProvider>(file, _logger);
                return tagReader.ReadMetadata(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read tags from audiobook file: {Path}", path);
                return new MetadataResult<AudioBook> { HasMetadata = false };
            }
        }
    }
}
