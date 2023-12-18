namespace MediaBrowser.Model.Entities;

/// <summary>
/// Interface for access to shares.
/// </summary>
public interface IHasShares
{
    /// <summary>
    /// Gets or sets the shares.
    /// </summary>
    Share[] Shares { get; set; }
}
