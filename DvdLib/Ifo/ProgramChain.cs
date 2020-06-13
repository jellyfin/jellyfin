#pragma warning disable CS1591

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DvdLib.Ifo
{
    public enum ProgramPlaybackMode
    {
        Sequential,
        Random,
        Shuffle
    }

    public class ProgramChain
    {
        private byte _programCount;
        public readonly List<Program> Programs;

        private byte _cellCount;
        public readonly List<Cell> Cells;

        public DvdTime PlaybackTime { get; private set; }
        public UserOperation ProhibitedUserOperations { get; private set; }
        public byte[] AudioStreamControl { get; private set; } // 8*2 entries
        public byte[] SubpictureStreamControl { get; private set; } // 32*4 entries

        private ushort _nextProgramNumber;

        private ushort _prevProgramNumber;

        private ushort _goupProgramNumber;

        public ProgramPlaybackMode PlaybackMode { get; private set; }
        public uint ProgramCount { get; private set; }

        public byte StillTime { get; private set; }
        public byte[] Palette { get; private set; } // 16*4 entries

        private ushort _commandTableOffset;

        private ushort _programMapOffset;
        private ushort _cellPlaybackOffset;
        private ushort _cellPositionOffset;

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

            StillTime = br.ReadByte();
            byte pbMode = br.ReadByte();
            if (pbMode == 0) PlaybackMode = ProgramPlaybackMode.Sequential;
            else PlaybackMode = ((pbMode & 0x80) == 0) ? ProgramPlaybackMode.Random : ProgramPlaybackMode.Shuffle;
            ProgramCount = (uint)(pbMode & 0x7F);

            Palette = br.ReadBytes(64);
            _commandTableOffset = br.ReadUInt16();
            _programMapOffset = br.ReadUInt16();
            _cellPlaybackOffset = br.ReadUInt16();
            _cellPositionOffset = br.ReadUInt16();

            // read position info
            br.BaseStream.Seek(startPos + _cellPositionOffset, SeekOrigin.Begin);
            for (int cellNum = 0; cellNum < _cellCount; cellNum++)
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
}
