using DvdLib.Enums;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DvdLib.Ifo;

/// <summary>
/// A program chain.
/// </summary>
public class ProgramChain
{
    private byte _programCount;
    private byte _cellCount;
    private ushort _nextProgramNumber;
    private ushort _prevProgramNumber;
    private ushort _goupProgramNumber;
    private ushort _commandTableOffset;
    private ushort _programMapOffset;
    private ushort _cellPlaybackOffset;
    private ushort _cellPositionOffset;

    /// <summary>
    /// The programs.
    /// </summary>
    public readonly List<Program> Programs;

    /// <summary>
    /// The cells.
    /// </summary>
    public readonly List<Cell> Cells;

    /// <summary>
    /// The playback time.
    /// </summary>
    public DvdTime PlaybackTime { get; private set; }

    /// <summary>
    /// The prohibited user operations.
    /// </summary>
    public UserOperation ProhibitedUserOperations { get; private set; }

    /// <summary>
    /// The audio stream control.
    /// </summary>
    public byte[] AudioStreamControl { get; private set; } // 8*2 entries

    /// <summary>
    /// The subpicture stream control.
    /// </summary>
    public byte[] SubpictureStreamControl { get; private set; } // 32*4 entries

    /// <summary>
    /// The playback mode.
    /// </summary>
    public ProgramPlaybackMode PlaybackMode { get; private set; }

    /// <summary>
    /// The program count.
    /// </summary>
    public uint ProgramCount { get; private set; }

    /// <summary>
    /// The still time.
    /// </summary>
    public byte StillTime { get; private set; }

    /// <summary>
    /// The color palette.
    /// </summary>
    public byte[] Palette { get; private set; } // 16*4 entries

    /// <summary>
    /// The video title set index.
    /// </summary>
    public readonly uint VideoTitleSetIndex;

    internal ProgramChain(uint vtsPgcNum)
    {
        VideoTitleSetIndex = vtsPgcNum;
        Cells = new List<Cell>();
        Programs = new List<Program>();
    }

    internal void ParseHeader(BinaryReader br)
    {
        long startPos = br.BaseStream.Position;

        br.ReadUInt16();
        _programCount = br.ReadByte();
        _cellCount = br.ReadByte();
        PlaybackTime = new DvdTime(br.ReadBytes(4));
        ProhibitedUserOperations = (UserOperation)br.ReadUInt32();
        AudioStreamControl = br.ReadBytes(16);
        SubpictureStreamControl = br.ReadBytes(128);

        _nextProgramNumber = br.ReadUInt16();
        _prevProgramNumber = br.ReadUInt16();
        _goupProgramNumber = br.ReadUInt16();

        byte pbMode = br.ReadByte();
        if (pbMode == 0)
        {
            PlaybackMode = ProgramPlaybackMode.Sequential;
        }
        else
        {
            PlaybackMode = ((pbMode & 0x80) == 0) ? ProgramPlaybackMode.Random : ProgramPlaybackMode.Shuffle;
        }

        ProgramCount = (uint)(pbMode & 0x7F);

        StillTime = br.ReadByte();
        Palette = br.ReadBytes(64);
        _commandTableOffset = br.ReadUInt16();
        _programMapOffset = br.ReadUInt16();
        _cellPlaybackOffset = br.ReadUInt16();
        _cellPositionOffset = br.ReadUInt16();

        // Read position info
        br.BaseStream.Seek(startPos + _cellPositionOffset, SeekOrigin.Begin);
        for (var cellNum = 0; cellNum < _cellCount; cellNum++)
        {
            var c = new Cell();
            c.ParsePosition(br);
            Cells.Add(c);
        }

        br.BaseStream.Seek(startPos + _cellPlaybackOffset, SeekOrigin.Begin);
        for (int cellNum = 0; cellNum < _cellCount; cellNum++)
        {
            Cells[cellNum].ParsePlayback(br);
        }

        br.BaseStream.Seek(startPos + _programMapOffset, SeekOrigin.Begin);
        var cellNumbers = new List<int>();
        for (int progNum = 0; progNum < _programCount; progNum++) cellNumbers.Add(br.ReadByte() - 1);

        for (int i = 0; i < cellNumbers.Count; i++)
        {
            int max = (i + 1 == cellNumbers.Count) ? _cellCount : cellNumbers[i + 1];
            Programs.Add(new Program(Cells.Where((c, idx) => idx >= cellNumbers[i] && idx < max).ToList()));
        }
    }
}
