namespace DvdLib.Enums;

/// <summary>
/// The block mode.
/// </summary>
public enum BlockMode
{
    /// <summary>
    /// Not in a block.
    /// </summary>
    NotInBlock = 0,

    /// <summary>
    /// First cell.
    /// </summary>
    FirstCell = 1,

    /// <summary>
    /// In a block.
    /// </summary>
    InBlock = 2,

    /// <summary>
    /// Last cell.
    /// </summary>
    LastCell = 3,
}
