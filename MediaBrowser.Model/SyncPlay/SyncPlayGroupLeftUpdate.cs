using System;
using System.ComponentModel;

namespace MediaBrowser.Model.SyncPlay;

/// <inheritdoc />
public class SyncPlayGroupLeftUpdate : GroupUpdate<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupLeftUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The groupId.</param>
    /// <param name="data">The data.</param>
    public SyncPlayGroupLeftUpdate(Guid groupId, string data) : base(groupId, data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(GroupUpdateType.GroupLeft)]
    public override GroupUpdateType Type => GroupUpdateType.GroupLeft;
}
