using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayUserJoinedUpdate : GroupUpdate<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayUserJoinedUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayUserJoinedUpdate(Guid groupId, string data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.UserJoined)]
    public override GroupUpdateType Type => GroupUpdateType.UserJoined;
}
