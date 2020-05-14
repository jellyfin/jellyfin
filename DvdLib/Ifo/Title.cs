#pragma warning disable CS1591

using System.Collections.Generic;
using System.IO;

namespace DvdLib.Ifo
{
    public class Title
    {
        public uint TitleNumber { get; private set; }
        public uint AngleCount { get; private set; }
        public ushort ChapterCount { get; private set; }
        public byte VideoTitleSetNumber { get; private set; }

        private ushort _parentalManagementMask;
        private byte _titleNumberInVTS;
        private uint _vtsStartSector; // relative to start of entire disk

        public ProgramChain EntryProgramChain { get; private set; }
        public readonly List<ProgramChain> ProgramChains;

        public readonly List<Chapter> Chapters;

        public Title(uint titleNum)
        {
            ProgramChains = new List<ProgramChain>();
            Chapters = new List<Chapter>();
            Chapters = new List<Chapter>();
            TitleNumber = titleNum;
        }

        public bool IsVTSTitle(uint vtsNum, uint vtsTitleNum)
        {
            return (vtsNum == VideoTitleSetNumber && vtsTitleNum == _titleNumberInVTS);
        }

        internal void ParseTT_SRPT(BinaryReader br)
        {
            byte titleType = br.ReadByte();
            // TODO parse Title Type

            AngleCount = br.ReadByte();
            ChapterCount = br.ReadUInt16();
            _parentalManagementMask = br.ReadUInt16();
            VideoTitleSetNumber = br.ReadByte();
            _titleNumberInVTS = br.ReadByte();
            _vtsStartSector = br.ReadUInt32();
        }

        internal void AddPgc(BinaryReader br, long startByte, bool entryPgc, uint pgcNum)
        {
            long curPos = br.BaseStream.Position;
            br.BaseStream.Seek(startByte, SeekOrigin.Begin);

            var pgc = new ProgramChain(pgcNum);
            pgc.ParseHeader(br);
            ProgramChains.Add(pgc);
            if (entryPgc) EntryProgramChain = pgc;

            br.BaseStream.Seek(curPos, SeekOrigin.Begin);
        }
    }
}
