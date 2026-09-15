namespace Jellyfin.Data.Enums;

/// <summary>
/// What a company did for an item.
/// </summary>
/// <remarks>
/// Mirrors <c>Jellyfin.Database.Implementations.Entities.CompanyKindEntity</c>; the two are cast
/// into each other, so the members and their values have to stay in sync.
/// </remarks>
public enum CompanyKind
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
