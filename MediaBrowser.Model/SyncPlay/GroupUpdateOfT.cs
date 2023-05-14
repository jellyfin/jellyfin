#pragma warning disable SA1649

using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// Class GroupUpdate.
/// </summary>
/// <typeparam name="T">The type of the data of the message.</typeparam>
public class GroupUpdate<T> : GroupUpdate
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GroupUpdate{T}"/> class.
    /// </summary>
    /// <param name="groupId">The group identifier.</param>
    /// <param name="type">The update type.</param>
    /// <param name="data">The update data.</param>
    public GroupUpdate(Guid groupId, GroupUpdateType type, T data)
        : base(groupId)
    {
        Data = data;
        Type = type;
    }

    /// <summary>
    /// Gets the update data.
    /// </summary>
    /// <value>The update data.</value>
    public T Data { get; }
}
