#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixFeedbackService : ICustomNetflixFeedbackService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    public CustomNetflixFeedbackService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _profileService = profileService;
        _repository = repository;
        _cache = cache;
        _userManager = userManager;
        _libraryManager = libraryManager;
    }

    public async Task<CustomNetflixItemFeedbackDto?> GetAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var row = await _repository.GetItemFeedbackAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        return Map(profileId, itemId, row);
    }

    public async Task<CustomNetflixItemFeedbackDto?> SetAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CustomNetflixItemFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var feedback = CustomNetflixFeedbackPolicy.Normalize(request.Feedback);
        var row = await _repository.UpsertItemFeedbackAsync(
            profileId,
            itemId,
            feedback,
            cancellationToken).ConfigureAwait(false);
        await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        return Map(profileId, itemId, row);
    }

    public async Task<CustomNetflixItemFeedbackDto?> ClearAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync(jellyfinUserId, profileId, itemId, cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        if (await _repository.DeleteItemFeedbackAsync(profileId, itemId, cancellationToken).ConfigureAwait(false))
        {
            await InvalidateHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        }

        return Map(profileId, itemId, null);
    }

    private async Task<bool> CanAccessAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(
            jellyfinUserId,
            profileId,
            cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        return profile is not null
            && user is not null
            && _libraryManager.GetItemById<BaseItem>(itemId, user) is not null;
    }

    private async Task InvalidateHomeSnapshotsAsync(Guid profileId, CancellationToken cancellationToken)
    {
        await _repository.DeleteHomeSnapshotsAsync(profileId, cancellationToken).ConfigureAwait(false);
        await _cache.RemoveAsync(CustomNetflixHomeSnapshots.CacheKeys(profileId), cancellationToken).ConfigureAwait(false);
    }

    private static CustomNetflixItemFeedbackDto Map(Guid profileId, Guid itemId, ItemFeedbackRow? row)
        => new()
        {
            ProfileId = profileId,
            ItemId = itemId,
            Feedback = row?.Feedback,
            UpdatedAt = row?.UpdatedAt
        };
}
