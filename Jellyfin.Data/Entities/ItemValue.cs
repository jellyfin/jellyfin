using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

public class ItemValue
{
    public Guid ItemId { get; set; }
    public required BaseItem Item { get; set; }

    public required int Type { get; set; }
    public required string Value { get; set; }
    public required string CleanValue { get; set; }
}
