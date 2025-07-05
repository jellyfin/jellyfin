using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayPlayQueueUpdate : GroupUpdate<PlayQueueUpdate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayPlayQueueUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayPlayQueueUpdate(Guid groupId, PlayQueueUpdate data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.PlayQueue)]
    public override GroupUpdateType Type => GroupUpdateType.PlayQueue;
}
