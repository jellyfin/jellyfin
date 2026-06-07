using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Manages video version groups (a primary version plus its linked alternate versions).
/// </summary>
public interface IVideoVersionManager
{
    /// <summary>
    /// Merges the given videos into a single version group: one video is selected as the primary
    /// version and the remaining ones are linked onto it as alternate versions. Videos that are
    /// already local (file-based) alternates of the selected primary are left untouched.
    /// </summary>
    /// <param name="videos">The videos to merge. Needs at least two entries to have an effect.</param>
    /// <param name="autoGrouped">Whether the merge is performed automatically by the library scan
    /// (<see cref="LinkedChildType.AutoLinkedAlternateVersion"/>) rather than by the user
    /// (<see cref="LinkedChildType.LinkedAlternateVersion"/>).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The primary version of the merged group, or <c>null</c> when fewer than two videos were supplied.</returns>
    Task<Video?> MergeVersionsAsync(IReadOnlyList<Video> videos, bool autoGrouped, CancellationToken cancellationToken);

    /// <summary>
    /// Removes an alternate version from its version group: clears its primary version reference
    /// and drops the corresponding link from the primary.
    /// </summary>
    /// <param name="alternate">The alternate version to unlink.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the unlink operation.</returns>
    Task UnlinkVersionAsync(Video alternate, CancellationToken cancellationToken);

    /// <summary>
    /// Removes the link from <paramref name="primary"/> to the given alternate version: unlinks
    /// the alternate when it still points back at the primary, otherwise just drops the dangling
    /// link entry (e.g. when the alternate no longer exists).
    /// </summary>
    /// <param name="primary">The primary version owning the link.</param>
    /// <param name="alternateId">The id of the linked alternate version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the removal operation.</returns>
    Task RemoveVersionLinkAsync(Video primary, Guid alternateId, CancellationToken cancellationToken);

    /// <summary>
    /// Splits a version group apart: clears the primary version reference and the alternate
    /// version links of every member. When the supplied video is an alternate, the group of its
    /// primary version is split.
    /// </summary>
    /// <param name="video">A member of the version group to split.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><c>true</c> when the group was split; <c>false</c> when its primary version could not be resolved.</returns>
    Task<bool> SplitVersionsAsync(Video video, CancellationToken cancellationToken);
}
