namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Specifies when embedded metadata (from media file tags) should be used.
/// </summary>
public enum EmbeddedMetadataPriority
{
    /// <summary>
    /// Never use embedded metadata titles. Defer to online providers or filename.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Use embedded metadata titles for Home Videos libraries only (default).
    /// </summary>
    ForHomeVideosOnly = 1,

    /// <summary>
    /// Always prefer embedded metadata titles over online providers.
    /// </summary>
    Always = 2
}
