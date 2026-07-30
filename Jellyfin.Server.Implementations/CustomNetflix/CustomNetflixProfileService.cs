#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixProfileService : ICustomNetflixProfileService
{
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly int _maxProfilesPerAccount;
    private readonly ConcurrentDictionary<Guid, Lazy<Task<IReadOnlyList<CustomNetflixProfileDto>>>> _profilesByUser = new();
    private readonly ConcurrentDictionary<(Guid UserId, Guid ProfileId), Lazy<Task<CustomNetflixProfileDto?>>> _ownedProfiles = new();

    public CustomNetflixProfileService(
        ICustomNetflixRepository repository,
        IUserManager userManager,
        IOptions<CustomNetflixOptions> options)
    {
        _repository = repository;
        _userManager = userManager;
        _maxProfilesPerAccount = Math.Clamp(options.Value.MaxProfilesPerAccount, 1, 20);
    }

    public Task<IReadOnlyList<CustomNetflixProfileDto>> GetProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken)
        => _profilesByUser.GetOrAdd(
            jellyfinUserId,
            _ => new Lazy<Task<IReadOnlyList<CustomNetflixProfileDto>>>(
                () => LoadProfilesAsync(jellyfinUserId, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private async Task<IReadOnlyList<CustomNetflixProfileDto>> LoadProfilesAsync(Guid jellyfinUserId, CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(jellyfinUserId)
            ?? throw new ArgumentException("The Jellyfin user does not exist.", nameof(jellyfinUserId));

        var profiles = await _repository.GetProfilesAsync(jellyfinUserId, cancellationToken).ConfigureAwait(false);
        if (profiles.Count == 0)
        {
            var defaultProfile = await _repository.CreateProfileAsync(
                jellyfinUserId,
                string.IsNullOrWhiteSpace(user.Username) ? "Default" : user.Username,
                null,
                false,
                true,
                _maxProfilesPerAccount,
                cancellationToken).ConfigureAwait(false);
            profiles = [defaultProfile];
        }

        var mappedProfiles = profiles.Select(MapProfile).ToArray();
        foreach (var profile in mappedProfiles)
        {
            CacheOwnedProfile(jellyfinUserId, profile, overwrite: false);
        }

        return mappedProfiles;
    }

    public async Task<CustomNetflixProfileDto> CreateProfileAsync(Guid jellyfinUserId, CustomNetflixCreateProfileRequest request, CancellationToken cancellationToken)
    {
        if (_userManager.GetUserById(jellyfinUserId) is null)
        {
            throw new ArgumentException("The Jellyfin user does not exist.", nameof(jellyfinUserId));
        }

        if (request.IsChild)
        {
            throw ChildProfilesUnavailable();
        }

        var profile = await _repository.CreateProfileAsync(
            jellyfinUserId,
            NormalizeProfileName(request.Name),
            request.AvatarId,
            false,
            false,
            _maxProfilesPerAccount,
            cancellationToken).ConfigureAwait(false);

        var mappedProfile = MapProfile(profile);
        InvalidateUser(jellyfinUserId);
        CacheOwnedProfile(jellyfinUserId, mappedProfile, overwrite: true);
        return mappedProfile;
    }

    public async Task<CustomNetflixProfileDto?> UpdateProfileAsync(Guid jellyfinUserId, Guid profileId, CustomNetflixUpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (profile is null || !profile.JellyfinUserId.Equals(jellyfinUserId))
        {
            return null;
        }

        if (request.IsChild == true)
        {
            throw ChildProfilesUnavailable();
        }

        ProfileSettingsRow? settings = null;
        if (request.Settings is not null)
        {
            settings = new ProfileSettingsRow(
                profileId,
                request.Settings.AutoplayEnabled,
                Math.Clamp(request.Settings.AutoplayDelaySeconds, 0, 60),
                request.Settings.SkipIntroEnabled,
                request.Settings.SkipRecapEnabled);
        }

        PlaybackPreferencesRow? playbackPreferences = null;
        if (request.PlaybackPreferences is not null)
        {
            playbackPreferences = new PlaybackPreferencesRow(
                profileId,
                request.PlaybackPreferences.PreferDirectPlay,
                request.PlaybackPreferences.AllowContainerRemuxing,
                request.PlaybackPreferences.AllowVideoTranscoding,
                request.PlaybackPreferences.AllowAudioTranscoding,
                request.PlaybackPreferences.PreferHardwareTranscoding,
                NormalizeMaxStreamingBitrate(request.PlaybackPreferences.MaxStreamingBitrate),
                NormalizeLanguage(request.PlaybackPreferences.PreferredAudioLanguage, nameof(request.PlaybackPreferences.PreferredAudioLanguage)),
                NormalizeLanguage(request.PlaybackPreferences.PreferredSubtitleLanguage, nameof(request.PlaybackPreferences.PreferredSubtitleLanguage)),
                request.PlaybackPreferences.SubtitlesEnabled,
                request.PlaybackPreferences.AudioDescriptionEnabled,
                request.PlaybackPreferences.ClosedCaptionsEnabled,
                request.PlaybackPreferences.SkipCreditsEnabled);
        }

        var updated = await _repository.UpdateProfileAsync(
            profileId,
            request.Name is null ? null : NormalizeProfileName(request.Name),
            request.AvatarId,
            settings,
            playbackPreferences,
            cancellationToken).ConfigureAwait(false);

        if (updated is null)
        {
            return null;
        }

        var mappedProfile = MapProfile(updated);
        InvalidateUser(jellyfinUserId);
        CacheOwnedProfile(jellyfinUserId, mappedProfile, overwrite: true);
        return mappedProfile;
    }

    public async Task<bool> DeleteProfileAsync(Guid jellyfinUserId, Guid profileId, CancellationToken cancellationToken)
    {
        var profiles = await _repository.GetProfilesAsync(jellyfinUserId, cancellationToken).ConfigureAwait(false);
        if (profiles.All(profile => !profile.Id.Equals(profileId)))
        {
            return false;
        }

        if (profiles.Count <= 1)
        {
            throw new InvalidOperationException("Cannot delete the last profile for a Jellyfin user.");
        }

        var deleted = await _repository.SoftDeleteProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
        if (deleted)
        {
            InvalidateUser(jellyfinUserId);
        }

        return deleted;
    }

    public Task<CustomNetflixProfileDto?> GetOwnedProfileAsync(Guid jellyfinUserId, Guid profileId, CancellationToken cancellationToken)
        => _ownedProfiles.GetOrAdd(
            (jellyfinUserId, profileId),
            _ => new Lazy<Task<CustomNetflixProfileDto?>>(
                () => LoadOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    private async Task<CustomNetflixProfileDto?> LoadOwnedProfileAsync(Guid jellyfinUserId, Guid profileId, CancellationToken cancellationToken)
    {
        var profile = await _repository.GetProfileAsync(profileId, cancellationToken).ConfigureAwait(false);
        return profile is null || !profile.JellyfinUserId.Equals(jellyfinUserId) ? null : MapProfile(profile);
    }

    private void CacheOwnedProfile(Guid jellyfinUserId, CustomNetflixProfileDto profile, bool overwrite)
    {
        var key = (jellyfinUserId, profile.Id);
        var value = new Lazy<Task<CustomNetflixProfileDto?>>(
            () => Task.FromResult<CustomNetflixProfileDto?>(profile),
            LazyThreadSafetyMode.ExecutionAndPublication);
        if (overwrite)
        {
            _ownedProfiles[key] = value;
        }
        else
        {
            _ownedProfiles.TryAdd(key, value);
        }
    }

    private void InvalidateUser(Guid jellyfinUserId)
    {
        _profilesByUser.TryRemove(jellyfinUserId, out _);
        foreach (var key in _ownedProfiles.Keys.Where(key => key.UserId.Equals(jellyfinUserId)))
        {
            _ownedProfiles.TryRemove(key, out _);
        }
    }

    private static CustomNetflixProfileDto MapProfile(ProfileRow profile)
        => new()
        {
            Id = profile.Id,
            JellyfinUserId = profile.JellyfinUserId,
            Name = profile.Name,
            AvatarId = profile.AvatarId,
            IsDefault = profile.IsDefault,
            IsChild = profile.IsChild,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            Settings = new CustomNetflixProfileSettingsDto
            {
                AutoplayEnabled = profile.Settings.AutoplayEnabled,
                AutoplayDelaySeconds = profile.Settings.AutoplayDelaySeconds,
                SkipIntroEnabled = profile.Settings.SkipIntroEnabled,
                SkipRecapEnabled = profile.Settings.SkipRecapEnabled
            },
            PlaybackPreferences = new CustomNetflixPlaybackPreferencesDto
            {
                PreferDirectPlay = profile.PlaybackPreferences.PreferDirectPlay,
                AllowContainerRemuxing = profile.PlaybackPreferences.AllowContainerRemuxing,
                AllowVideoTranscoding = profile.PlaybackPreferences.AllowVideoTranscoding,
                AllowAudioTranscoding = profile.PlaybackPreferences.AllowAudioTranscoding,
                PreferHardwareTranscoding = profile.PlaybackPreferences.PreferHardwareTranscoding,
                MaxStreamingBitrate = profile.PlaybackPreferences.MaxStreamingBitrate,
                PreferredAudioLanguage = profile.PlaybackPreferences.PreferredAudioLanguage,
                PreferredSubtitleLanguage = profile.PlaybackPreferences.PreferredSubtitleLanguage,
                SubtitlesEnabled = profile.PlaybackPreferences.SubtitlesEnabled,
                AudioDescriptionEnabled = profile.PlaybackPreferences.AudioDescriptionEnabled,
                ClosedCaptionsEnabled = profile.PlaybackPreferences.ClosedCaptionsEnabled,
                SkipCreditsEnabled = profile.PlaybackPreferences.SkipCreditsEnabled
            }
        };

    private static string NormalizeProfileName(string name)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > 64)
        {
            throw new ArgumentException("Profile name must be between 1 and 64 characters.", nameof(name));
        }

        return name;
    }

    private static string? NormalizeLanguage(string? language, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        language = language.Trim();
        if (language.Length > 35)
        {
            throw new ArgumentException("Language tags cannot exceed 35 characters.", parameterName);
        }

        return language;
    }

    private static int? NormalizeMaxStreamingBitrate(int? bitrate)
    {
        if (bitrate is < 1_000_000 or > 500_000_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bitrate),
                "Maximum streaming bitrate must be between 1,000,000 and 500,000,000 bits per second.");
        }

        return bitrate;
    }

    private static ArgumentException ChildProfilesUnavailable()
        => new(
            "Child profiles are unavailable because native Jellyfin item and stream routes cannot enforce a CustomNetflix profile context.",
            "IsChild");
}
