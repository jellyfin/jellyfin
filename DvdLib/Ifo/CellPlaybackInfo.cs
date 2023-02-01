using DvdLib.Enums;
using System.IO;

namespace DvdLib.Ifo;

/// <summary>
/// A cell's playback information.
/// </summary>
public class CellPlaybackInfo
{
    /// <summary>
    /// The block mode.
    /// </summary>
    public readonly BlockMode Mode;

    /// <summary>
    /// The block type.
    /// </summary>
    public readonly BlockType Type;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> supports seamless playback.
    /// </summary>
    /// <value><c>true</c> if the cell supports it; otherwise, <c>false</c>.</value>
    public readonly bool SeamlessPlay;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> is interleaved.
    /// </summary>
    /// <value><c>true</c> if the cell is interleaved; otherwise, <c>false</c>.</value>
    public readonly bool Interleaved;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> supports seamless playback.
    /// </summary>
    /// <value><c>true</c> if the cell supports it; otherwise, <c>false</c>.</value>
    public readonly bool STCDiscontinuity;

    /// <summary>
    /// A value indicating whether this <see cref="Cell" /> supports seamless playback.
    /// </summary>
    /// <value><c>true</c> if the cell supports it; otherwise, <c>false</c>.</value>
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
    /// The first sector.
    /// </summary>
    public readonly uint FirstSector;

    /// <summary>
    /// The first ILV end sector.
    /// </summary>
    public readonly uint FirstILVUEndSector;

    /// <summary>
    /// The last VOB start sector.
    /// </summary>
    public readonly uint LastVOBUStartSector;

    /// <summary>
    /// The last sector.
    /// </summary>
    public readonly uint LastSector;

    internal CellPlaybackInfo(BinaryReader br)
    {
        br.BaseStream.Seek(0x4, SeekOrigin.Current);
        PlaybackTime = new DvdTime(br.ReadBytes(4));
        br.BaseStream.Seek(0x10, SeekOrigin.Current);
    }
}
