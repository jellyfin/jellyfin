using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class Chapter
{
    public Guid ItemId { get; set; }

    public required int ChapterIndex { get; set; }

    public required long StartPositionTicks { get; set; }

    public string? Name { get; set; }

    public string? ImagePath { get; set; }

    public DateTime? ImageDateModified { get; set; }
}
