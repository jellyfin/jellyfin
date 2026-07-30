#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal interface ICustomNetflixRepository
{
    bool IsEnabled { get; }

    Task EnsureSchemaAsync(CancellationToken cancellationToken);

    Task CheckHealthAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ProfileRow>> GetProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken);

    Task<ProfileRow?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<ProfileRow> CreateProfileAsync(Guid jellyfinUserId, string name, string? avatarId, bool isChild, bool isDefault, int maxProfiles, CancellationToken cancellationToken);

    Task<ProfileRow?> UpdateProfileAsync(
        Guid profileId,
        string? name,
        string? avatarId,
        ProfileSettingsRow? settings,
        PlaybackPreferencesRow? playbackPreferences,
        CancellationToken cancellationToken);

    Task<bool> SoftDeleteProfileAsync(Guid profileId, CancellationToken cancellationToken);

    Task<CustomNetflixUserDataKeys> PurgeUserDataAsync(Guid jellyfinUserId, CancellationToken cancellationToken);

    Task<Guid?> GetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, CancellationToken cancellationToken);

    Task SetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, Guid profileId, CancellationToken cancellationToken);

    Task<WatchProgressRow?> GetProgressAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WatchProgressRow>> GetProgressForItemsAsync(Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    Task UpsertProgressAsync(WatchProgressRow progress, CancellationToken cancellationToken);

    Task UpsertProgressRowsAsync(IReadOnlyList<WatchProgressRow> progressRows, CancellationToken cancellationToken);

    Task InsertWatchEventsAsync(IReadOnlyList<WatchEventRow> watchEvents, CancellationToken cancellationToken);

    Task<int> PurgeWatchEventsAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken);

    Task<IReadOnlyList<WatchProgressRow>> GetContinueWatchingAsync(Guid profileId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<WatchHistoryRow>> GetWatchHistoryAsync(Guid profileId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<MyListRow>> GetMyListAsync(Guid profileId, int limit, CancellationToken cancellationToken);

    Task<MyListRow?> GetMyListItemAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<MyListRow> AddToMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<bool> RemoveFromMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<ItemFeedbackRow?> GetItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemFeedbackRow>> GetLikedItemFeedbacksAsync(Guid profileId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemFeedbackRow>> GetItemFeedbacksForItemsAsync(Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    Task<ItemFeedbackRow> UpsertItemFeedbackAsync(Guid profileId, Guid itemId, string feedback, CancellationToken cancellationToken);

    Task<bool> DeleteItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<WatchProgressRow> SetPlayedAsync(Guid profileId, Guid jellyfinUserId, Guid itemId, bool played, string itemType, CancellationToken cancellationToken);

    Task<bool> HideFromContinueWatchingAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<AutoplayStateRow> TrackAutoplayAsync(Guid profileId, Guid currentItemId, bool completed, CancellationToken cancellationToken);

    Task<DateTime> ConfirmStillWatchingAsync(Guid profileId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomMediaSegmentRow>> GetManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken);

    Task ReplaceManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<CustomMediaSegmentRow> segments, CancellationToken cancellationToken);

    Task DeleteManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken);

    Task<IReadOnlyList<RankedItemRow>> GetTrendingItemsAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<RankedItemRow>> GetTopTenItemsAsync(int limit, CancellationToken cancellationToken);

    Task<RankingSnapshotRow?> GetRankingSnapshotAsync(string rankingId, int limit, DateTime utcNow, CancellationToken cancellationToken);

    Task SaveRankingSnapshotAsync(string rankingId, IReadOnlyList<RankedItemRow> items, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken);

    Task<HomeSnapshotRow?> GetHomeSnapshotAsync(Guid profileId, string snapshotKey, DateTime utcNow, CancellationToken cancellationToken);

    Task SaveHomeSnapshotAsync(Guid profileId, string snapshotKey, string payloadJson, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken);

    Task DeleteHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken);
}
