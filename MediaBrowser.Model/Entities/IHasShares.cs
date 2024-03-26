using System.Collections.Generic;

namespace MediaBrowser.Model.Entities;

/// <summary>
/// Interface for access to shares.
/// </summary>
public interface IHasShares
{
    /// <summary>
    /// Gets or sets the shares.
    /// </summary>
    IReadOnlyList<Share> Shares { get; set; }
}
