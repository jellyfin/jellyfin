using System.IO;

namespace DvdLib.Ifo;

/// <summary>
/// The cell position information.
/// </summary>
public class CellPositionInfo
{
    /// <summary>
    /// The VOB id.
    /// </summary>
    public readonly ushort VOBId;

    /// <summary>
    /// The cell id.
    /// </summary>
    public readonly byte CellId;

    internal CellPositionInfo(BinaryReader br)
    {
        // Read VOB id
        VOBId = br.ReadUInt16();

        // Skip reserved byte
        br.ReadByte();

        // Read cell id
        CellId = br.ReadByte();
    }
}
