namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Enum TrickplayScanBehavior.
/// </summary>
public enum TrickplayScanBehavior
{
    /// <summary>
    /// Starts generation, only return once complete.
    /// </summary>
    Blocking,

    /// <summary>
    /// Start generation, return immediately.
    /// </summary>
    NonBlocking
}
