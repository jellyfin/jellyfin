#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.CustomNetflix;

public sealed class CustomNetflixProfileDto
{
    public Guid Id { get; set; }

    public Guid JellyfinUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? AvatarId { get; set; }

    public bool IsDefault { get; set; }

    public bool IsChild { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public CustomNetflixProfileSettingsDto Settings { get; set; } = new();

    public CustomNetflixPlaybackPreferencesDto PlaybackPreferences { get; set; } = new();
}

public sealed class CustomNetflixProfileSettingsDto
{
    public bool AutoplayEnabled { get; set; } = true;

    public int AutoplayDelaySeconds { get; set; } = 8;

    public bool SkipIntroEnabled { get; set; } = true;

    public bool SkipRecapEnabled { get; set; } = true;
}

public sealed class CustomNetflixPlaybackPreferencesDto
{
    public bool PreferDirectPlay { get; set; } = true;

    public bool AllowContainerRemuxing { get; set; } = true;

    public bool AllowVideoTranscoding { get; set; } = true;

    public bool AllowAudioTranscoding { get; set; } = true;

    public bool PreferHardwareTranscoding { get; set; } = true;

    public int? MaxStreamingBitrate { get; set; }

    public string? PreferredAudioLanguage { get; set; }

    public string? PreferredSubtitleLanguage { get; set; }

    public bool SubtitlesEnabled { get; set; }

    public bool AudioDescriptionEnabled { get; set; }

    public bool ClosedCaptionsEnabled { get; set; }

    public bool SkipCreditsEnabled { get; set; }
}

public sealed class CustomNetflixCreateProfileRequest
{
    public string Name { get; set; } = string.Empty;

    public string? AvatarId { get; set; }

    public bool IsChild { get; set; }
}

public sealed class CustomNetflixUpdateProfileRequest
{
    public string? Name { get; set; }

    public string? AvatarId { get; set; }

    public bool? IsChild { get; set; }

    public CustomNetflixProfileSettingsDto? Settings { get; set; }

    public CustomNetflixPlaybackPreferencesDto? PlaybackPreferences { get; set; }
}

public sealed class CustomNetflixSetActiveProfileRequest
{
    public Guid ProfileId { get; set; }
}

public sealed class CustomNetflixActiveProfileDto
{
    public Guid ProfileId { get; set; }

    public CustomNetflixProfileDto? Profile { get; set; }
}

public sealed class CustomNetflixWatchProgressDto
{
    public Guid ProfileId { get; set; }

    public Guid ItemId { get; set; }

    public Guid? MediaSourceId { get; set; }

    public double PositionSeconds { get; set; }

    public double DurationSeconds { get; set; }

    public double PercentViewed { get; set; }

    public bool Completed { get; set; }

    public int PlayCount { get; set; }

    public DateTime LastPlayedAt { get; set; }
}

public sealed class CustomNetflixWatchProgressReportRequest
{
    public Guid ItemId { get; set; }

    public Guid? MediaSourceId { get; set; }

    public double PositionSeconds { get; set; }

    public double DurationSeconds { get; set; }

    public bool IsPaused { get; set; }

    public string? PlaySessionId { get; set; }

    public string? ClientName { get; set; }
}

public sealed class CustomNetflixMarkPlayedRequest
{
    public bool Played { get; set; }
}

public sealed class CustomNetflixWatchProgressBatchRequest
{
    public IReadOnlyList<Guid> ItemIds { get; set; } = Array.Empty<Guid>();
}

public sealed class CustomNetflixWatchProgressBatchResponse
{
    public IReadOnlyList<CustomNetflixWatchProgressDto> Items { get; set; } = Array.Empty<CustomNetflixWatchProgressDto>();
}

public sealed class CustomNetflixContinueWatchingItemDto
{
    public BaseItemDto Item { get; set; } = new();

    public CustomNetflixWatchProgressDto Progress { get; set; } = new();
}

public sealed class CustomNetflixWatchHistoryItemDto
{
    public BaseItemDto Item { get; set; } = new();

    public CustomNetflixWatchHistoryDto History { get; set; } = new();
}

public sealed class CustomNetflixWatchHistoryDto
{
    public Guid ProfileId { get; set; }

    public Guid ItemId { get; set; }

    public DateTime FirstPlayedAt { get; set; }

