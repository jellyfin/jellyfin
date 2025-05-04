namespace Jellyfin.Api.Attributes;

/// <summary>
/// Produces file attribute of "image/*".
/// </summary>
public sealed class AcceptsImageFileAttribute : AcceptsFileAttribute
{
    private const string ContentType = "image/*";

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptsImageFileAttribute"/> class.
    /// </summary>
    public AcceptsImageFileAttribute()
        : base(ContentType)
    {
    }
}
