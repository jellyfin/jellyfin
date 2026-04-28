using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Books.ComicInfo;

/// <summary>
/// Handles metadata for comics which is saved as an XML document. This XML document is not part
/// of the comic itself but an external file.
/// </summary>
public class ExternalComicInfoProvider : IComicProvider
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ExternalComicInfoProvider> _logger;
    private readonly ComicInfoReader _utilities = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalComicInfoProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ExternalComicInfoProvider}"/> interface.</param>
    public ExternalComicInfoProvider(IFileSystem fileSystem, ILogger<ExternalComicInfoProvider> logger)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        var comicInfoXml = await LoadXml(info, cancellationToken).ConfigureAwait(false);

        if (comicInfoXml is null)
        {
            _logger.LogInformation("Could not load ComicInfo metadata for {Path} from XML file.", info.Path);
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var book = _utilities.ReadComicBookMetadata(comicInfoXml);

        if (book is null)
        {
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

        _utilities.ReadPeopleMetadata(comicInfoXml, metadataResult);
        _utilities.ReadCultureInfoInto(comicInfoXml, "ComicInfo/LanguageISO", cultureInfo => metadataResult.ResultLanguage = cultureInfo.ThreeLetterISOLanguageName);

        return metadataResult;
    }

    /// <inheritdoc />
    public bool HasItemChanged(BaseItem item)
    {
        var file = GetXmlFilePath(item.Path);

        return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
    }

    private async Task<XDocument?> LoadXml(ItemInfo info, CancellationToken cancellationToken)
    {
        var path = GetXmlFilePath(info.Path).FullName;

        if (path is null)
        {
            return null;
        }

        try
        {
            using var reader = XmlReader.Create(path, new XmlReaderSettings { Async = true });
            var comicInfoXml = XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);

            return await comicInfoXml.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogInformation(e, "Could not load external xml from {Path}. This could mean there is no separate ComicInfo metadata file for this comic or the metadata is bundled within the comic.", path);
            return null;
        }
    }

    private FileSystemMetadata GetXmlFilePath(string path)
    {
        var fileInfo = _fileSystem.GetFileSystemInfo(path);
        var directoryInfo = fileInfo.IsDirectory ? fileInfo : _fileSystem.GetDirectoryInfo(Path.GetDirectoryName(path)!);
        var file = _fileSystem.GetFileInfo(Path.Combine(directoryInfo.FullName, Path.GetFileNameWithoutExtension(path) + ".xml"));

        return file.Exists ? file : _fileSystem.GetFileInfo(Path.Combine(directoryInfo.FullName, ComicInfoReader.ComicRackMetaFile));
    }
}
