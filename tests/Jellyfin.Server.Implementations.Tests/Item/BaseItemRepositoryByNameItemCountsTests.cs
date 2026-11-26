using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The by-name listings count what a by-name item is attached to by joining a link table to
/// BaseItems. One item can reach the same by-name item through more than one link row, so the
/// join has to be counted per distinct item; counting rows reports a multiple of the truth.
/// </summary>
public sealed class BaseItemRepositoryByNameItemCountsTests : SqliteDbTestFixture
{
    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryByNameItemCountsTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);
    }

    [Fact]
    public void GetAllArtists_AlbumCreditedAsArtistAndAlbumArtist_CountsTheAlbumOnce()
    {
        // GetAllArtists spans both credit types, so an album whose artist is also its album artist
        // reaches the one artist through two credits.
        SeedArtistWithAlbum(PersonKind.Artist, PersonKind.AlbumArtist);

        var result = _repository.GetAllArtists(CreateCountingQuery());

        var (_, counts) = Assert.Single(result.Items);
        Assert.NotNull(counts);
        Assert.Equal(1, counts.AlbumCount);
        Assert.Equal(1, counts.ItemCount);
    }

    [Fact]
    public void GetAlbumArtists_TwoValueRowsCleaningToOneName_CountsTheAlbumOnce()
    {
        // Two differently cased credits of one type, both resolved to the one artist.
        SeedArtistWithAlbum(PersonKind.AlbumArtist, PersonKind.AlbumArtist);

        var result = _repository.GetAlbumArtists(CreateCountingQuery());

        var (_, counts) = Assert.Single(result.Items);
        Assert.NotNull(counts);
        Assert.Equal(1, counts.AlbumCount);
    }

    [Fact]
    public void GetArtists_TwoValueRowsCleaningToOneName_CountsTheAlbumOnce()
    {
        SeedArtistWithAlbum(PersonKind.Artist, PersonKind.Artist);

        var result = _repository.GetArtists(CreateCountingQuery());

        var (_, counts) = Assert.Single(result.Items);
        Assert.NotNull(counts);
        Assert.Equal(1, counts.AlbumCount);
    }

    [Theory]
    [InlineData(BaseItemKind.Book)]
    [InlineData(BaseItemKind.BoxSet)]
    public void GetGenres_TaggedBookOrBoxSet_CountsIt(BaseItemKind kind)
    {
        // The listing used to dispatch only nine of the eleven counted types, so a genre on a book
        // or a box set read as zero in a list and as one on the genre's own page.
        SeedGenreWith(kind);

        var result = _repository.GetGenres(CreateCountingQuery());

        var (_, counts) = Assert.Single(result.Items);
        Assert.NotNull(counts);
        Assert.Equal(1, kind == BaseItemKind.Book ? counts.BookCount : counts.BoxSetCount);
        Assert.Equal(1, counts.ItemCount);
    }

    /// <summary>
    /// Seeds one genre carried by a single item of the given kind.
    /// </summary>
    /// <param name="kind">The kind of the tagged item.</param>
    private void SeedGenreWith(BaseItemKind kind)
    {
        const string Name = "Reference";
        const string CleanName = "reference";

        using var ctx = CreateDbContext();

        var genreId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
        var taggedId = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001");

        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = genreId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Genre],
            Name = Name,
            CleanName = CleanName,
            PresentationUniqueKey = genreId.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false
        });

        var tagged = new BaseItemEntity
        {
            Id = taggedId,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = "Tagged",
            CleanName = "tagged",
            PresentationUniqueKey = taggedId.ToString("N"),
            IsFolder = false,
            IsVirtualItem = false
        };
        ctx.BaseItems.Add(tagged);

        var itemValue = new ItemValue
        {
            ItemValueId = Guid.Parse("ffffffff-0000-0000-0000-000000000001"),
            Type = ItemValueType.Genre,
            Value = Name,
            CleanValue = CleanName
        };

        ctx.ItemValues.Add(itemValue);
        ctx.ItemValuesMap.Add(new ItemValueMap
        {
            ItemId = taggedId,
            ItemValueId = itemValue.ItemValueId,
            Item = tagged,
            ItemValue = itemValue
        });

        ctx.SaveChanges();
    }

    private static InternalItemsQuery CreateCountingQuery()
    {
        return new InternalItemsQuery(new User("test", "auth", "reset"))
        {
            DtoOptions = new DtoOptions(true) { Fields = [ItemFields.ItemCounts] }
        };
    }

    /// <summary>
    /// Seeds one artist and a single album crediting it through two credits of the given kinds.
    /// </summary>
    /// <param name="first">The kind of the first credit.</param>
    /// <param name="second">The kind of the second credit.</param>
    private void SeedArtistWithAlbum(PersonKind first, PersonKind second)
    {
        const string Name = "Tangerine Dream";
        const string CleanName = "tangerine dream";

        using var ctx = CreateDbContext();

        var artistId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var albumId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = artistId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicArtist],
            Name = Name,
            CleanName = CleanName,
            PresentationUniqueKey = artistId.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false
        });

        var album = new BaseItemEntity
        {
            Id = albumId,
            Type = _itemTypeLookup.BaseItemKindNames[BaseItemKind.MusicAlbum],
            Name = "Phaedra",
            CleanName = "phaedra",
            PresentationUniqueKey = albumId.ToString("N"),
            IsFolder = true,
            IsVirtualItem = false
        };
        ctx.BaseItems.Add(album);

        var kinds = new[] { first, second };
        for (var i = 0; i < kinds.Length; i++)
        {
            var credit = new People
            {
                Id = Guid.Parse($"cccccccc-0000-0000-0000-{i:D12}"),
                Name = i == 0 ? Name : Name.ToUpperInvariant(),
                CleanName = CleanName,
                PersonType = kinds[i].ToString(),
                ItemId = artistId
            };

            ctx.Peoples.Add(credit);
            ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap
            {
                ItemId = albumId,
                PeopleId = credit.Id,
                Item = album,
                People = credit,
                Role = string.Empty
            });
        }

        ctx.SaveChanges();
    }
}
