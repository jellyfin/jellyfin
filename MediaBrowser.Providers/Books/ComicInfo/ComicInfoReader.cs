using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using SharpCompress;

namespace MediaBrowser.Providers.Books.ComicInfo;

/// <summary>
/// ComicInfo reader.
/// </summary>
public class ComicInfoReader
{
    /// <summary>
    /// Filename to check for comic metadata either next to the comic file or inside the archive.
    /// </summary>
    public const string ComicRackMetaFile = "ComicInfo.xml";

    /// <summary>
    /// Read comic book metadata.
    /// </summary>
    /// <param name="xml">The XDocument to read for comic metadata.</param>
    /// <returns>The resulting book.</returns>
    public Book? ReadComicBookMetadata(XDocument xml)
    {
        var book = new Book();
        var hasFoundMetadata = false;

        // this value is only used internally since Jellyfin has no manga flag
        var isManga = false;

        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Title", title => book.Name = title);
        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Manga", manga => isManga = manga.Equals("Yes", StringComparison.OrdinalIgnoreCase));
        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Series", series => book.SeriesName = series);
        hasFoundMetadata |= ReadIntInto(xml, "ComicInfo/Number", issue => book.IndexNumber = issue);
        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Summary", summary => book.Overview = summary);
        hasFoundMetadata |= ReadIntInto(xml, "ComicInfo/Year", year => book.ProductionYear = year);
        hasFoundMetadata |= ReadThreePartDateInto(xml, "ComicInfo/Year", "ComicInfo/Month", "ComicInfo/Day", dateTime => book.PremiereDate = dateTime);
        hasFoundMetadata |= ReadCommaSeparatedStringsInto(xml, "ComicInfo/Genre", genres => genres.ForEach(genre => book.AddGenre(genre)));
        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/Publisher", publisher => book.SetStudios([publisher]));

        hasFoundMetadata |= ReadStringInto(xml, "ComicInfo/AlternateSeries", title =>
        {
            if (isManga)
            {
                // Software like ComicTagger (https://github.com/comictagger/comictagger) will use
                // this field for the series name in the original language when tagging manga.
                book.OriginalTitle = title;
            }
            else
            {
                // Some US comics can be part of cross-over story arcs. This field is then used to
                // specify an alternate series.
            }
        });

        return hasFoundMetadata ? book : null;
    }

    /// <summary>
    /// Read people metadata.
    /// </summary>
    /// <param name="xml">The XDocument to read for people metadata.</param>
    /// <param name="metadataResult">The metadata result to update.</param>
    public void ReadPeopleMetadata(XDocument xml, MetadataResult<Book> metadataResult)
    {
        ReadCommaSeparatedStringsInto(xml, "ComicInfo/Writer", authors =>
        {
            authors.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.Author }));
        });

        ReadCommaSeparatedStringsInto(xml, "ComicInfo/Penciller", pencillers =>
        {
            pencillers.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.Penciller }));
        });

        ReadCommaSeparatedStringsInto(xml, "ComicInfo/Inker", inkers =>
        {
            inkers.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.Inker }));
        });

        ReadCommaSeparatedStringsInto(xml, "ComicInfo/Letterer", letterers =>
        {
            letterers.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.Letterer }));
        });

        ReadCommaSeparatedStringsInto(xml, "ComicInfo/CoverArtist", artists =>
        {
            artists.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.CoverArtist }));
        });

        ReadCommaSeparatedStringsInto(xml, "ComicInfo/Colourist", colorists =>
        {
            colorists.ForEach(p => metadataResult.AddPerson(new PersonInfo { Name = p, Type = PersonKind.Colorist }));
        });
    }

    /// <summary>
    /// Read culture information.
    /// </summary>
    /// <param name="xml">the XDocument to read for metadata.</param>
    /// <param name="xPath">The path to search.</param>
    /// <param name="commitResult">The action to take after parsing all metadata.</param>
    public void ReadCultureInfoInto(XDocument xml, string xPath, Action<CultureInfo> commitResult)
    {
        string? culture = null;

        if (!ReadStringInto(xml, xPath, value => culture = value))
        {
            return;
        }

        try
        {
            // culture cannot be null here as the method would have returned earlier
            commitResult(new CultureInfo(culture!));
        }
        catch (CultureNotFoundException)
        {
        }
    }

    private static bool ReadStringInto(XDocument xml, string xPath, Action<string> commitResult)
    {
        var resultElement = xml.XPathSelectElement(xPath);

        if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.Value))
        {
            commitResult(resultElement.Value);
            return true;
        }

        return false;
    }

    private static bool ReadCommaSeparatedStringsInto(XDocument xml, string xPath, Action<IEnumerable<string>> commitResult)
    {
        var resultElement = xml.XPathSelectElement(xPath);

        if (resultElement is null || string.IsNullOrWhiteSpace(resultElement.Value))
        {
            return false;
        }

        try
        {
            var splits = resultElement.Value.Split(",").Select(p => p.Trim()).ToArray();
            if (splits.Length < 1)
            {
                return false;
            }

            commitResult(splits);
            return true;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    private static bool ReadIntInto(XDocument xml, string xPath, Action<int> commitResult)
    {
        var resultElement = xml.XPathSelectElement(xPath);

        if (resultElement is not null && !string.IsNullOrWhiteSpace(resultElement.Value))
        {
            return ParseInt(resultElement.Value, commitResult);
        }

        return false;
    }

    private static bool ReadThreePartDateInto(XDocument xml, string yearXPath, string monthXPath, string dayXPath, Action<DateTime> commitResult)
    {
        int year = 0;
        int month = 0;
        int day = 0;
        var parsed = false;

        parsed |= ReadIntInto(xml, yearXPath, num => year = num);
        parsed |= ReadIntInto(xml, monthXPath, num => month = num);
        parsed |= ReadIntInto(xml, dayXPath, num => day = num);

        if (!parsed)
        {
            return false;
        }

        try
        {
            var dateTime = new DateTime(year, month, day);

            commitResult(dateTime);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static bool ParseInt(string input, Action<int> commitResult)
    {
        if (int.TryParse(input, out var parsed))
        {
            commitResult(parsed);
            return true;
        }

        return false;
    }
}
