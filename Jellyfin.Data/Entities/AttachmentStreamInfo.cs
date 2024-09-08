using System;

namespace Jellyfin.Data.Entities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AttachmentStreamInfo
{
    public required Guid ItemId { get; set; }

    public required int Index { get; set; }

    public required string Codec { get; set; }

    public string? CodecTag { get; set; }

    public string? Comment { get; set; }

    public string? Filename { get; set; }

    public string? MimeType { get; set; }
}
