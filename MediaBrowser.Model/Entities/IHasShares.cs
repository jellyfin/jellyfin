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
    IReadOnlyList<PlaylistUserPermissions> Shares { get; set; }
}
