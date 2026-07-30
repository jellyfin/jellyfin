#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixActiveProfileService : ICustomNetflixActiveProfileService
{
    private static readonly TimeSpan ActiveProfileCacheTtl = TimeSpan.FromDays(30);

    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly ICustomNetflixCacheService _cache;

    public CustomNetflixActiveProfileService(
        ICustomNetflixProfileService profileService,
        ICustomNetflixRepository repository,
        ICustomNetflixCacheService cache)
    {
        _profileService = profileService;
        _repository = repository;
        _cache = cache;
    }

    public bool IsEnabled => _repository.IsEnabled;

    public async Task<CustomNetflixActiveProfileDto> GetActiveProfileAsync(Guid jellyfinUserId, string? token, CancellationToken cancellationToken)
    {
        var profiles = await _profileService.GetProfilesAsync(jellyfinUserId, cancellationToken).ConfigureAwait(false);
        var tokenHash = CustomNetflixNativePlaystateSyncPolicy.HashToken(token);
        var cacheKey = RedisKeyBuilder.ActiveProfile(jellyfinUserId, tokenHash);
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        if (Guid.TryParse(cached, out var cachedProfileId))
        {
            var cachedProfile = profiles.FirstOrDefault(profile => profile.Id.Equals(cachedProfileId));
            if (cachedProfile is not null)
            {
                return new CustomNetflixActiveProfileDto { ProfileId = cachedProfile.Id, Profile = cachedProfile };
            }
        }

        var persistedProfileId = await _repository.GetActiveProfileAsync(jellyfinUserId, tokenHash, cancellationToken).ConfigureAwait(false);
        var selectedProfile = CustomNetflixProfileSelectionPolicy.SelectProfile(profiles, persistedProfileId)
            ?? throw new InvalidOperationException("No CustomNetflix profile is available for the Jellyfin user.");

        await _repository.SetActiveProfileAsync(jellyfinUserId, tokenHash, selectedProfile.Id, cancellationToken).ConfigureAwait(false);
        await _cache.SetStringAsync(cacheKey, selectedProfile.Id.ToString("D", CultureInfo.InvariantCulture), ActiveProfileCacheTtl, cancellationToken).ConfigureAwait(false);

        return new CustomNetflixActiveProfileDto { ProfileId = selectedProfile.Id, Profile = selectedProfile };
    }

    public async Task<CustomNetflixActiveProfileDto?> SetActiveProfileAsync(Guid jellyfinUserId, string? token, Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        if (profile is null)
        {
            return null;
        }

        var tokenHash = CustomNetflixNativePlaystateSyncPolicy.HashToken(token);
        await _repository.SetActiveProfileAsync(jellyfinUserId, tokenHash, profileId, cancellationToken).ConfigureAwait(false);
        var cacheKey = RedisKeyBuilder.ActiveProfile(jellyfinUserId, tokenHash);
        await _cache.SetStringAsync(cacheKey, profileId.ToString("D", CultureInfo.InvariantCulture), ActiveProfileCacheTtl, cancellationToken).ConfigureAwait(false);
        return new CustomNetflixActiveProfileDto { ProfileId = profileId, Profile = profile };
    }

    public async Task<CustomNetflixProfileDto?> GetActiveProfileForWriteAsync(
        Guid jellyfinUserId,
        string? token,
        Guid requestedProfileId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var active = await GetActiveProfileAsync(jellyfinUserId, token, cancellationToken).ConfigureAwait(false);
        return active.ProfileId.Equals(requestedProfileId)
            && active.Profile?.Id.Equals(requestedProfileId) == true
            && active.Profile.JellyfinUserId.Equals(jellyfinUserId)
                ? active.Profile
                : null;
    }
}
