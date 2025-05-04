namespace Jellyfin.Api.Attributes;

/// <summary>
/// Produces file attribute of "image/*".
/// </summary>
public sealed class ProducesPlaylistFileAttribute : ProducesFileAttribute
{
    private const string ContentType = "application/x-mpegURL";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesPlaylistFileAttribute"/> class.
    /// </summary>
    public ProducesPlaylistFileAttribute()
        : base(ContentType)
    {
    }
}
