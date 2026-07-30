#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model.Configuration;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal sealed class CustomNetflixSegmentService : ICustomNetflixSegmentService
{
    private readonly ICustomNetflixRepository _repository;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly IMediaSegmentManager _mediaSegmentManager;

    public CustomNetflixSegmentService(
        ICustomNetflixRepository repository,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSegmentManager mediaSegmentManager)
    {
        _repository = repository;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSegmentManager = mediaSegmentManager;
    }

    public async Task<CustomNetflixMediaSegmentCoverageDto> GetCoverageAsync(CancellationToken cancellationToken)
    {
        var eligibleItems = _libraryManager.GetCount(new InternalItemsQuery
        {
            Recursive = true,
            IsFolder = false,
            IsVirtualItem = false,
            MediaTypes = [MediaType.Video],
            GroupByPresentationUniqueKey = false
        });
        var coveredItems = await _mediaSegmentManager.GetSegmentedItemCountsAsync(
            CustomNetflixSegmentCoveragePolicy.SegmentTypes,
            cancellationToken).ConfigureAwait(false);
        return CustomNetflixSegmentCoveragePolicy.Build(eligibleItems, coveredItems, DateTime.UtcNow);
    }

    public async Task<CustomNetflixMediaSegmentsResponseDto?> ReplaceManualSegmentsAsync(
        Guid jellyfinUserId,
        Guid itemId,
        CustomNetflixManualMediaSegmentsRequest request,
        CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(jellyfinUserId);
        var item = user is null ? null : _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null || !_mediaSegmentManager.IsTypeSupported(item))
        {
            return null;
        }

        var rows = CustomNetflixManualSegmentPolicy.BuildManualRows(itemId, request.Segments, DateTime.UtcNow);
        await _repository.ReplaceManualMediaSegmentsAsync(itemId, rows, cancellationToken).ConfigureAwait(false);
        await SynchronizeNativeSegmentsAsync(item, cancellationToken).ConfigureAwait(false);
        return new CustomNetflixMediaSegmentsResponseDto
        {
            ItemId = itemId,
            Segments = rows.Select(MapSegment).ToArray()
        };
    }

    public async Task<bool> DeleteManualSegmentsAsync(Guid jellyfinUserId, Guid itemId, IReadOnlyList<string>? requestedTypes, CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(jellyfinUserId);
        var item = user is null ? null : _libraryManager.GetItemById<BaseItem>(itemId, user);
        if (item is null || !_mediaSegmentManager.IsTypeSupported(item))
        {
            return false;
        }

        var normalizedTypes = CustomNetflixSegmentTypeMapper.NormalizeRequestedTypes(requestedTypes);
        await _repository.DeleteManualMediaSegmentsAsync(itemId, normalizedTypes, cancellationToken).ConfigureAwait(false);
        await SynchronizeNativeSegmentsAsync(item, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task SynchronizeNativeSegmentsAsync(
        BaseItem item,
        CancellationToken cancellationToken)
    {
        var libraryOptions = new LibraryOptions
        {
            DisabledMediaSegmentProviders = _mediaSegmentManager
                .GetSupportedProviders(item)
                .Where(provider => !string.Equals(
                    provider.Name,
                    CustomNetflixManualMediaSegmentProvider.ProviderName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(provider => provider.Name)
                .ToArray(),
            MediaSegmentProviderOrder = [CustomNetflixManualMediaSegmentProvider.ProviderName]
        };
        await _mediaSegmentManager
            .RunSegmentPluginProviders(item, libraryOptions, forceOverwrite: false, cancellationToken)
            .ConfigureAwait(false);
    }

    private static CustomNetflixMediaSegmentDto MapSegment(CustomMediaSegmentRow segment)
        => new()
        {
            Id = segment.Id,
            ItemId = segment.ItemId,
            Type = segment.SegmentType,
            StartSeconds = Math.Round(segment.StartSeconds, 3),
            EndSeconds = Math.Round(segment.EndSeconds, 3),
            Source = segment.Source
        };
}
