using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DvdLib.Ifo;

/// <summary>
/// A DVD.
/// </summary>
public class Dvd
{
    private readonly ushort _titleSetCount;
    private ushort _titleCount;

    /// <summary>
    /// The list of titles.
    /// </summary>
    public readonly List<Title> Titles;


    /// <summary>
    /// The dictionary of valid VTS paths.
    /// </summary>
    public readonly Dictionary<ushort, string> VTSPaths = new Dictionary<ushort, string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="Dvd"/> class.
    /// </summary>
    /// <param name="path">The path to the DVD.</param>
    /// <returns>The <see cref="Dvd"/>.</returns>
    public Dvd(string path)
    {
        Titles = new List<Title>();
        var allFiles = new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories);

        var vmgPath = allFiles.FirstOrDefault(i => string.Equals(i.Name, "VIDEO_TS.IFO", StringComparison.OrdinalIgnoreCase))
            ?? allFiles.FirstOrDefault(i => string.Equals(i.Name, "VIDEO_TS.BUP", StringComparison.OrdinalIgnoreCase));

        if (vmgPath == null)
        {
            // Check all files for IFOs
            foreach (var ifo in allFiles)
            {
                if (!string.Equals(ifo.Extension, ".ifo", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Extract IFO number
                var nums = ifo.Name.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (nums.Length >= 2 && ushort.TryParse(nums[1], out var ifoNumber))
                {
                    // Read VTS IFO
                    ReadVTS(ifoNumber, ifo.FullName);
                }
            }
        }
        else
        {
            using (var vmgFs = new FileStream(vmgPath.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var vmgRead = new BigEndianBinaryReader(vmgFs))
                {
                    // Seek to title count
                    vmgFs.Seek(0x3E, SeekOrigin.Begin);
                    // Read title count
                    _titleSetCount = vmgRead.ReadUInt16();
                    // Seek to title table sector pointer (TT_SRPT)
                    vmgFs.Seek(0xC4, SeekOrigin.Begin);
                    // Read TT_SRPT
                    uint ttSectorPtr = vmgRead.ReadUInt32();
                    // Seek to title table
                    vmgFs.Seek(ttSectorPtr * 2048, SeekOrigin.Begin);
                    // Parse title table
                    ReadTT_SRPT(vmgRead);
                }
            }

            // Read all VTS IFOs
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
            Titles.Add(new Title(titleNum, read));
        }
    }

    private void ReadVTS(ushort vtsNum, IReadOnlyList<FileInfo> allFiles)
    {
        var filename = string.Format(CultureInfo.InvariantCulture, "VTS_{0:00}_0.IFO", vtsNum);

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

        using (var vtsFs = new FileStream(vtsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            using (var vtsRead = new BigEndianBinaryReader(vtsFs))
            {
                vtsFs.Seek(0xC8, SeekOrigin.Begin);
                // Seek to VTS_PTT_SRPT
                uint vtsPttSrptSecPtr = vtsRead.ReadUInt32();
                uint baseAddr = (vtsPttSrptSecPtr * 2048);
                vtsFs.Seek(baseAddr, SeekOrigin.Begin);

                // Read VTS_PTT_SRPT
                // Read number of titles (PTTs)
                ushort numTitles = vtsRead.ReadUInt16();
                // Skip reserved bytes
                vtsRead.ReadUInt16();
                // Read end address
                uint endaddr = vtsRead.ReadUInt32();
                // Calculate title offsets
                uint[] offsets = new uint[numTitles];
                for (ushort titleNum = 0; titleNum < numTitles; titleNum++)
                {
                    offsets[titleNum] = vtsRead.ReadUInt32();
                }
                // Add chapters to titles
                for (uint titleNum = 0; titleNum < numTitles; titleNum++)
                {
                    uint chapNum = 1;
                    // Seek to title
                    vtsFs.Seek(baseAddr + offsets[titleNum], SeekOrigin.Begin);
                    // Get matching title object
                    var t = Titles.FirstOrDefault(vtst => vtst.IsVTSTitle(vtsNum, titleNum + 1));
                    if (t == null)
                    {
                        continue;
                    }

                    // Add all chapters
                    do
                    {
                        t.Chapters.Add(new Chapter(vtsRead.ReadUInt16(), vtsRead.ReadUInt16(), chapNum));
                        if (titleNum + 1 < numTitles && vtsFs.Position == (baseAddr + offsets[titleNum + 1]))
                        {
                            break;
                        }

                        chapNum++;
                    }
                    while (vtsFs.Position < (baseAddr + endaddr));
                }

                vtsFs.Seek(0xCC, SeekOrigin.Begin);
                // Seek to VTS_PGCI
                uint vtsPgciSecPtr = vtsRead.ReadUInt32();
                vtsFs.Seek(vtsPgciSecPtr * 2048, SeekOrigin.Begin);
                // Read VTS_PGCI
                long startByte = vtsFs.Position;
                // Read number of program chains (PGCs)
                ushort numPgcs = vtsRead.ReadUInt16();
                vtsFs.Seek(6, SeekOrigin.Current);
                // Add all PGCs to titles
                for (ushort pgcNum = 1; pgcNum <= numPgcs; pgcNum++)
                {
                    byte pgcCat = vtsRead.ReadByte();
                    bool entryPgc = (pgcCat & 0x80) != 0;
                    uint titleNum = (uint)(pgcCat & 0x7F);

                    vtsFs.Seek(3, SeekOrigin.Current);
                    uint vtsPgcOffset = vtsRead.ReadUInt32();

                    var t = Titles.FirstOrDefault(vtst => vtst.IsVTSTitle(vtsNum, titleNum));
                    if (t != null)
                    {
                        t.AddProgramChains(vtsRead, startByte + vtsPgcOffset, entryPgc, pgcNum);
                    }
                }
            }
        }
    }
}
