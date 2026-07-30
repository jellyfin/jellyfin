#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixItemDetailsService : ICustomNetflixItemDetailsService
{
    private readonly ICustomNetflixProfileService _profileService;
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IDtoService _dtoService;

    public CustomNetflixItemDetailsService(
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

    public async Task<CustomNetflixItemDetailsDto?> GetItemDetailsAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetOwnedProfileAsync(jellyfinUserId, profileId, cancellationToken).ConfigureAwait(false);
        var user = _userManager.GetUserById(jellyfinUserId);
        if (profile is null || user is null)
        {
            return null;
        }

        var item = _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null)
        {
            return null;
        }

        var progress = await _repository.GetProgressAsync(profileId, itemId, cancellationToken).ConfigureAwait(false);
        return new CustomNetflixItemDetailsDto
        {
            ProfileId = profileId,
            GeneratedAt = DateTime.UtcNow,
            Item = _dtoService.GetBaseItemDto(item, CustomNetflixDtoMapper.CreateCardOptions(includeTrickplay: true), user),
            Progress = progress is null ? null : CustomNetflixDtoMapper.MapProgress(progress)
        };
    }
}
