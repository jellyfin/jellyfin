#pragma warning disable CA2227

using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Enum TrailerTypes.
/// </summary>
public class BaseItemImageInfo
{
    /// <summary>
    /// Gets or Sets.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or Sets the path to the original image.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or Sets the time the image was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// Gets or Sets the imagetype.
    /// </summary>
    public ImageInfoImageType ImageType { get; set; }

    /// <summary>
    /// Gets or Sets the width of the original image.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or Sets the height of the original image.
    /// </summary>
    public int Height { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
    /// <summary>
    /// Gets or Sets the blurhash.
    /// </summary>
    public byte[]? Blurhash { get; set; }
#pragma warning restore CA1819

    /// <summary>
    /// Gets or Sets the reference id to the BaseItem.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the referenced Item.
    /// </summary>
    public required BaseItemEntity Item { get; set; }
}
