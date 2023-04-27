using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// Group update without data.
/// </summary>
public abstract class GroupUpdate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupUpdate"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="type">The update type.</param>
    protected GroupUpdate(Guid groupId, GroupUpdateType type)
    {
        GroupId = groupId;
        Type = type;
    }

    /// <summary>
    /// Gets the group identifier.
    /// </summary>
    /// <value>The group identifier.</value>
    public Guid GroupId { get; }

    /// <summary>
    /// Gets the update type.
    /// </summary>
    /// <value>The update type.</value>
    public GroupUpdateType Type { get; }
}
