using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the user data rows <see cref="BaseItemMapper"/> hands the domain item. A domain item is
/// held for as long as its folder holds it, so a row that still points back at the entity it was
/// read with would keep that entity - and everything loaded alongside it - alive with it.
/// </summary>
public class BaseItemMapperUserDataTests
{
    [Fact]
    public void Map_CopiesUserDataWithoutTheEntityGraphBehindIt()
    {
        var itemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User("someone", "Default", "Default");
        var entity = new BaseItemEntity { Id = itemId, Type = "MediaBrowser.Controller.Entities.TV.Episode" };

        var row = new UserData
        {
            ItemId = itemId,
            Item = entity,
            UserId = userId,
            User = user,
            CustomDataKey = "key",
            PlayCount = 3,
            PlaybackPositionTicks = 1234,
            IsFavorite = true,
            Played = true,
            Rating = 7.5,
            LastPlayedDate = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc),
            AudioStreamIndex = 1,
            SubtitleStreamIndex = 2,
            Likes = true
        };

        entity.UserData = [row];

        var dto = BaseItemMapper.Map(entity, new Folder(), null);

        var mapped = Assert.Single(dto.UserData);
        Assert.Null(mapped.Item);
        Assert.Null(mapped.User);

        // The values callers actually read still come through.
        Assert.Equal(itemId, mapped.ItemId);
        Assert.Equal(userId, mapped.UserId);
        Assert.Equal("key", mapped.CustomDataKey);
        Assert.Equal(3, mapped.PlayCount);
        Assert.Equal(1234, mapped.PlaybackPositionTicks);
        Assert.True(mapped.IsFavorite);
        Assert.True(mapped.Played);
        Assert.Equal(7.5, mapped.Rating);
        Assert.Equal(row.LastPlayedDate, mapped.LastPlayedDate);
        Assert.Equal(1, mapped.AudioStreamIndex);
        Assert.Equal(2, mapped.SubtitleStreamIndex);
        Assert.True(mapped.Likes);
    }

    [Fact]
    public void Map_WithoutUserData_YieldsAnEmptyCollection()
    {
        var entity = new BaseItemEntity
        {
            Id = Guid.NewGuid(),
            Type = "MediaBrowser.Controller.Entities.Folder"
        };

        var dto = BaseItemMapper.Map(entity, new Folder(), null);

        Assert.Empty(dto.UserData);
    }
}
