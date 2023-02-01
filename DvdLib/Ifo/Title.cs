using System;
using System.Collections.Generic;
using System.IO;

namespace DvdLib.Ifo;

/// <summary>
/// A title.
/// </summary>
public class Title
{
    private ushort _parentalManagementMask;
    private byte _titleNumberInVTS;
    private uint _vtsStartSector; // relative to start of entire disk

    /// <summary>
    /// The title number.
    /// </summary>
    public uint TitleNumber { get; private set; }

    /// <summary>
    /// The title number.
    /// </summary>
    public TitleType TitleType { get; private set; }

    /// <summary>
    /// The angle count.
    /// </summary>
    public uint NumberOfAngles { get; private set; }

    /// <summary>
    /// The chapter count.
    /// </summary>
    public ushort NumberOfChapters { get; private set; }

    /// <summary>
    /// The video title set number.
    /// </summary>
    public byte VideoTitleSetNumber { get; private set; }

    /// <summary>
    /// The entry program chain.
    /// </summary>
    public ProgramChain EntryProgramChain { get; private set; }

    /// <summary>
    /// The program chains.
    /// </summary>
    public readonly List<ProgramChain> ProgramChains;

    /// <summary>
    /// The chapters.
    /// </summary>
    public readonly List<Chapter> Chapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="Title"/> class.
    /// </summary>
    /// <param name="titleNum">The title number.</param>
    /// <param name="br">The binary reader.</param>
    /// <returns>The <see cref="Title"/>.</returns>
    public Title(uint titleNum, BinaryReader br)
    {
        TitleNumber = titleNum;
        ParseTT_SRPT(br);
        ProgramChains = new List<ProgramChain>();
        Chapters = new List<Chapter>();
    }

    /// <summary>
    /// Checks if a title is a VTS title.
    /// </summary>
    /// <param name="vtsNum">The VTS number.</param>
    /// <param name="vtsTitleNum">The VTS title number.</param>
    /// <returns>Returns true if the title is VTS.</returns>
    public bool IsVTSTitle(uint vtsNum, uint vtsTitleNum)
    {
        return (vtsNum == VideoTitleSetNumber && vtsTitleNum == _titleNumberInVTS);
    }

    private void ParseTT_SRPT(BinaryReader br)
    {
        TitleType = new TitleType(br);
        NumberOfAngles = br.ReadByte();
        NumberOfChapters = br.ReadUInt16();
        _parentalManagementMask = br.ReadUInt16();
        VideoTitleSetNumber = br.ReadByte();
        _titleNumberInVTS = br.ReadByte();
        _vtsStartSector = br.ReadUInt32();
    }

    internal void AddProgramChains(BinaryReader br, long startByte, bool entryPgc, uint pgcNum)
    {
        long curPos = br.BaseStream.Position;
        br.BaseStream.Seek(startByte, SeekOrigin.Begin);

        var pgc = new ProgramChain(pgcNum);
        pgc.ParseHeader(br);
        ProgramChains.Add(pgc);
        if (entryPgc)
        {
            EntryProgramChain = pgc;
        }

        br.BaseStream.Seek(curPos, SeekOrigin.Begin);
    }
}
