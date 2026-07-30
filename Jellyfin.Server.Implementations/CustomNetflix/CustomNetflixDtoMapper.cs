#pragma warning disable CS1591

using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixDtoMapper
{
    public static DtoOptions CreateCardOptions(bool includeTrickplay = false)
        => new(false)
        {
            Fields = includeTrickplay
                ? [ItemFields.PrimaryImageAspectRatio, ItemFields.ChildCount, ItemFields.Trickplay]
                : [ItemFields.PrimaryImageAspectRatio, ItemFields.ChildCount],
            ImageTypes = [ImageType.Primary, ImageType.Thumb, ImageType.Backdrop],
            ImageTypeLimit = 1,
            EnableUserData = false,
            AddCurrentProgram = false
        };

    public static CustomNetflixWatchProgressDto MapProgress(WatchProgressRow row)
        => new()
        {
            ProfileId = row.ProfileId,
            ItemId = row.ItemId,
            MediaSourceId = row.MediaSourceId,
            PositionSeconds = row.PositionSeconds,
            DurationSeconds = row.DurationSeconds,
            PercentViewed = row.PercentViewed,
            Completed = row.Completed,
            PlayCount = row.PlayCount,
            LastPlayedAt = row.LastPlayedAt
        };

    public static CustomNetflixWatchHistoryDto MapHistory(WatchHistoryRow row)
        => new()
        {
            ProfileId = row.ProfileId,
            ItemId = row.ItemId,
            FirstPlayedAt = row.FirstPlayedAt,
            LastPlayedAt = row.LastPlayedAt,
            CompletedAt = row.CompletedAt,
            PlayCount = row.PlayCount
        };
}
