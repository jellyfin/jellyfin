using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// The Chapter entity.
/// </summary>
public class Chapter
{
    /// <summary>
    /// Gets or Sets the <see cref="BaseItemEntity"/> reference id.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the <see cref="BaseItemEntity"/> reference.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the chapters index in Item.
    /// </summary>
    public required int ChapterIndex { get; set; }

    /// <summary>
    /// Gets or Sets the position within the source file.
    /// </summary>
    public required long StartPositionTicks { get; set; }

    /// <summary>
    /// Gets or Sets the common name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or Sets the image path.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// Gets or Sets the time the image was last modified.
    /// </summary>
    public DateTime? ImageDateModified { get; set; }
}
