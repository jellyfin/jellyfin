using System;
using Jellyfin.Server.Implementations.CustomNetflix;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.CustomNetflix;

public class CustomNetflixDtoMapperTests
{
    [Fact]
    public void CreateCardOptions_OnlyRequestsCardData()
    {
        var options = CustomNetflixDtoMapper.CreateCardOptions();

        Assert.Equal(
            [ItemFields.PrimaryImageAspectRatio, ItemFields.ChildCount],
            options.Fields);
        Assert.Equal([ImageType.Primary, ImageType.Thumb, ImageType.Backdrop], options.ImageTypes);
        Assert.Equal(1, options.ImageTypeLimit);
        Assert.True(options.EnableImages);
        Assert.False(options.EnableUserData);
        Assert.False(options.AddCurrentProgram);
    }

    [Fact]
    public void CreateCardOptions_OnlyLoadsTrickplayWhenRequested()
    {
        var options = CustomNetflixDtoMapper.CreateCardOptions(includeTrickplay: true);

        Assert.Contains(ItemFields.Trickplay, options.Fields);
    }

    [Fact]
    public void MapProgress_PreservesRepositoryValues()
    {
        var row = new WatchProgressRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            123.45,
            1500,
            8.23,
            false,
            2,
            DateTime.UtcNow);

        var dto = CustomNetflixDtoMapper.MapProgress(row);

        Assert.Equal(row.ProfileId, dto.ProfileId);
        Assert.Equal(row.ItemId, dto.ItemId);
        Assert.Equal(row.MediaSourceId, dto.MediaSourceId);
        Assert.Equal(row.PositionSeconds, dto.PositionSeconds);
        Assert.Equal(row.DurationSeconds, dto.DurationSeconds);
        Assert.Equal(row.PercentViewed, dto.PercentViewed);
        Assert.Equal(row.Completed, dto.Completed);
        Assert.Equal(row.PlayCount, dto.PlayCount);
        Assert.Equal(row.LastPlayedAt, dto.LastPlayedAt);
    }

    [Fact]
    public void MapHistory_PreservesRepositoryValues()
    {
        var row = new WatchHistoryRow(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(-5),
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow,
            3);

        var dto = CustomNetflixDtoMapper.MapHistory(row);

        Assert.Equal(row.ProfileId, dto.ProfileId);
        Assert.Equal(row.ItemId, dto.ItemId);
        Assert.Equal(row.FirstPlayedAt, dto.FirstPlayedAt);
        Assert.Equal(row.LastPlayedAt, dto.LastPlayedAt);
        Assert.Equal(row.CompletedAt, dto.CompletedAt);
        Assert.Equal(row.PlayCount, dto.PlayCount);
    }
}
