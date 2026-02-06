using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// SyncPlay group snapshot update.
/// </summary>
public class SyncPlayGroupSnapshotUpdate : GroupUpdate<SyncPlayGroupSnapshotDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupSnapshotUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="data">The snapshot data.</param>
    public SyncPlayGroupSnapshotUpdate(Guid groupId, SyncPlayGroupSnapshotDto data)
        : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.GroupSnapshot)]
    public override GroupUpdateType Type => GroupUpdateType.GroupSnapshot;
}
