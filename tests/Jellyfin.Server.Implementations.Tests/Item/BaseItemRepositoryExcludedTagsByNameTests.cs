using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The by-name listings rebuild their query from the user, which only knows the tags that user blocks. Tags a
/// caller excludes on its own - the library collage, which is shared by every user and so has no user to ask -
/// have to survive that, both for the media that reaches a by-name item and for the by-name item itself.
/// </summary>
public sealed class BaseItemRepositoryExcludedTagsByNameTests : SqliteDbTestFixture
{
    private const string Tag = "Adult";

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup;

    public BaseItemRepositoryExcludedTagsByNameTests()
    {
        _itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(_itemTypeLookup);
        Seed();
    }

    [Fact]
    public void GetAllArtists_ExcludedTagsWithoutAUser_DropsTheArtistsTheyReach()
    {
        var result = _repository.GetAllArtists(new InternalItemsQuery
        {
            DtoOptions = new DtoOptions(false),
            ExcludeInheritedTags = [Tag]
        });

        // "Explicit" is credited only by a tagged track and "Tagged" carries the tag itself.
        Assert.Equal(["Clean", "Mixed"], Names(result));
    }

    [Fact]
    public void GetAllArtists_UserAssignedWithoutItsTags_StillDropsWhatTheUserBlocks()
    {
        // Assigning the user leaves ExcludeInheritedTags empty, so carrying the caller's tags over must not
        // take the place of the ones the user blocks.
        var user = new User("restricted", "auth", "reset");
        user.SetPreference(PreferenceKind.BlockedTags, [Tag]);

        var result = _repository.GetAllArtists(new InternalItemsQuery
        {
            DtoOptions = new DtoOptions(false),
            User = user
        });

        Assert.Equal(["Clean", "Mixed"], Names(result));
    }

    [Fact]
    public void GetAllArtists_WithoutExcludedTags_ReturnsEveryArtist()
    {
        var result = _repository.GetAllArtists(new InternalItemsQuery { DtoOptions = new DtoOptions(false) });

        Assert.Equal(["Clean", "Explicit", "Mixed", "Tagged"], Names(result));
    }

    private static string[] Names(MediaBrowser.Model.Querying.QueryResult<(BaseItem Item, MediaBrowser.Model.Dto.ItemCounts? ItemCounts)> result)
        => result.Items.Select(i => i.Item.Name!).Order(StringComparer.Ordinal).ToArray();

    private void Seed()
    {
        using var context = CreateDbContext();

        var tag = CreateItemValue(ItemValueType.Tags, Tag);
        context.ItemValues.Add(tag);

        // Credited by an untagged track only.
        var clean = AddArtist(context, "Clean");
        AddTrack(context, clean);

        // Credited by a tagged track only, so nothing the tag leaves alone reaches it.
        var explicitArtist = AddArtist(context, "Explicit");
        AddTrack(context, explicitArtist, tag);

        // Still reached through its untagged track once the tagged one is gone.
        var mixed = AddArtist(context, "Mixed");
        AddTrack(context, mixed, tag);
        AddTrack(context, mixed);

        // Tagged itself, although the track crediting it is not.
        var tagged = AddArtist(context, "Tagged");
        AddTrack(context, tagged);
        context.ItemValuesMap.Add(CreateMap(tagged.Artist, tag));

        context.SaveChanges();
    }

    private (BaseItemEntity Artist, ItemValue Credit) AddArtist(JellyfinDbContext context, string name)
    {
        var artist = CreateItem(BaseItemKind.MusicArtist, name);
        var credit = CreateItemValue(ItemValueType.Artist, name);

        context.BaseItems.Add(artist);
        context.ItemValues.Add(credit);

        return (artist, credit);
    }

    private void AddTrack(JellyfinDbContext context, (BaseItemEntity Artist, ItemValue Credit) artist, ItemValue? tag = null)
    {
        var track = CreateItem(BaseItemKind.Audio, $"{artist.Artist.Name} - Track", mediaType: "Audio");

        context.BaseItems.Add(track);
        context.ItemValuesMap.Add(CreateMap(track, artist.Credit));
        if (tag is not null)
        {
            context.ItemValuesMap.Add(CreateMap(track, tag));
        }
    }

    private BaseItemEntity CreateItem(BaseItemKind kind, string name, string? mediaType = null)
    {
        var id = Guid.NewGuid();

        return new BaseItemEntity
        {
            Id = id,
            Type = _itemTypeLookup.BaseItemKindNames[kind],
            Name = name,
            CleanName = name.GetCleanValue(),
            PresentationUniqueKey = id.ToString("N"),
            MediaType = mediaType,
            IsFolder = kind == BaseItemKind.MusicArtist,
            IsVirtualItem = false
        };
    }

    private static ItemValue CreateItemValue(ItemValueType type, string value)
    {
        return new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = type,
            Value = value,
            CleanValue = value.GetCleanValue()
        };
    }

    private static ItemValueMap CreateMap(BaseItemEntity item, ItemValue itemValue)
    {
        return new ItemValueMap
        {
            ItemId = item.Id,
            ItemValueId = itemValue.ItemValueId,
            Item = item,
            ItemValue = itemValue
        };
    }
}
