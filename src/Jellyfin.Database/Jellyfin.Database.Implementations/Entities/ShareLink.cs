using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// An entity representing a scoped media share link.
    /// </summary>
    public class ShareLink
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShareLink"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="id">The Paseto token id (jti) of the share link.</param>
        /// <param name="userId">The id of the user that created the share link.</param>
        /// <param name="itemId">The id of the media item the share link grants access to.</param>
        /// <param name="createdAt">The creation timestamp in unix seconds.</param>
        public ShareLink(string id, Guid userId, Guid itemId, long createdAt)
        {
            ArgumentException.ThrowIfNullOrEmpty(id);

            Id = id;
            UserId = userId;
            ItemId = itemId;
            CreatedAt = createdAt;
            IsRevoked = false;
        }

        /// <summary>
        /// Gets or sets the Paseto token id (jti) of the share link.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 32, Identity.
        /// </remarks>
        [MaxLength(32)]
        [StringLength(32)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the id of the user that created the share link.
        /// </summary>
        /// <remarks>
        /// Required, Foreign key to <see cref="User"/>, Indexed.
        /// </remarks>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the id of the media item the share link grants access to.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp in unix seconds.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public long CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the first access in unix seconds, or <c>null</c> if the link has not been opened yet.
        /// </summary>
        public long? FirstAccessedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the share link has been revoked by its owner.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Gets or sets the absolute expiration timestamp in unix seconds, or <c>null</c> when the link expires by duration instead.
        /// </summary>
        public long? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the lifetime in seconds starting from the first access, or <c>null</c> when the link expires absolutely.
        /// </summary>
        public int? DurationSeconds { get; set; }
    }
}
