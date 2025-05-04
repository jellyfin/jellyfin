namespace Jellyfin.Api.Attributes;

/// <summary>
/// Produces file attribute of "image/*".
/// </summary>
public sealed class ProducesAudioFileAttribute : ProducesFileAttribute
{
    private const string ContentType = "audio/*";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProducesAudioFileAttribute"/> class.
    /// </summary>
    public ProducesAudioFileAttribute()
        : base(ContentType)
    {
    }
}
