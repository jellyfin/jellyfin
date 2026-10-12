using System;
using System.Xml.Linq;
using MediaBrowser.Providers.Books.ComicInfo;
using Xunit;

namespace Jellyfin.Providers.Tests.Books;

public class ComicInfoReaderTests
{
    [Theory]
    [InlineData(2021, 3, 1)]
    [InlineData(1999, 12, 31)]
    [InlineData(2020, 2, 29)]
    public void ReadComicBookMetadata_YearMonthDay_ReturnsUtcCalendarDate(int year, int month, int day)
    {
        var xml = XDocument.Parse($"<ComicInfo><Year>{year}</Year><Month>{month}</Month><Day>{day}</Day></ComicInfo>");

        var book = ComicInfoReader.ReadComicBookMetadata(xml);

        Assert.NotNull(book);
        var premiereDate = Assert.NotNull(book.PremiereDate);
        Assert.Equal(DateTimeKind.Utc, premiereDate.Kind);
        Assert.Equal(year, premiereDate.Year);
        Assert.Equal(month, premiereDate.Month);
        Assert.Equal(day, premiereDate.Day);
        Assert.Equal(TimeSpan.Zero, premiereDate.TimeOfDay);
    }
}
