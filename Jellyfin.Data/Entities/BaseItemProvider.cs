using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

public class BaseItemProvider
{
    public Guid ItemId { get; set; }
    public required BaseItem Item { get; set; }

    public string ProviderId { get; set; }
    public string ProviderValue { get; set; }
}
