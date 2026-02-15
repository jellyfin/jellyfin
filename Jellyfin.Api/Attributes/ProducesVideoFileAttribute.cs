namespace Jellyfin.Api.Attributes;

/// <summary>
/// Produces file attribute of "video/*".
/// </summary>
public sealed class ProducesVideoFileAttribute : ProducesFileAttribute
{
    private const string ContentType = "video/*";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesVideoFileAttribute"/> class.
    /// </summary>
    public ProducesVideoFileAttribute()
        : base(ContentType)
    {
    }
}
