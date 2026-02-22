using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// Group update without data.
/// </summary>
/// <typeparam name="T">The type of the update data.</typeparam>
public abstract class GroupUpdate<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupUpdate{T}"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="data">The update data.</param>
    protected GroupUpdate(Guid groupId, T data)
    {
        GroupId = groupId;
        Data = data;
    }

    /// <summary>
    /// Gets the group identifier.
    /// </summary>
    /// <value>The group identifier.</value>
    public Guid GroupId { get; }

    /// <summary>
    /// Gets the update data.
    /// </summary>
    /// <value>The update data.</value>
    public T Data { get; }

    /// <summary>
    /// Gets the update type.
    /// </summary>
    /// <value>The update type.</value>
    public abstract GroupUpdateType Type { get; }
}
