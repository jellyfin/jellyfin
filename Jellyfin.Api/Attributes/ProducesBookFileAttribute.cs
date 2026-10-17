namespace Jellyfin.Api.Attributes;

/// <summary>
/// Produces file attributes for streamable book content types.
/// </summary>
public sealed class ProducesBookFileAttribute : ProducesFileAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesBookFileAttribute"/> class.
    /// </summary>
    public ProducesBookFileAttribute() : base("application/zip")
    {
    }
}
