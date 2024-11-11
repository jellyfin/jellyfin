using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Provides <see cref="BaseItemEntity"/> and <see cref="User"/> related data.
/// </summary>
public class UserData
{
    /// <summary>
    /// Gets or sets the custom data key.
    /// </summary>
    /// <value>The rating.</value>
    public required string CustomDataKey { get; set; }

    /// <summary>
    /// Gets or sets the users 0-10 rating.
    /// </summary>
    /// <value>The rating.</value>
    public double? Rating { get; set; }

    /// <summary>
    /// Gets or sets the playback position ticks.
    /// </summary>
    /// <value>The playback position ticks.</value>
    public long PlaybackPositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the play count.
    /// </summary>
    /// <value>The play count.</value>
    public int PlayCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is favorite.
    /// </summary>
    /// <value><c>true</c> if this instance is favorite; otherwise, <c>false</c>.</value>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// Gets or sets the last played date.
    /// </summary>
    /// <value>The last played date.</value>
    public DateTime? LastPlayedDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="UserData" /> is played.
    /// </summary>
    /// <value><c>true</c> if played; otherwise, <c>false</c>.</value>
    public bool Played { get; set; }

    /// <summary>
    /// Gets or sets the index of the audio stream.
    /// </summary>
    /// <value>The index of the audio stream.</value>
    public int? AudioStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the index of the subtitle stream.
    /// </summary>
    /// <value>The index of the subtitle stream.</value>
    public int? SubtitleStreamIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item is liked or not.
    /// This should never be serialized.
    /// </summary>
    /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
    public bool? Likes { get; set; }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the BaseItem.
    /// </summary>
    public required BaseItemEntity? Item { get; set; }

    /// <summary>
    /// Gets or Sets the UserId.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Gets or Sets the User.
    /// </summary>
    public required User? User { get; set; }
}
