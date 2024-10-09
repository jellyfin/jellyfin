using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Defines an Entity that is modeled after an Enum.
/// </summary>
public abstract class EnumLikeTable
{
    /// <summary>
    /// Gets or Sets Numerical ID of this enumeratable.
    /// </summary>
    public required int Id { get; set; }
}
