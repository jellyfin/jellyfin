using System;

namespace DvdLib.Enums;

/// <summary>
/// The command.
/// </summary>
[Flags]
public enum Command
{
    /// <summary>
    /// None.
    /// </summary>
    None = 0,

    /// <summary>
    /// None.
    /// </summary>
    Exists = 1,

    /// <summary>
    /// Only in pre/post.
    /// </summary>
    Button = 2,

    /// <summary>
    /// Only in cell.
    /// </summary>
    PrePost = 4,

    /// <summary>
    /// Only in Button.
    /// </summary>
    Cell = 8,
}
