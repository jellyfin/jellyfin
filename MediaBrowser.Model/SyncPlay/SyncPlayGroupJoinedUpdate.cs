using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayGroupJoinedUpdate : GroupUpdate<GroupInfoDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupJoinedUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayGroupJoinedUpdate(Guid groupId, GroupInfoDto data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.GroupJoined)]
    public override GroupUpdateType Type => GroupUpdateType.GroupJoined;
}
