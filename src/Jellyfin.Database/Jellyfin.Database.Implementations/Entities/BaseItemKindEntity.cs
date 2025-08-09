#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Database.Implementations.Entities;

public class BaseItemKindEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required int Kind { get; set; }

    public required string TypeName { get; set; }

    public string? Description { get; set; }
}
