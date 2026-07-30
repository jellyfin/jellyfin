#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class DisabledCustomNetflixRepository : ICustomNetflixRepository
{
    private const string NotConfiguredMessage = "CustomNetflix PostgreSQL is not configured. Set CustomNetflix:PostgreSqlConnectionString or ConnectionStrings:CustomNetflixPostgres.";
    private readonly string _failureMessage;

    public DisabledCustomNetflixRepository()
        : this(false, NotConfiguredMessage)
    {
    }

    public DisabledCustomNetflixRepository(bool isConfigured, string failureMessage)
    {
        IsEnabled = isConfigured;
        _failureMessage = failureMessage;
    }

    public bool IsEnabled { get; }

    public Task EnsureSchemaAsync(CancellationToken cancellationToken)
        => IsEnabled ? ThrowDisabled() : Task.CompletedTask;

    public Task CheckHealthAsync(CancellationToken cancellationToken)
        => IsEnabled ? ThrowDisabled() : Task.CompletedTask;

    public Task<IReadOnlyList<ProfileRow>> GetProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<ProfileRow>>();

    public Task<ProfileRow?> GetProfileAsync(Guid profileId, CancellationToken cancellationToken)
        => ThrowDisabled<ProfileRow?>();

    public Task<ProfileRow> CreateProfileAsync(Guid jellyfinUserId, string name, string? avatarId, bool isChild, bool isDefault, int maxProfiles, CancellationToken cancellationToken)
        => ThrowDisabled<ProfileRow>();

    public Task<ProfileRow?> UpdateProfileAsync(
        Guid profileId,
        string? name,
        string? avatarId,
        ProfileSettingsRow? settings,
        PlaybackPreferencesRow? playbackPreferences,
        CancellationToken cancellationToken)
        => ThrowDisabled<ProfileRow?>();

    public Task<bool> SoftDeleteProfileAsync(Guid profileId, CancellationToken cancellationToken)
        => ThrowDisabled<bool>();

    public Task<CustomNetflixUserDataKeys> PurgeUserDataAsync(Guid jellyfinUserId, CancellationToken cancellationToken)
        => ThrowDisabled<CustomNetflixUserDataKeys>();

    public Task<Guid?> GetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, CancellationToken cancellationToken)
        => ThrowDisabled<Guid?>();

    public Task SetActiveProfileAsync(Guid jellyfinUserId, string tokenHash, Guid profileId, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task<WatchProgressRow?> GetProgressAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<WatchProgressRow?>();

    public Task<IReadOnlyList<WatchProgressRow>> GetProgressForItemsAsync(Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<WatchProgressRow>>();

    public Task UpsertProgressAsync(WatchProgressRow progress, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task UpsertProgressRowsAsync(IReadOnlyList<WatchProgressRow> progressRows, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task InsertWatchEventsAsync(IReadOnlyList<WatchEventRow> watchEvents, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task<int> PurgeWatchEventsAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken)
        => ThrowDisabled<int>();

    public Task<IReadOnlyList<WatchProgressRow>> GetContinueWatchingAsync(Guid profileId, int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<WatchProgressRow>>();

    public Task<IReadOnlyList<WatchHistoryRow>> GetWatchHistoryAsync(Guid profileId, int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<WatchHistoryRow>>();

    public Task<IReadOnlyList<MyListRow>> GetMyListAsync(Guid profileId, int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<MyListRow>>();

    public Task<MyListRow?> GetMyListItemAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<MyListRow?>();

    public Task<MyListRow> AddToMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<MyListRow>();

    public Task<bool> RemoveFromMyListAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<bool>();

    public Task<ItemFeedbackRow?> GetItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<ItemFeedbackRow?>();

    public Task<IReadOnlyList<ItemFeedbackRow>> GetLikedItemFeedbacksAsync(Guid profileId, int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<ItemFeedbackRow>>();

    public Task<IReadOnlyList<ItemFeedbackRow>> GetItemFeedbacksForItemsAsync(Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<ItemFeedbackRow>>();

    public Task<ItemFeedbackRow> UpsertItemFeedbackAsync(Guid profileId, Guid itemId, string feedback, CancellationToken cancellationToken)
        => ThrowDisabled<ItemFeedbackRow>();

    public Task<bool> DeleteItemFeedbackAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<bool>();

    public Task<WatchProgressRow> SetPlayedAsync(Guid profileId, Guid jellyfinUserId, Guid itemId, bool played, string itemType, CancellationToken cancellationToken)
        => ThrowDisabled<WatchProgressRow>();

    public Task<bool> HideFromContinueWatchingAsync(Guid profileId, Guid itemId, CancellationToken cancellationToken)
        => ThrowDisabled<bool>();

    public Task<AutoplayStateRow> TrackAutoplayAsync(Guid profileId, Guid currentItemId, bool completed, CancellationToken cancellationToken)
        => ThrowDisabled<AutoplayStateRow>();

    public Task<DateTime> ConfirmStillWatchingAsync(Guid profileId, CancellationToken cancellationToken)
        => ThrowDisabled<DateTime>();

    public Task<IReadOnlyList<CustomMediaSegmentRow>> GetManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<CustomMediaSegmentRow>>();

    public Task ReplaceManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<CustomMediaSegmentRow> segments, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task DeleteManualMediaSegmentsAsync(Guid itemId, IReadOnlyList<string>? segmentTypes, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task<IReadOnlyList<RankedItemRow>> GetTrendingItemsAsync(int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<RankedItemRow>>();

    public Task<IReadOnlyList<RankedItemRow>> GetTopTenItemsAsync(int limit, CancellationToken cancellationToken)
        => ThrowDisabled<IReadOnlyList<RankedItemRow>>();

    public Task<RankingSnapshotRow?> GetRankingSnapshotAsync(string rankingId, int limit, DateTime utcNow, CancellationToken cancellationToken)
        => ThrowDisabled<RankingSnapshotRow?>();

    public Task SaveRankingSnapshotAsync(string rankingId, IReadOnlyList<RankedItemRow> items, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task<HomeSnapshotRow?> GetHomeSnapshotAsync(Guid profileId, string snapshotKey, DateTime utcNow, CancellationToken cancellationToken)
        => ThrowDisabled<HomeSnapshotRow?>();

    public Task SaveHomeSnapshotAsync(Guid profileId, string snapshotKey, string payloadJson, DateTime generatedAt, DateTime expiresAt, CancellationToken cancellationToken)
        => ThrowDisabled();

    public Task DeleteHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken)
        => ThrowDisabled();

    private Task ThrowDisabled()
        => throw new CustomNetflixUnavailableException(_failureMessage);

    private Task<T> ThrowDisabled<T>()
        => throw new CustomNetflixUnavailableException(_failureMessage);
}
