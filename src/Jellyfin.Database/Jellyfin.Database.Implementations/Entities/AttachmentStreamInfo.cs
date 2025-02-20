using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Provides information about an Attachment to an <see cref="BaseItemEntity"/>.
/// </summary>
public class AttachmentStreamInfo
{
    /// <summary>
    /// Gets or Sets the <see cref="BaseItemEntity"/> reference.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the <see cref="BaseItemEntity"/> reference.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the index within the source file.
    /// </summary>
    public required int Index { get; set; }

    /// <summary>
    /// Gets or Sets the codec of the attachment.
    /// </summary>
    public required string Codec { get; set; }

    /// <summary>
    /// Gets or Sets the codec tag of the attachment.
    /// </summary>
    public string? CodecTag { get; set; }

    /// <summary>
    /// Gets or Sets the comment of the attachment.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or Sets the filename of the attachment.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Gets or Sets the attachments mimetype.
    /// </summary>
    public string? MimeType { get; set; }
}
