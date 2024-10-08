using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;
public class People
{
    public Guid ItemId { get; set; }
    public BaseItem Item { get; set; }

    public required string Name { get; set; }
    public string? Role { get; set; }
    public string? PersonType { get; set; }
    public int? SortOrder { get; set; }
    public int? ListOrder { get; set; }
}
