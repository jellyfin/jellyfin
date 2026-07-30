#pragma warning disable CS1591, SA1402, SA1649

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.CustomNetflix;

public interface ICustomNetflixProfileService
{
    Task<IReadOnlyList<CustomNetflixProfileDto>> GetProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken);

    Task<CustomNetflixProfileDto> CreateProfileAsync(Guid jellyfinUserId, CustomNetflixCreateProfileRequest request, CancellationToken cancellationToken);

    Task<CustomNetflixProfileDto?> UpdateProfileAsync(Guid jellyfinUserId, Guid profileId, CustomNetflixUpdateProfileRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteProfileAsync(Guid jellyfinUserId, Guid profileId, CancellationToken cancellationToken);

    Task<CustomNetflixProfileDto?> GetOwnedProfileAsync(Guid jellyfinUserId, Guid profileId, CancellationToken cancellationToken);
}

public interface ICustomNetflixActiveProfileService
{
    bool IsEnabled { get; }

    Task<CustomNetflixActiveProfileDto> GetActiveProfileAsync(Guid jellyfinUserId, string? token, CancellationToken cancellationToken);

    Task<CustomNetflixActiveProfileDto?> SetActiveProfileAsync(Guid jellyfinUserId, string? token, Guid profileId, CancellationToken cancellationToken);

    Task<CustomNetflixProfileDto?> GetActiveProfileForWriteAsync(
        Guid jellyfinUserId,
        string? token,
        Guid requestedProfileId,
        CancellationToken cancellationToken);
}

public interface ICustomNetflixWatchProgressService
{
    Task<CustomNetflixWatchProgressDto?> ReportProgressAsync(Guid jellyfinUserId, Guid profileId, CustomNetflixWatchProgressReportRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomNetflixContinueWatchingItemDto>> GetContinueWatchingAsync(Guid jellyfinUserId, Guid profileId, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomNetflixWatchProgressDto>> GetProgressForItemsAsync(Guid jellyfinUserId, Guid profileId, IReadOnlyList<Guid> itemIds, CancellationToken cancellationToken);

    Task<CustomNetflixWatchProgressDto?> SetPlayedAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, bool played, CancellationToken cancellationToken);

    Task<bool> HideFromContinueWatchingAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, CancellationToken cancellationToken);
}

public interface ICustomNetflixWatchHistoryService
{
    Task<IReadOnlyList<CustomNetflixWatchHistoryItemDto>> GetHistoryAsync(Guid jellyfinUserId, Guid profileId, int limit, CancellationToken cancellationToken);
}

public interface ICustomNetflixMyListService
{
    Task<CustomNetflixMyListResponseDto?> GetMyListAsync(Guid jellyfinUserId, Guid profileId, int limit, CancellationToken cancellationToken);

    Task<CustomNetflixMyListStatusDto?> GetStatusAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<CustomNetflixMyListStatusDto?> AddAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, CancellationToken cancellationToken);

    Task<CustomNetflixMyListStatusDto?> RemoveAsync(Guid jellyfinUserId, Guid profileId, Guid itemId, CancellationToken cancellationToken);
}

public interface ICustomNetflixHomeRowsService
{
    Task<CustomNetflixHomeResponseDto> GetHomeAsync(Guid jellyfinUserId, Guid profileId, int itemLimit, CancellationToken cancellationToken);
}

public interface ICustomNetflixRecommendationService
{
    Task<CustomNetflixRecommendationsResponseDto?> GetRecommendationsAsync(
        Guid jellyfinUserId,
        Guid profileId,
        int limit,
        CancellationToken cancellationToken);
}

public interface ICustomNetflixItemDetailsService
{
    Task<CustomNetflixItemDetailsDto?> GetItemDetailsAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken);
}

public interface ICustomNetflixAutoplayService
{
    Task<CustomNetflixNextEpisodeDto> GetNextEpisodeAsync(Guid jellyfinUserId, Guid profileId, Guid currentItemId, CancellationToken cancellationToken);

    Task<CustomNetflixStillWatchingConfirmationDto?> ConfirmStillWatchingAsync(
        Guid jellyfinUserId,
        Guid profileId,
        CancellationToken cancellationToken);
}

public interface ICustomNetflixSegmentService
{
    Task<CustomNetflixMediaSegmentCoverageDto> GetCoverageAsync(CancellationToken cancellationToken);

    Task<CustomNetflixMediaSegmentsResponseDto?> ReplaceManualSegmentsAsync(Guid jellyfinUserId, Guid itemId, CustomNetflixManualMediaSegmentsRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteManualSegmentsAsync(Guid jellyfinUserId, Guid itemId, IReadOnlyList<string>? requestedTypes, CancellationToken cancellationToken);
}

public interface ICustomNetflixRankingService
{
    Task<CustomNetflixRankedItemsResponseDto> GetTrendingAsync(Guid jellyfinUserId, int limit, CancellationToken cancellationToken);

    Task<CustomNetflixRankedItemsResponseDto> GetTopTenAsync(Guid jellyfinUserId, int limit, CancellationToken cancellationToken);
}
