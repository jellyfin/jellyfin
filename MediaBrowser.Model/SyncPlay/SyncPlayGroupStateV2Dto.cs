using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// V2 authoritative SyncPlay group state payload.
/// </summary>
public class SyncPlayGroupStateV2Dto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupStateV2Dto"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="revision">The monotonic server revision for this group state.</param>
    /// <param name="snapshot">The latest full snapshot.</param>
    /// <param name="serverUtcNow">Current server UTC time.</param>
    public SyncPlayGroupStateV2Dto(Guid groupId, long revision, SyncPlayGroupSnapshotDto snapshot, DateTime serverUtcNow)
    {
        GroupId = groupId;
        Revision = revision;
        Snapshot = snapshot;
        ServerUtcNow = serverUtcNow;
    }

    /// <summary>
    /// Gets the group identifier.
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// Gets the monotonic revision for this group state.
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// Gets the latest full snapshot.
    /// </summary>
    public SyncPlayGroupSnapshotDto Snapshot { get; }

    /// <summary>
    /// Gets the current server UTC time.
    /// </summary>
    public DateTime ServerUtcNow { get; }
}
