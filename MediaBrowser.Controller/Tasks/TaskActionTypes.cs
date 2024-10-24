using System;

namespace MediaBrowser.Controller;

/// <summary>
/// The type of change for a base item.
/// </summary>
public enum TaskActionTypes
{
    /// <summary>
    /// Item as added.
    /// </summary>
    Added,

    /// <summary>
    /// Item changed.
    /// </summary>
    Changed,

    /// <summary>
    /// Item removed.
    /// </summary>
    Removed
}
