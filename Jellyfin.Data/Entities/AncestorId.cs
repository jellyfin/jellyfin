using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

#pragma warning disable CA1708 // Identifiers should differ by more than case
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class AncestorId
{
    public Guid Id { get; set; }

    public Guid ItemId { get; set; }

    public required BaseItemEntity Item { get; set; }

    public string? AncestorIdText { get; set; }
}
