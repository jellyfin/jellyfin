using DvdLib.Enums;
using System.Collections;
using System.IO;

namespace DvdLib.Ifo;

/// <summary>
/// A cell's playback information.
/// </summary>
public class CellPlaybackInfo
{
    /// <summary>
    /// The cell type.
    /// </summary>
    public readonly CellType CellType;

    /// <summary>
    /// The block type.
    /// </summary>
    public readonly BlockType BlockType;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> uses seamless multiplex.
    /// </summary>
    /// <value><c>true</c> if the cell uses it; otherwise, <c>false</c>.</value>
    public readonly bool SeamlessMultiplex;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> is interleaved.
    /// </summary>
    /// <value><c>true</c> if the cell is interleaved; otherwise, <c>false</c>.</value>
    public readonly bool Interleaved;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> uses SCR discontinuity.
    /// </summary>
    /// <value><c>true</c> if the cell uses it; otherwise, <c>false</c>.</value>
    public readonly bool SCRDiscontinuity;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> has seamless angle linked in DSI.
    /// </summary>
    /// <value><c>true</c> if the cell has it; otherwise, <c>false</c>.</value>
    public readonly bool SeamlessAngle;

    /// <summary>
    /// The playback mode.
    /// </summary>
    public readonly PlaybackMode PlaybackMode;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> is restricted.
    /// </summary>
    /// <value><c>true</c> if the cell is restricted; otherwise, <c>false</c>.</value>
    public readonly bool Restricted;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> uses VOBU still mode.
    /// </summary>
    /// <value><c>true</c> if the cell does; otherwise, <c>false</c>.</value>
    public readonly bool VOBUStillMode;

    /// <summary>
    /// The still time.
    /// </summary>
    public readonly byte StillTime;

    /// <summary>
    /// The command number.
    /// </summary>
    public readonly byte CommandNumber;

    /// <summary>
    /// The playback time.
    /// </summary>
    public readonly DvdTime PlaybackTime;

    /// <summary>
    /// The first VOBU sector.
    /// </summary>
    public readonly uint FirstVOBUStartSector;

    /// <summary>
    /// The first ILVU end sector.
    /// </summary>
    public readonly uint FirstILVUEndSector;

    /// <summary>
    /// The last VOBU start sector.
    /// </summary>
    public readonly uint LastVOBUStartSector;

    /// <summary>
    /// The last VOBU end sector.
    /// </summary>
    public readonly uint LastVOBUEndSector;

    internal CellPlaybackInfo(BinaryReader br)
    {
        // Read cell category bytes and extract information (beware endianess)
        var cellCategory = br.ReadBytes(4);
        var bitArray = new BitArray(new byte[] {cellCategory[0]});
        SeamlessAngle = bitArray[0];
        SCRDiscontinuity = bitArray[1];
        Interleaved = bitArray[2];
        SeamlessMultiplex = bitArray[3];
        BlockType = (BlockType) (bitArray[5] ? 1 : 0); // Skip second bit because it is not relevant
        CellType = (CellType) ((bitArray[6] ? 1 : 0) + (bitArray[7] ? 1 : 0) * 2);

        bitArray = new BitArray(new byte[] {cellCategory[1]});
        // Skip karaoke application cell type handling
        Restricted = bitArray[5];
        VOBUStillMode = bitArray[6];
        // Skip reserved bit

        StillTime = cellCategory[2];
        CommandNumber = cellCategory[3];

        PlaybackTime = new DvdTime(br.ReadBytes(4));
        FirstVOBUStartSector = br.ReadByte();
        FirstILVUEndSector = br.ReadByte();
        FirstVOBUStartSector = br.ReadByte();
        LastVOBUEndSector = br.ReadByte();

        // Seek to the end of the cell playback information table
        br.BaseStream.Seek(0x14, SeekOrigin.Current);
    }
}
