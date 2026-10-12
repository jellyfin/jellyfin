using System;
using System.Linq;
using System.Xml.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Providers.Books.ComicInfo;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// A publication date read from comic metadata is a calendar date, so it has to come back from the
/// database as the same day whatever time zone the server runs in.
/// </summary>
public sealed class ComicPremiereDateStorageTests : SqliteDbTestFixture
{
    [Theory]
    [InlineData(2021, 3, 1)]
    [InlineData(1999, 12, 31)]
    public void ComicInfoPremiereDate_RoundTrip_KeepsCalendarDate(int year, int month, int day)
    {
        var xml = XDocument.Parse($"<ComicInfo><Year>{year}</Year><Month>{month}</Month><Day>{day}</Day></ComicInfo>");
        var book = ComicInfoReader.ReadComicBookMetadata(xml);
        Assert.NotNull(book);

        var itemId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = itemId,
                Type = typeof(Book).FullName!,
                PremiereDate = book.PremiereDate
            });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            var stored = Assert.NotNull(context.BaseItems.Single(i => i.Id.Equals(itemId)).PremiereDate);
            Assert.Equal(year, stored.Year);
            Assert.Equal(month, stored.Month);
            Assert.Equal(day, stored.Day);
            Assert.Equal(TimeSpan.Zero, stored.TimeOfDay);
        }
    }
}
