namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// The kind of company a <see cref="CompanyEntity"/> is.
/// </summary>
/// <remarks>
/// Mirrors <c>Jellyfin.Data.Enums.CompanyKind</c>; the two are cast into each other, so the
/// members and their values have to stay in sync.
/// </remarks>
public enum CompanyKindEntity
{
    /// <summary>
    /// A company that produced the item.
    /// </summary>
    Studio = 0,

    /// <summary>
    /// A network that aired the item.
    /// </summary>
    Network = 1,

    /// <summary>
    /// A label that released the item.
    /// </summary>
    Label = 2,

    /// <summary>
    /// A company that published the item.
    /// </summary>
    Publisher = 3
}
