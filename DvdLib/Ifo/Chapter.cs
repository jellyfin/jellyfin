using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DvdLib.Ifo
{
    public class Chapter
    {
        public ushort ProgramChainNumber { get; private set; }
        public ushort ProgramNumber { get; private set; }
        public uint ChapterNumber { get; private set; }

        public Chapter(ushort pgcNum, ushort programNum, uint chapterNum)
        {
            ProgramChainNumber = pgcNum;
            ProgramNumber = programNum;
            ChapterNumber = chapterNum;
        }
    }
}
