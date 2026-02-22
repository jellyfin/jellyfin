using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayStateUpdate : GroupUpdate<GroupStateUpdate>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayStateUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayStateUpdate(Guid groupId, GroupStateUpdate data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.StateUpdate)]
    public override GroupUpdateType Type => GroupUpdateType.StateUpdate;
}
