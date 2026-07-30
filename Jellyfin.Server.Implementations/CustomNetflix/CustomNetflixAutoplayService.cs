#pragma warning disable CS1591

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixAutoplayService : ICustomNetflixAutoplayService
{
    private const string StillWatchingReason = "still_watching_confirmation_required";
    private const string StillWatchingTitleKey = "customnetflix.autoplay.still_watching";

    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;

    public CustomNetflixAutoplayService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IDtoService dtoService)
    {
        _profileService = profileService;
        _repository = repository;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _dtoService = dtoService;
    }

    public async Task<CustomNetflixNextEpisodeDto> GetNextEpisodeAsync(Guid jellyfinUserId, Guid profileId, Guid currentItemId, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return None("profile_not_found");
        }

        if (!profile.Settings.AutoplayEnabled)
        {
            return None("disabled");
        }

        var currentEpisode = _libraryManager.GetItemById<Episode>(currentItemId, user);
        if (currentEpisode is null)
        {
            return None("not_episode");
        }

        var seriesId = currentEpisode.SeriesId;
        if (seriesId.Equals(Guid.Empty))
        {
            return None("series_not_found");
        }

        var currentSeasonNumber = currentEpisode.ParentIndexNumber ?? -1;
        var currentEpisodeNumber = currentEpisode.IndexNumber ?? -1;
        var query = new InternalItemsQuery(user)
        {
            Recursive = true,
            IsFolder = false,
            IncludeItemTypes = [BaseItemKind.Episode],
            AncestorIds = [seriesId],
            OrderBy = [(ItemSortBy.ParentIndexNumber, SortOrder.Ascending), (ItemSortBy.IndexNumber, SortOrder.Ascending)]
        };

        var candidates = _libraryManager.GetItemList(query)
            .OfType<Episode>()
            .Where(episode => !episode.Id.Equals(currentItemId))
            .Where(episode => IsAfter(episode, currentSeasonNumber, currentEpisodeNumber))
            .OrderBy(episode => episode.ParentIndexNumber ?? int.MaxValue)
            .ThenBy(episode => episode.IndexNumber ?? int.MaxValue)
            .ToArray();

        foreach (var candidate in candidates)
        {
            var progress = await _repository.GetProgressAsync(profileId, candidate.Id, cancellationToken).ConfigureAwait(false);
            if (progress?.Completed == true)
            {
                continue;
            }

            var currentProgress = await _repository.GetProgressAsync(profileId, currentItemId, cancellationToken).ConfigureAwait(false);
            var autoplayState = await _repository.TrackAutoplayAsync(
                profileId,
                currentItemId,
                currentProgress?.Completed == true,
                cancellationToken).ConfigureAwait(false);
            if (autoplayState.StillWatchingRequired)
            {
                return new CustomNetflixNextEpisodeDto
                {
                    HasNext = false,
                    DelaySeconds = 0,
                    Reason = StillWatchingReason,
                    ReasonKey = GetReasonKey(StillWatchingReason),
                    RequiresStillWatchingConfirmation = true,
                    TitleKey = StillWatchingTitleKey
                };
            }

            var resumePosition = progress is { Completed: false, PositionSeconds: >= 30 } ? progress.PositionSeconds : 0;
            var reason = resumePosition > 0 ? "resume_next_episode" : "next_episode";
            return new CustomNetflixNextEpisodeDto
            {
                HasNext = true,
                DelaySeconds = profile.Settings.AutoplayDelaySeconds,
                Item = _dtoService.GetBaseItemDto(candidate, CustomNetflixDtoMapper.CreateCardOptions(), user),
                ResumePositionSeconds = resumePosition,
                Reason = reason,
                ReasonKey = GetReasonKey(reason),
                TitleKey = GetTitleKey(reason)
            };
        }

        return None("end_of_series");
    }

    public async Task<CustomNetflixStillWatchingConfirmationDto?> ConfirmStillWatchingAsync(
        Guid jellyfinUserId,
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            return null;
        }

        var confirmedAt = await _repository.ConfirmStillWatchingAsync(profileId, cancellationToken).ConfigureAwait(false);
        return new CustomNetflixStillWatchingConfirmationDto
        {
            ProfileId = profileId,
            Required = false,
            ConfirmedAt = confirmedAt
        };
    }

    private static bool IsAfter(Episode episode, int currentSeasonNumber, int currentEpisodeNumber)
    {
        var seasonNumber = episode.ParentIndexNumber ?? -1;
        var episodeNumber = episode.IndexNumber ?? -1;
        return seasonNumber > currentSeasonNumber
            || (seasonNumber == currentSeasonNumber && episodeNumber > currentEpisodeNumber);
    }

    private static CustomNetflixNextEpisodeDto None(string reason)
        => new()
        {
            HasNext = false,
            DelaySeconds = 0,
            Reason = reason,
            ReasonKey = GetReasonKey(reason),
            TitleKey = GetTitleKey(reason)
        };

    private static string GetReasonKey(string reason)
        => $"customnetflix.autoplay.reason.{reason}";

    private static string GetTitleKey(string reason)
        => $"customnetflix.autoplay.title.{reason}";
}