    public DateTime LastPlayedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int PlayCount { get; set; }
}

public sealed class CustomNetflixMyListResponseDto
{
    public Guid ProfileId { get; set; }

    public IReadOnlyList<CustomNetflixMyListItemDto> Items { get; set; } = Array.Empty<CustomNetflixMyListItemDto>();
}

public sealed class CustomNetflixMyListItemDto
{
    public BaseItemDto Item { get; set; } = new();

    public DateTime AddedAt { get; set; }

    public CustomNetflixWatchProgressDto? Progress { get; set; }
}

public sealed class CustomNetflixMyListStatusDto
{
    public Guid ProfileId { get; set; }

    public Guid ItemId { get; set; }

    public bool IsInMyList { get; set; }

    public DateTime? AddedAt { get; set; }
}

public sealed class CustomNetflixHomeItemDto
{
    public BaseItemDto Item { get; set; } = new();

    public CustomNetflixWatchProgressDto? Progress { get; set; }

    public string RecommendationReason { get; set; } = string.Empty;
}

public sealed class CustomNetflixHomeRowDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TitleKey { get; set; } = string.Empty;

    public IReadOnlyList<CustomNetflixHomeItemDto> Items { get; set; } = Array.Empty<CustomNetflixHomeItemDto>();
}

public sealed class CustomNetflixHomeResponseDto
{
    public Guid ProfileId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public IReadOnlyList<CustomNetflixHomeRowDto> Rows { get; set; } = Array.Empty<CustomNetflixHomeRowDto>();
}

public sealed class CustomNetflixRecommendationsResponseDto
{
    public Guid ProfileId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public bool Personalized { get; set; }

    public string Title { get; set; } = string.Empty;

    public string TitleKey { get; set; } = string.Empty;

    public IReadOnlyList<CustomNetflixHomeItemDto> Items { get; set; } = Array.Empty<CustomNetflixHomeItemDto>();
}

public sealed class CustomNetflixItemDetailsDto
{
    public Guid ProfileId { get; set; }

    public DateTime GeneratedAt { get; set; }

    public BaseItemDto Item { get; set; } = new();

    public CustomNetflixWatchProgressDto? Progress { get; set; }
}

public sealed class CustomNetflixNextEpisodeDto
{
    public bool HasNext { get; set; }

    public int DelaySeconds { get; set; }

    public BaseItemDto? Item { get; set; }

    public double ResumePositionSeconds { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string ReasonKey { get; set; } = string.Empty;

    public bool RequiresStillWatchingConfirmation { get; set; }

    public string TitleKey { get; set; } = string.Empty;
}

public sealed class CustomNetflixStillWatchingConfirmationDto
{
    public Guid ProfileId { get; set; }

    public bool Required { get; set; }

    public DateTime ConfirmedAt { get; set; }
}

public sealed class CustomNetflixMediaSegmentsResponseDto
{
    public Guid ItemId { get; set; }

    public IReadOnlyList<CustomNetflixMediaSegmentDto> Segments { get; set; } = Array.Empty<CustomNetflixMediaSegmentDto>();
}

public sealed class CustomNetflixMediaSegmentDto
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public string Type { get; set; } = string.Empty;

    public double StartSeconds { get; set; }

    public double EndSeconds { get; set; }

    public string Source { get; set; } = string.Empty;
}

public sealed class CustomNetflixManualMediaSegmentsRequest
{
    public IReadOnlyList<CustomNetflixManualMediaSegmentRequest> Segments { get; set; } = Array.Empty<CustomNetflixManualMediaSegmentRequest>();
}

public sealed class CustomNetflixManualMediaSegmentRequest
{
    public string Type { get; set; } = string.Empty;

    public double StartSeconds { get; set; }

    public double EndSeconds { get; set; }
}

public sealed class CustomNetflixRankedItemsResponseDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string TitleKey { get; set; } = string.Empty;

    public DateTime GeneratedAt { get; set; }

    public IReadOnlyList<CustomNetflixRankedItemDto> Items { get; set; } = Array.Empty<CustomNetflixRankedItemDto>();
}

public sealed class CustomNetflixRankedItemDto
{
    public int Rank { get; set; }

    public double Score { get; set; }

    public BaseItemDto Item { get; set; } = new();
}
