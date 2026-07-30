#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed record ProfileRow(
    Guid Id,
    Guid JellyfinUserId,
    string Name,
    string? AvatarId,
    bool IsDefault,
    bool IsChild,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ProfileSettingsRow Settings,
    PlaybackPreferencesRow PlaybackPreferences);

internal sealed record ProfileSettingsRow(
    Guid ProfileId,
    bool AutoplayEnabled,
    int AutoplayDelaySeconds,
    bool SkipIntroEnabled,
    bool SkipRecapEnabled);

internal sealed record PlaybackPreferencesRow(
    Guid ProfileId,
    bool PreferDirectPlay,
    bool AllowContainerRemuxing,
    bool AllowVideoTranscoding,
    bool AllowAudioTranscoding,
    bool PreferHardwareTranscoding,
    int? MaxStreamingBitrate,
    string? PreferredAudioLanguage,
    string? PreferredSubtitleLanguage,
    bool SubtitlesEnabled,
    bool AudioDescriptionEnabled,
    bool ClosedCaptionsEnabled,
    bool SkipCreditsEnabled);

internal sealed record AutoplayStateRow(
    Guid ProfileId,
    int ConsecutiveCount,
    Guid? LastItemId,
    bool StillWatchingRequired,
    DateTime? ConfirmedAt);

internal sealed record CustomNetflixUserDataKeys(
    IReadOnlyList<Guid> ProfileIds,
    IReadOnlyList<string> ActiveProfileTokenHashes);

internal sealed record WatchProgressRow(
    Guid ProfileId,
    Guid ItemId,
    Guid? MediaSourceId,
    double PositionSeconds,
    double DurationSeconds,
    double PercentViewed,
    bool Completed,
    int PlayCount,
    DateTime LastPlayedAt);

internal sealed record WatchEventRow(
    Guid Id,
    Guid ProfileId,
    Guid JellyfinUserId,
    Guid ItemId,
    string ItemType,
    string EventType,
    double PositionSeconds,
    double DurationSeconds,
    string? PlaySessionId,
    string? ClientName);

internal sealed record WatchHistoryRow(
    Guid ProfileId,
    Guid ItemId,
    DateTime FirstPlayedAt,
    DateTime LastPlayedAt,
    DateTime? CompletedAt,
    int PlayCount);

internal sealed record MyListRow(
    Guid ProfileId,
    Guid ItemId,
    DateTime AddedAt);

internal sealed record ItemFeedbackRow(
    Guid ProfileId,
    Guid ItemId,
    string Feedback,
    DateTime UpdatedAt);

internal sealed record CustomMediaSegmentRow(
    Guid Id,
    Guid ItemId,
    string SegmentType,
    double StartSeconds,
    double EndSeconds,
    string Source,
    DateTime UpdatedAt);

internal sealed record RankedItemRow(
    Guid ItemId,
    double Score,
    int Rank);

internal sealed record RankingSnapshotRow(
    string RankingId,
    IReadOnlyList<RankedItemRow> Items,
    DateTime GeneratedAt,
    DateTime ExpiresAt);

internal sealed record HomeSnapshotRow(
    Guid ProfileId,
    string SnapshotKey,
    string PayloadJson,
    DateTime GeneratedAt,
    DateTime ExpiresAt);
