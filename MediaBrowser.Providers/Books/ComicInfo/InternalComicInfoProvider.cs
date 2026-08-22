using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives;

namespace MediaBrowser.Providers.Books.ComicInfo;

/// <summary>
/// Handles metadata for comics which is saved as an XML document inside the comic itself.
/// </summary>
public class InternalComicInfoProvider : IComicProvider
{
    private readonly IFileSystem _fileSystem;
    private readonly NamingOptions _namingOptions;
    private readonly ILogger<InternalComicInfoProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InternalComicInfoProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="namingOptions">Instance of the <see cref="NamingOptions"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{InternalComicInfoProvider}"/> interface.</param>
    public InternalComicInfoProvider(IFileSystem fileSystem, NamingOptions namingOptions, ILogger<InternalComicInfoProvider> logger)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _namingOptions = namingOptions;
    }

    /// <inheritdoc />
    public async ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        var comicInfoXml = await LoadXml(info, cancellationToken).ConfigureAwait(false);

        if (comicInfoXml is null)
        {
            _logger.LogDebug("Could not load ComicInfo metadata for {Path} from XML file. No internal XML in comic archive.", info.Path);
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var book = ComicInfoReader.ReadComicBookMetadata(comicInfoXml);

        if (book is null)
        {
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

        ComicInfoReader.ReadPeopleMetadata(comicInfoXml, metadataResult);
        ComicInfoReader.ReadCultureInfoInto(comicInfoXml, "ComicInfo/LanguageISO", cultureInfo => metadataResult.ResultLanguage = cultureInfo.ThreeLetterISOLanguageName);

        return metadataResult;
    }

    /// <inheritdoc />
    public bool HasItemChanged(BaseItem item)
    {
        var file = GetComicBookFile(item.Path);

        if (file is null)
        {
            return false;
        }

        return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
    }

    private async Task<XDocument?> LoadXml(ItemInfo info, CancellationToken cancellationToken)
    {
        var path = GetComicBookFile(info.Path)?.FullName;

        if (path is null)
        {
            return null;
        }

        try
        {
            // open the comic archive and try to get the ComicInfo.xml entry
            using var stream = AsyncFile.OpenRead(path);
            var archive = await ArchiveFactory.OpenAsyncArchive(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var container = await archive.EntriesAsync
                .FirstOrDefaultAsync(e => string.Equals(e.Key, ComicInfoReader.ComicRackMetaFile, StringComparison.OrdinalIgnoreCase), cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (container is null)
            {
                return null;
            }

            var containerStream = await container.OpenEntryStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (containerStream.ConfigureAwait(false))
            {
                return await XDocument.LoadAsync(containerStream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "could not load internal XML from {Path}", path);
            return null;
        }
    }

    private FileSystemMetadata? GetComicBookFile(string path)
    {
        var fileInfo = _fileSystem.GetFileSystemInfo(path);

        if (fileInfo.IsDirectory)
        {
            return null;
        }

        // only parse files that are known to have internal metadata
        // SharpCompress is required to support non-ZIP archives
        if (!_namingOptions.ComicFileExtensions.Contains(fileInfo.Extension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fileInfo;
    }
}
