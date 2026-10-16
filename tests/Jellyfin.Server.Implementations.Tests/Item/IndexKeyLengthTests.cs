using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Holds every engine to storing items whose strings that are indexed without a bound of their own are long: a path of
/// 850 and of 851 characters, and the sort name of an episode at the length where its key of the type, series and
/// presentation index reaches 1700 bytes and one more. SQL Server holds at most 1700 bytes of key in an index that is not
/// clustered and refuses the longer rows, which is a known gap.
/// </summary>
public sealed class IndexKeyLengthTests : DatabaseTestFixture
{
    // An episode, as the type column holds it, and keys as long as a series and an episode have them.
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const int SeriesKeyLength = 116;
    private const int KeyLength = 32;

    // The room of the sort name in that index: (1700 bytes - 2 * (43 + 116 + 32)) / 2 bytes per character.
    private const int SortNameRoom = 659;

    [Fact]
    public void Save_PathOf850Characters_IsStored()
    {
        var id = Save(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", Path = "/" + new string('p', 849) });

        Assert.Equal(850, Stored(id).Path!.Length);
    }

    [Fact]
    public void Save_PathOf851Characters_IsStored()
    {
        KnownGap.On(KnownGaps.IndexKeyOverTheLimitOnSqlServer, () =>
        {
            var id = Save(new BaseItemEntity { Id = Guid.NewGuid(), Type = "Test", Path = "/" + new string('p', 850) });

            Assert.Equal(851, Stored(id).Path!.Length);
        });
    }

    [Fact]
    public void Save_SortNameThatFillsTheKeyOfTheSeriesIndex_IsStored()
    {
        var id = Save(Episode(SortNameRoom));

        Assert.Equal(SortNameRoom, Stored(id).SortName!.Length);
    }

    [Fact]
    public void Save_SortNameOneCharacterOverTheKeyOfTheSeriesIndex_IsStored()
    {
        KnownGap.On(KnownGaps.IndexKeyOverTheLimitOnSqlServer, () =>
        {
            var id = Save(Episode(SortNameRoom + 1));

            Assert.Equal(SortNameRoom + 1, Stored(id).SortName!.Length);
        });
    }

    private static BaseItemEntity Episode(int sortNameLength) => new()
    {
        Id = Guid.NewGuid(),
        Type = EpisodeType,
        SeriesPresentationUniqueKey = new string('s', SeriesKeyLength),
        PresentationUniqueKey = new string('k', KeyLength),
        SortName = new string('n', sortNameLength)
    };

    private Guid Save(BaseItemEntity item)
    {
        using var context = CreateDbContext();
        context.BaseItems.Add(item);
        context.SaveChanges();
        return item.Id;
    }

    private BaseItemEntity Stored(Guid id)
    {
        using var context = CreateDbContext();
        return context.BaseItems.Single(e => e.Id.Equals(id));
    }
}
