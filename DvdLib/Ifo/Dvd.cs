using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using MediaBrowser.Model.IO;

namespace DvdLib.Ifo
{
    public class Dvd
    {
        private readonly ushort _titleSetCount;
        public readonly List<Title> Titles;

        private ushort _titleCount;
        public readonly Dictionary<ushort, string> VTSPaths = new Dictionary<ushort, string>();
        private readonly IFileSystem _fileSystem;

        public Dvd(string path, IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
            Titles = new List<Title>();
            var allFiles = _fileSystem.GetFiles(path, true).ToList();

            var vmgPath = allFiles.FirstOrDefault(i => string.Equals(i.Name, "VIDEO_TS.IFO", StringComparison.OrdinalIgnoreCase)) ??
                allFiles.FirstOrDefault(i => string.Equals(i.Name, "VIDEO_TS.BUP", StringComparison.OrdinalIgnoreCase));

            if (vmgPath == null)
            {
                var allIfos = allFiles.Where(i => string.Equals(i.Extension, ".ifo", StringComparison.OrdinalIgnoreCase));

                foreach (var ifo in allIfos)
                {
                    var num = ifo.Name.Split('_').ElementAtOrDefault(1);
                    ushort ifoNumber;
                    var numbersRead = new List<ushort>();

                    if (!string.IsNullOrEmpty(num) && ushort.TryParse(num, out ifoNumber) && !numbersRead.Contains(ifoNumber))
                    {
                        ReadVTS(ifoNumber, ifo.FullName);
                        numbersRead.Add(ifoNumber);
                    }
                }
            }
            else
            {
                using (var vmgFs = _fileSystem.GetFileStream(vmgPath.FullName, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
                {
                    using (BigEndianBinaryReader vmgRead = new BigEndianBinaryReader(vmgFs))
                    {
                        vmgFs.Seek(0x3E, SeekOrigin.Begin);
                        _titleSetCount = vmgRead.ReadUInt16();

                        // read address of TT_SRPT
                        vmgFs.Seek(0xC4, SeekOrigin.Begin);
                        uint ttSectorPtr = vmgRead.ReadUInt32();
                        vmgFs.Seek(ttSectorPtr * 2048, SeekOrigin.Begin);
                        ReadTT_SRPT(vmgRead);
                    }
                }

                for (ushort titleSetNum = 1; titleSetNum <= _titleSetCount; titleSetNum++)
                {
                    ReadVTS(titleSetNum, allFiles);
                }
            }
        }

        private void ReadTT_SRPT(BinaryReader read)
        {
            _titleCount = read.ReadUInt16();
            read.BaseStream.Seek(6, SeekOrigin.Current);
            for (uint titleNum = 1; titleNum <= _titleCount; titleNum++)
            {
                Title t = new Title(titleNum);
                t.ParseTT_SRPT(read);
                Titles.Add(t);
            }
        }

        private void ReadVTS(ushort vtsNum, List<FileSystemMetadata> allFiles)
        {
            var filename = String.Format("VTS_{0:00}_0.IFO", vtsNum);

            var vtsPath = allFiles.FirstOrDefault(i => string.Equals(i.Name, filename, StringComparison.OrdinalIgnoreCase)) ??
                allFiles.FirstOrDefault(i => string.Equals(i.Name, Path.ChangeExtension(filename, ".bup"), StringComparison.OrdinalIgnoreCase));

            if (vtsPath == null)
            {
                throw new FileNotFoundException("Unable to find VTS IFO file");
            }

            ReadVTS(vtsNum, vtsPath.FullName);
        }

        private void ReadVTS(ushort vtsNum, string vtsPath)
        {
            VTSPaths[vtsNum] = vtsPath;

            using (var vtsFs = _fileSystem.GetFileStream(vtsPath, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
            {
                using (BigEndianBinaryReader vtsRead = new BigEndianBinaryReader(vtsFs))
                {
                    // Read VTS_PTT_SRPT
                    vtsFs.Seek(0xC8, SeekOrigin.Begin);
                    uint vtsPttSrptSecPtr = vtsRead.ReadUInt32();
                    uint baseAddr = (vtsPttSrptSecPtr * 2048);
                    vtsFs.Seek(baseAddr, SeekOrigin.Begin);

                    ushort numTitles = vtsRead.ReadUInt16();
                    vtsRead.ReadUInt16();
                    uint endaddr = vtsRead.ReadUInt32();
                    uint[] offsets = new uint[numTitles];
                    for (ushort titleNum = 0; titleNum < numTitles; titleNum++)
                    {
                        offsets[titleNum] = vtsRead.ReadUInt32();
                    }

                    for (uint titleNum = 0; titleNum < numTitles; titleNum++)
                    {
                        uint chapNum = 1;
                        vtsFs.Seek(baseAddr + offsets[titleNum], SeekOrigin.Begin);
                        Title t = Titles.FirstOrDefault(vtst => vtst.IsVTSTitle(vtsNum, titleNum + 1));
                        if (t == null) continue;

                        do
                        {
                            t.Chapters.Add(new Chapter(vtsRead.ReadUInt16(), vtsRead.ReadUInt16(), chapNum));
                            if (titleNum + 1 < numTitles && vtsFs.Position == (baseAddr + offsets[titleNum + 1])) break;
                            chapNum++;
                        }
                        while (vtsFs.Position < (baseAddr + endaddr));
                    }

                    // Read VTS_PGCI
                    vtsFs.Seek(0xCC, SeekOrigin.Begin);
                    uint vtsPgciSecPtr = vtsRead.ReadUInt32();
                    vtsFs.Seek(vtsPgciSecPtr * 2048, SeekOrigin.Begin);

                    long startByte = vtsFs.Position;

                    ushort numPgcs = vtsRead.ReadUInt16();
                    vtsFs.Seek(6, SeekOrigin.Current);
                    for (ushort pgcNum = 1; pgcNum <= numPgcs; pgcNum++)
                    {
                        byte pgcCat = vtsRead.ReadByte();
                        bool entryPgc = (pgcCat & 0x80) != 0;
                        uint titleNum = (uint)(pgcCat & 0x7F);

                        vtsFs.Seek(3, SeekOrigin.Current);
                        uint vtsPgcOffset = vtsRead.ReadUInt32();

                        Title t = Titles.FirstOrDefault(vtst => vtst.IsVTSTitle(vtsNum, titleNum));
                        if (t != null) t.AddPgc(vtsRead, startByte + vtsPgcOffset, entryPgc, pgcNum);
                    }
                }
            }
        }
    }
}