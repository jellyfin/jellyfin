using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Manages video version groups.
/// </summary>
public class VideoVersionManager : IVideoVersionManager
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<VideoVersionManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VideoVersionManager"/> class.
    /// </summary>
    /// <param name="libraryManager">The library manager.</param>
    /// <param name="logger">The logger.</param>
    public VideoVersionManager(ILibraryManager libraryManager, ILogger<VideoVersionManager> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Video?> MergeVersionsAsync(IReadOnlyList<Video> videos, bool autoGrouped, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(videos);

        if (videos.Count < 2)
        {
            return null;
        }

        var items = videos.OrderBy(i => i.Id).ToList();
        var linkType = autoGrouped ? LinkedChildType.AutoLinkedAlternateVersion : LinkedChildType.LinkedAlternateVersion;

        // Prefer an existing primary that already has multiple sources, otherwise pick the
        // plain video file with the highest resolution.
        var primaryVersion = items.FirstOrDefault(i => i.MediaSourceCount > 1 && !i.PrimaryVersionId.HasValue);
        primaryVersion ??= items
            .OrderBy(i =>
            {
                if (i.Video3DFormat.HasValue || i.VideoType != VideoType.VideoFile)
                {
                    return 1;
                }

                return 0;
            })
            .ThenByDescending(i => i.GetDefaultVideoStream()?.Width ?? 0)
            .First();

        _logger.LogDebug(
            "Merging {Count} videos into version group with primary {PrimaryName} ({PrimaryId})",
            items.Count,
            primaryVersion.Name,
            primaryVersion.Id);

        var alternateVersionsOfPrimary = primaryVersion.LinkedAlternateVersions.ToList();
        var localAlternateIds = _libraryManager.GetLocalAlternateVersionIds(primaryVersion).ToHashSet();

        foreach (var item in items.Where(i => !i.Id.Equals(primaryVersion.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (localAlternateIds.Contains(item.Id))
            {
                // Already a local (file-based) alternate of the primary; linking it would
                // wrongly mark the group as user-merged (splittable).
                continue;
            }

            item.SetPrimaryVersionId(primaryVersion.Id);

            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

            // Re-route any playlist/collection references from this item to the primary
            await _libraryManager.RerouteLinkedChildReferencesAsync(item.Id, primaryVersion.Id).ConfigureAwait(false);

            if (!alternateVersionsOfPrimary.Any(i => i.ItemId.HasValue && i.ItemId.Value.Equals(item.Id)))
            {
                alternateVersionsOfPrimary.Add(new LinkedChild
                {
                    ItemId = item.Id,
                    Type = linkType
                });
            }

            foreach (var linkedItem in item.LinkedAlternateVersions)
            {
                if (linkedItem.ItemId.HasValue && !alternateVersionsOfPrimary.Any(i => i.ItemId.HasValue && i.ItemId.Value.Equals(linkedItem.ItemId.Value)))
                {
                    alternateVersionsOfPrimary.Add(linkedItem);
                }
            }

            if (item.LinkedAlternateVersions.Length > 0)
            {
                item.LinkedAlternateVersions = [];
                await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            }
        }

        primaryVersion.LinkedAlternateVersions = [.. alternateVersionsOfPrimary];
        await primaryVersion.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

        return primaryVersion;
    }

    /// <inheritdoc />
    public async Task UnlinkVersionAsync(Video alternate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(alternate);

        var primaryId = alternate.PrimaryVersionId;

        alternate.SetPrimaryVersionId(null);
        await alternate.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

        if (primaryId.HasValue && _libraryManager.GetItemById(primaryId.Value) is Video primary)
        {
            primary.LinkedAlternateVersions = [.. primary.LinkedAlternateVersions.Where(l => !l.ItemId.HasValue || !l.ItemId.Value.Equals(alternate.Id))];
            await primary.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task RemoveVersionLinkAsync(Video primary, Guid alternateId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(primary);

        if (_libraryManager.GetItemById(alternateId) is Video alternate
            && alternate.PrimaryVersionId.HasValue
            && alternate.PrimaryVersionId.Value.Equals(primary.Id))
        {
            await UnlinkVersionAsync(alternate, cancellationToken).ConfigureAwait(false);
            return;
        }

        // The alternate no longer exists or does not point back; just drop the dangling link entry.
        primary.LinkedAlternateVersions = [.. primary.LinkedAlternateVersions.Where(l => !l.ItemId.HasValue || !l.ItemId.Value.Equals(alternateId))];
        await primary.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReassignAlternatesAsync(Video oldPrimary, Video newPrimary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(oldPrimary);
        ArgumentNullException.ThrowIfNull(newPrimary);

        var localAlternateIds = _libraryManager.GetLocalAlternateVersionIds(oldPrimary).ToHashSet();
        var allAlternateIds = localAlternateIds
            .Concat(_libraryManager.GetLinkedAlternateVersions(oldPrimary).Select(v => v.Id))
            .Distinct()
            .ToList();

        foreach (var alternateId in allAlternateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_libraryManager.GetItemById(alternateId) is Video alternate && !alternate.Id.Equals(newPrimary.Id))
            {
                alternate.SetPrimaryVersionId(newPrimary.Id);

                // Only local (file-based) alternates are owned by their primary; linked alternates
                // remain independent items.
                alternate.OwnerId = localAlternateIds.Contains(alternate.Id) ? newPrimary.Id : Guid.Empty;
                await alternate.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
            }
        }

        // Re-route playlist/collection references from the old primary to the new one.
        await _libraryManager.RerouteLinkedChildReferencesAsync(oldPrimary.Id, newPrimary.Id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> SplitVersionsAsync(Video video, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(video);

        // When an alternate is supplied, split the group of its primary version.
        var primary = video;
        if (video.LinkedAlternateVersions.Length == 0 && video.PrimaryVersionId.HasValue)
        {
            primary = _libraryManager.GetItemById(video.PrimaryVersionId.Value) as Video;
            if (primary is null)
            {
                return false;
            }
        }

        _logger.LogDebug(
            "Splitting version group of {PrimaryName} ({PrimaryId})",
            primary.Name,
            primary.Id);

        foreach (var link in _libraryManager.GetLinkedAlternateVersions(primary))
        {
            cancellationToken.ThrowIfCancellationRequested();

            link.SetPrimaryVersionId(null);
            link.LinkedAlternateVersions = [];

            await link.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        }

        primary.LinkedAlternateVersions = [];
        primary.SetPrimaryVersionId(null);
        await primary.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);

        return true;
    }
}
