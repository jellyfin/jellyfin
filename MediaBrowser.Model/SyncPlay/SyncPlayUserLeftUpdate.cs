using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayUserLeftUpdate : GroupUpdate<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayUserLeftUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayUserLeftUpdate(Guid groupId, string data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.UserLeft)]
    public override GroupUpdateType Type => GroupUpdateType.UserLeft;
}
