using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Books.ComicBookInfo.Models;
using Microsoft.Extensions.Logging;
using SharpCompress.Archives.Zip;

namespace MediaBrowser.Providers.Books.ComicBookInfo;

/// <summary>
/// ComicBookInfo provider.
/// </summary>
public class ComicBookInfoProvider : IComicProvider
{
    private readonly ILogger<ComicBookInfoProvider> _logger;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComicBookInfoProvider"/> class.
    /// </summary>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ComicBookInfoProvider}"/> interface.</param>
    public ComicBookInfoProvider(IFileSystem fileSystem, ILogger<ComicBookInfoProvider> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<MetadataResult<Book>> ReadMetadata(ItemInfo info, IDirectoryService directoryService, CancellationToken cancellationToken)
    {
        var path = GetComicBookFile(info.Path)?.FullName;

        if (path is null)
        {
            _logger.LogError("could not load comic: {Path}", info.Path);
            return new MetadataResult<Book> { HasMetadata = false };
        }

        try
        {
            Stream stream = File.OpenRead(path);

            // not yet async: https://github.com/adamhathcock/sharpcompress/pull/565
            await using (stream.ConfigureAwait(false))
            using (var archive = ZipArchive.Open(stream))
            {
                if (!archive.IsComplete)
                {
                    _logger.LogError("incomplete comic archive: {Path}", info.Path);
                    return new MetadataResult<Book> { HasMetadata = false };
                }

                var volume = archive.Volumes.First();

                if (volume.Comment is null)
                {
                    _logger.LogInformation("missing ComicBookInfo in archive comment: {Path}", info.Path);
                    return new MetadataResult<Book> { HasMetadata = false };
                }

                var comicBookMetadata = JsonSerializer.Deserialize<ComicBookInfoFormat>(volume.Comment, JsonDefaults.Options);

                if (comicBookMetadata is null)
                {
                    _logger.LogError("ComicBookInfo deserialization failure: {Path}", info.Path);
                    return new MetadataResult<Book> { HasMetadata = false };
                }

                return SaveMetadata(comicBookMetadata);
            }
        }
        catch (Exception)
        {
            _logger.LogError("failed to load ComicBookInfo metadata: {Path}", info.Path);
            return new MetadataResult<Book> { HasMetadata = false };
        }
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

    private MetadataResult<Book> SaveMetadata(ComicBookInfoFormat comic)
    {
        if (comic.Metadata is null)
        {
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var book = ReadComicBookMetadata(comic.Metadata);

        if (book is null)
        {
            return new MetadataResult<Book> { HasMetadata = false };
        }

        var metadataResult = new MetadataResult<Book> { Item = book, HasMetadata = true };

        if (comic.Metadata.Language is not null)
        {
            metadataResult.ResultLanguage = ReadCultureInfoInto(comic.Metadata.Language);
        }

        if (comic.Metadata.Credits.Count > 0)
        {
            ReadPeopleMetadata(comic.Metadata, metadataResult);
        }

        return metadataResult;
    }

    private FileSystemMetadata? GetComicBookFile(string path)
    {
        var fileInfo = _fileSystem.GetFileSystemInfo(path);

        if (fileInfo.IsDirectory)
        {
            return null;
        }

        // only parse files that are known to have ComicBookInfo metadata
        return fileInfo.Extension.Equals(".cbz", StringComparison.OrdinalIgnoreCase) ? fileInfo : null;
    }

    private static Book? ReadComicBookMetadata(ComicBookInfoMetadata comic)
    {
        var book = new Book();
        var hasFoundMetadata = false;

        hasFoundMetadata |= ReadStringInto(comic.Title, title => book.Name = title);
        hasFoundMetadata |= ReadStringInto(comic.Series, series => book.SeriesName = series);
        hasFoundMetadata |= ReadStringInto(comic.Genre, genre => book.AddGenre(genre));
        hasFoundMetadata |= ReadStringInto(comic.Comments, overview => book.Overview = overview);
        hasFoundMetadata |= ReadStringInto(comic.Publisher, publisher => book.SetStudios([publisher]));

        if (comic.PublicationYear is not null)
        {
            book.ProductionYear = comic.PublicationYear;
            hasFoundMetadata = true;
        }

        if (comic.Issue is not null)
        {
            book.IndexNumber = comic.Issue;
            hasFoundMetadata = true;
        }

        if (comic.Tags.Count > 0)
        {
            book.Tags = comic.Tags.ToArray();
            hasFoundMetadata = true;
        }

        if (comic.PublicationYear is not null && comic.PublicationMonth is not null)
        {
            book.PremiereDate = ReadTwoPartDateInto(comic.PublicationYear.Value, comic.PublicationMonth.Value);
            hasFoundMetadata = true;
        }

        return hasFoundMetadata ? book : null;
    }

    private static void ReadPeopleMetadata(ComicBookInfoMetadata comic, MetadataResult<Book> metadataResult)
    {
        foreach (var person in comic.Credits)
        {
            if (person.Person is null || person.Role is null)
            {
                continue;
            }

            if (person.Person.Contains(',', StringComparison.InvariantCultureIgnoreCase))
            {
                var name = person.Person.Split(',');
                person.Person = name[1].Trim(' ') + " " + name[0].Trim(' ');
            }

            if (!Enum.TryParse(person.Role, out PersonKind personKind))
            {
                personKind = PersonKind.Unknown;
            }

            if (string.Equals("Colorer", person.Role, StringComparison.OrdinalIgnoreCase))
            {
                personKind = PersonKind.Colorist;
            }

            metadataResult.AddPerson(new PersonInfo { Name = person.Person, Type = personKind });
        }
    }

    private static string? ReadCultureInfoInto(string language)
    {
        try
        {
            return CultureInfo.GetCultureInfo(language).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static bool ReadStringInto(string? data, Action<string> commitResult)
    {
        if (!string.IsNullOrWhiteSpace(data))
        {
            commitResult(data);
            return true;
        }

        return false;
    }

    private static DateTime? ReadTwoPartDateInto(int year, int month)
    {
        try
        {
            // use first day of the month because this format doesn't include a day
            return new DateTime(year, month, 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
