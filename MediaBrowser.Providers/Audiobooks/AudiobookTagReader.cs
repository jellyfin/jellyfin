using System;
using System.Collections.Generic;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Audiobooks;

/// <summary>
/// Audiobook tag reader for extracting metadata from audiobook files.
/// </summary>
/// <typeparam name="TCategoryName">The type of category.</typeparam>
public class AudiobookTagReader<TCategoryName>
{
    private readonly TagLib.File _audiobookFile;
    private readonly ILogger<TCategoryName> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudiobookTagReader{TCategoryName}"/> class.
    /// </summary>
    /// <param name="audiobookFile">The TagLib.File to parse.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public AudiobookTagReader(TagLib.File audiobookFile, ILogger<TCategoryName> logger)
    {
        _audiobookFile = audiobookFile;
        _logger = logger;
    }

    /// <summary>
    /// Read metadata from the audiobook file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The metadata result.</returns>
    public MetadataResult<AudioBook> ReadMetadata(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var book = CreateBookFromTags();
            var bookResult = new MetadataResult<AudioBook>
            {
                Item = book,
                HasMetadata = true,
                People = new List<PersonInfo>()
            };
            ExtractAuthors(bookResult);
            ExtractNarrators(bookResult);
            return bookResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading audiobook metadata");
            return new MetadataResult<AudioBook> { HasMetadata = false };
        }
    }

    private AudioBook CreateBookFromTags()
    {
        var book = new AudioBook();
        var tag = _audiobookFile.Tag;

        // Extract basic information - prefer tag title, then folder name, then filename
        book.Name = !string.IsNullOrEmpty(tag.Title) ? tag.Title : GetTitleFromPath();

        // Extract description/comment
        if (!string.IsNullOrEmpty(tag.Comment))
        {
            book.Overview = tag.Comment;
        }

        // Extract album (can be series name for audiobooks)
        if (!string.IsNullOrEmpty(tag.Album))
        {
            // Check if this looks like a series (e.g., "Series Name, Book 1")
            if (TryParseSeriesInfo(tag.Album, out var seriesName, out var bookNumber))
            {
                book.SeriesName = seriesName;
                book.IndexNumber = bookNumber;
            }
            else
            {
                // Use album as series name if different from title
                if (string.IsNullOrEmpty(book.Name) || !book.Name.Equals(tag.Album, StringComparison.OrdinalIgnoreCase))
                {
                    book.SeriesName = tag.Album;
                }
            }
        }

        // Also try to parse series info from the book title itself
        if (!string.IsNullOrEmpty(book.Name) && string.IsNullOrEmpty(book.SeriesName))
        {
            if (AudiobookUtils.TryParseSeriesInfo(book.Name, out var titleSeriesName, out var titleBookNumber))
            {
                book.SeriesName = titleSeriesName;
                book.IndexNumber = titleBookNumber;
            }
        }

        // Extract year
        if (tag.Year > 0)
        {
            book.ProductionYear = (int)tag.Year;
            book.PremiereDate = new DateTime((int)tag.Year, 1, 1);
        }

        // Extract genres
        if (tag.Genres != null && tag.Genres.Length > 0)
        {
            foreach (var genre in tag.Genres)
            {
                if (!string.IsNullOrEmpty(genre))
                {
                    book.AddGenre(genre);
                }
            }
        }

        // Extract publisher (sometimes stored in various fields)
        var publisher = GetPublisher(tag);
        if (!string.IsNullOrEmpty(publisher))
        {
            book.AddStudio(publisher);
        }

        // Try to extract ISBN or other identifiers from various fields
        ExtractIdentifiers(book, tag);

        return book;
    }

    private string GetTitleFromPath()
    {
        try
        {
            // First try to get title from folder structure
            if (AudiobookUtils.TryParseBookTitleFromPath(_audiobookFile.Name, out var folderTitle))
            {
                return folderTitle;
            }

            // Fallback to filename
            var filename = System.IO.Path.GetFileNameWithoutExtension(_audiobookFile.Name);
            return CleanTitle(filename);
        }
        catch
        {
            return "Unknown Title";
        }
    }

    private string CleanTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return "Unknown Title";
        }

        // Remove common patterns like [Unabridged], (Unabridged), etc.
        var cleanTitle = System.Text.RegularExpressions.Regex.Replace(
            title,
            @"\s*[\[\(]\s*(unabridged|abridged|audiobook)\s*[\]\)]\s*",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return cleanTitle.Trim();
    }

    private bool TryParseSeriesInfo(string album, out string seriesName, out int? bookNumber)
    {
        seriesName = album;
        bookNumber = null;

        // Look for patterns like "Series Name, Book 1" or "Series Name 1"
        var match = System.Text.RegularExpressions.Regex.Match(
            album,
            @"^(.+?)(?:,?\s*(?:book|vol|volume)?\s*(\d+))?$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (match.Success)
        {
            seriesName = match.Groups[1].Value.Trim();
            if (match.Groups[2].Success && int.TryParse(match.Groups[2].Value, out var number))
            {
                bookNumber = number;
                return true;
            }
        }

        return false;
    }

    private string? GetPublisher(TagLib.Tag tag)
    {
        // Try various fields where publisher might be stored
        if (!string.IsNullOrEmpty(tag.Publisher))
        {
            return tag.Publisher;
        }

        // Sometimes stored in copyright field
        if (!string.IsNullOrEmpty(tag.Copyright))
        {
            // Extract publisher from copyright notice
            var copyrightMatch = System.Text.RegularExpressions.Regex.Match(
                tag.Copyright,
                @"(?:©|\(c\)|copyright)\s*\d*\s*(.+?)(?:\s|$)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (copyrightMatch.Success)
            {
                return copyrightMatch.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private void ExtractIdentifiers(AudioBook book, TagLib.Tag tag)
    {
        // Try to find ISBN in various text fields
        var fieldsToCheck = new[] { tag.Comment, tag.Description, tag.Copyright, tag.Publisher };

        foreach (var field in fieldsToCheck)
        {
            if (string.IsNullOrEmpty(field))
            {
                continue;
            }

            var isbn = AudiobookUtils.ExtractIsbn(field);
            if (isbn != null)
            {
                book.SetProviderId("ISBN", isbn);
                break;
            }
        }
    }

    private void ExtractAuthors(MetadataResult<AudioBook> result)
    {
        var tag = _audiobookFile.Tag;
        var artists = new List<string>();

        // Primary artist/author - set on the Artists property for AudioBook
        if (tag.Performers != null && tag.Performers.Length > 0)
        {
            foreach (var author in tag.Performers)
            {
                if (!string.IsNullOrEmpty(author))
                {
                    artists.Add(author);
                    result.AddPerson(new PersonInfo
                    {
                        Name = author,
                        Type = PersonKind.Author
                    });
                }
            }
        }
        else if (tag.AlbumArtists != null && tag.AlbumArtists.Length > 0)
        {
            foreach (var author in tag.AlbumArtists)
            {
                if (!string.IsNullOrEmpty(author))
                {
                    artists.Add(author);
                    result.AddPerson(new PersonInfo
                    {
                        Name = author,
                        Type = PersonKind.Author
                    });
                }
            }
        }

        if (artists.Count > 0)
        {
            result.Item.Artists = artists;
        }
    }

    private void ExtractNarrators(MetadataResult<AudioBook> result)
    {
        var tag = _audiobookFile.Tag;

        // Narrators are sometimes stored in the Composer field for audiobooks
        if (tag.Composers != null && tag.Composers.Length > 0)
        {
            foreach (var narrator in tag.Composers)
            {
                if (!string.IsNullOrEmpty(narrator))
                {
                    result.AddPerson(new PersonInfo
                    {
                        Name = narrator,
                        Type = PersonKind.Composer // Using Composer for narrator role
                    });
                }
            }
        }

        // Sometimes narrators are in the conductor field
        if (!string.IsNullOrEmpty(tag.Conductor))
        {
            result.AddPerson(new PersonInfo
            {
                Name = tag.Conductor,
                Type = PersonKind.Composer
            });
        }
    }
}
