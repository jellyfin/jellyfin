using System;

namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class GroupUpdateDto.
    /// </summary>
    /// <typeparam name="T">The type of the data of the message.</typeparam>
    public class GroupUpdateDto<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupUpdateDto{T}"/> class.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="type">The update type.</param>
        /// <param name="data">The update data.</param>
        public GroupUpdateDto(Guid groupId, GroupUpdateType type, T data)
        {
            GroupId = groupId;
            Type = type;
            Data = data;
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

        /// <summary>
        /// Gets the update data.
        /// </summary>
        /// <value>The update data.</value>
        public T Data { get; }
    }
}
