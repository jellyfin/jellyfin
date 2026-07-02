using System;
using System.Collections.Generic;
using System.IO;
using MediaBrowser.MediaEncoding.Probing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests.Probing;

public class DvdVideoProberTests
{
    // ── Synthetic IFO builders ────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal VMG IFO byte array.
    /// Layout:
    ///   0x00: "DVDVIDEO-VMG"
    ///   0xC0: vmgm_vobs sector (big-endian uint32) — set to 0 unless overridden
    ///   0xC4: tt_srpt sector (big-endian uint32)
    ///   srptSector*2048 + 0: nr_of_srpts (big-endian uint16).
    /// </summary>
    private static byte[] BuildVmgIfoBytes(uint srptSector, ushort titleCount, uint vmgmVobsSector = 0)
    {
        var totalSize = (int)(((long)srptSector * 2048) + 2);
        var data = new byte[totalSize];

        "DVDVIDEO-VMG"u8.CopyTo(data.AsSpan(0));

        WriteBE32(data, 0xC0, vmgmVobsSector);
        WriteBE32(data, 0xC4, srptSector);
        WriteBE16(data, (int)((long)srptSector * 2048), titleCount);

        return data;
    }

    private static MemoryStream BuildVmgIfo(uint srptSector, ushort titleCount, uint vmgmVobsSector = 0)
        => new MemoryStream(BuildVmgIfoBytes(srptSector, titleCount, vmgmVobsSector), writable: false);

    // ── Binary helper tests ───────────────────────────────────────────────────

    [Theory]
    [InlineData(new byte[] { 0x00, 0x01 }, 0, 1)]
    [InlineData(new byte[] { 0x01, 0x00 }, 0, 256)]
    [InlineData(new byte[] { 0xFF, 0xFF }, 0, 65535)]
    [InlineData(new byte[] { 0x00, 0x07 }, 0, 7)]
    public void ReadUInt16BE_ReturnsCorrectValue(byte[] data, int offset, int expected)
        => Assert.Equal((ushort)expected, DvdVideoProber.ReadUInt16BE(data, offset));

    [Theory]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x01 }, 0, 1u)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x11 }, 0, 17u)]
    [InlineData(new byte[] { 0x00, 0x00, 0x00, 0x08 }, 0, 8u)]
    [InlineData(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, 0, 0xFFFFFFFFu)]
    [InlineData(new byte[] { 0x01, 0x02, 0x03, 0x04 }, 0, 0x01020304u)]
    public void ReadUInt32BE_ReturnsCorrectValue(byte[] data, int offset, uint expected)
        => Assert.Equal(expected, DvdVideoProber.ReadUInt32BE(data, offset));

    // ── ReadVmgTitleCount tests (stream-based, kept for compat) ──────────────

    [Theory]
    [InlineData(1u, 1)]
    [InlineData(1u, 5)]
    [InlineData(2u, 3)]
    [InlineData(1u, 99)]
    public void ReadVmgTitleCount_ValidIfo_ReturnsCorrectCount(uint srptSector, int expectedCount)
    {
        using var stream = BuildVmgIfo(srptSector, (ushort)expectedCount);
        Assert.Equal(expectedCount, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    [Fact]
    public void ReadVmgTitleCount_WrongMagic_ReturnsZero()
    {
        using var stream = new MemoryStream(new byte[4096]);
        Assert.Equal(0, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    [Fact]
    public void ReadVmgTitleCount_EmptyStream_ReturnsZero()
    {
        using var stream = new MemoryStream(Array.Empty<byte>());
        Assert.Equal(0, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    [Fact]
    public void ReadVmgTitleCount_TruncatedAfterMagic_ReturnsZero()
    {
        var data = new byte[20];
        "DVDVIDEO-VMG"u8.CopyTo(data.AsSpan(0));
        using var stream = new MemoryStream(data, writable: false);
        Assert.Equal(0, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    [Fact]
    public void ReadVmgTitleCount_ZeroSectorPointer_ReturnsZero()
    {
        var data = new byte[2050];
        "DVDVIDEO-VMG"u8.CopyTo(data.AsSpan(0));
        using var stream = new MemoryStream(data, writable: false);
        Assert.Equal(0, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    [Fact]
    public void ReadVmgTitleCount_SectorAddressBeyondFileLength_ReturnsZero()
    {
        const uint hugeSector = 100u;
        var data = new byte[512];
        "DVDVIDEO-VMG"u8.CopyTo(data.AsSpan(0));
        WriteBE32(data, 0xC4, hugeSector);
        using var stream = new MemoryStream(data, writable: false);
        Assert.Equal(0, DvdVideoProber.ReadVmgTitleCount(stream));
    }

    // ── Real-world layout regression tests ───────────────────────────────────

    /// <summary>
    /// Verifies the three real-world IFO layouts from the bug report:
    ///   VIDEO_TS_1.IFO: vmgm_vobs=0x00, tt_srpt=1 → 7 titles
    ///   VIDEO_TS_2.IFO: vmgm_vobs=0x11, tt_srpt=1 → 72 titles
    ///   VIDEO_TS_3.IFO: vmgm_vobs=0x08, tt_srpt=1 → 11 titles
    /// Confirms that vmgm_vobs at 0xC0 is not confused with tt_srpt at 0xC4.
    /// </summary>
    /// <param name="vmgmVobsSector">The sector value stored at the vmgm_vobs pointer (byte value placed at 0xC0).</param>
    /// <param name="expectedCount">The expected number of titles parsed from the TT_SRPT table.</param>
    [Theory]
    [InlineData(0x00u, 7)]
    [InlineData(0x11u, 72)]
    [InlineData(0x08u, 11)]
    public void ReadVmgTitleCount_NonZeroVmgmVobs_DoesNotConfuseWithTtSrpt(uint vmgmVobsSector, int expectedCount)
    {
        using var ms = BuildVmgIfo(1u, (ushort)expectedCount, vmgmVobsSector);
        Assert.Equal(expectedCount, DvdVideoProber.ReadVmgTitleCount(ms));
    }

    // ── ParseVmgIfo tests (via ParseVideoTsDirectory helper) ─────────────────

    /// <summary>
    /// Verifies TT_SRPT entry parsing: TitleNumber, TitleSetNumber, VtsTitleNumber,
    /// ChapterCount, AngleCount, VtsStartSector.
    /// </summary>
    [Fact]
    public void ParseVmgIfo_SingleTitle_ParsesAllFields()
    {
        var data = BuildVmgIfoBytes(1u, 1);
        Array.Resize(ref data, 2048 + 8 + 12);

        WriteBE32(data, 2048 + 4, 19u);

        int o = 2048 + 8;
        data[o + 0] = 0x01;
        data[o + 1] = 2;
        WriteBE16(data, o + 2, 34);
        WriteBE16(data, o + 4, 0);
        data[o + 6] = 1;
        data[o + 7] = 1;
        WriteBE32(data, o + 8, 5u);

        WriteBE16(data, 2048 + 0, 1);

        using var dir = new TempVideoTsDirectory(data);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());

        Assert.Single(disc.Titles);
        var t = disc.Titles[0];
        Assert.Equal(1, t.TitleNumber);
        Assert.Equal(1, t.TitleSetNumber);
        Assert.Equal(1, t.VtsTitleNumber);
        Assert.Equal(34, t.ChapterCount);
        Assert.Equal(2, t.AngleCount);
        Assert.Equal(5u, t.VtsStartSector);
    }

    [Fact]
    public void ParseVmgIfo_MultipleTitles_CountMatchesTitleCount()
    {
        const int titleCount = 7;
        var data = BuildVmgIfoBytes(1u, (ushort)titleCount);
        int entriesBase = 2048 + 8;
        Array.Resize(ref data, entriesBase + (titleCount * 12));
        WriteBE32(data, 2048 + 4, (uint)(8 + (titleCount * 12) - 1));
        WriteBE16(data, 2048 + 0, (ushort)titleCount);

        for (int i = 0; i < titleCount; i++)
        {
            int o = entriesBase + (i * 12);
            data[o + 1] = 1;
            WriteBE16(data, o + 2, 1);
            data[o + 6] = 1;
            data[o + 7] = (byte)(i + 1);
        }

        using var dir = new TempVideoTsDirectory(data);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());

        Assert.Equal(titleCount, disc.Titles.Count);
        for (int i = 0; i < titleCount; i++)
        {
            Assert.Equal(i + 1, disc.Titles[i].TitleNumber);
        }
    }

    [Fact]
    public void ParseVideoTsDirectory_MissingVmgIfo_ReturnsEmptyDisc()
    {
        using var emptyDir = new TempVideoTsDirectory(null);
        var disc = DvdVideoProber.ParseVideoTsDirectory(emptyDir.Path, Mock.Of<ILogger<DvdVideoProber>>());
        Assert.Empty(disc.Titles);
    }

    [Fact]
    public void ParseVideoTsDirectory_WrongMagic_ReturnsEmptyDisc()
    {
        var data = new byte[4096];
        "NOT-A-DVD-IFO"u8.CopyTo(data.AsSpan(0));
        using var dir = new TempVideoTsDirectory(data);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());
        Assert.Empty(disc.Titles);
    }

    // ── DVD time decoding tests ───────────────────────────────────────────────

    /// <summary>
    /// Verifies BCD decoding against the FromBcd helper exposed via ReadDvdTime round-trips.
    /// </summary>
    /// <param name="bcd">The BCD-encoded minute value to decode.</param>
    /// <param name="expected">The expected minute value after decoding the BCD byte.</param>
    [Theory]
    [InlineData(0x00, 0)] // 0x00 → 0
    [InlineData(0x09, 9)] // 0x09 → 9
    [InlineData(0x10, 10)] // 0x10 → 10
    [InlineData(0x59, 59)] // 0x59 → 59
    [InlineData(0x23, 23)] // 0x23 → 23
    public void FromBcd_ViaDvdTime_CorrectlyDecodesMinutes(byte bcd, int expected)
    {
        // Build a 4-byte dvd_time_t: hour=0x00, minute=bcd, second=0x00, frame_u=0x00
        var time = BuildDvdTime(0x00, bcd, 0x00, 0x00);
        var result = ParseOnePgcDuration(time);
        Assert.NotNull(result);
        Assert.Equal(expected, result!.Value.Minutes);
    }

    [Theory]
    [InlineData(0x01, 0x55, 0x34, 0x40 | 7, 1, 55, 34)] // 01:55:34 @ 25fps, 7 frames
    [InlineData(0x00, 0x00, 0x12, 0x40 | 0, 0, 0, 12)] // 00:00:12 @ 25fps, 0 frames
    [InlineData(0x00, 0x02, 0x34, 0xC0 | 5, 0, 2, 34)] // 00:02:34 @ 29.97fps, 5 frames
    public void TryReadDvdTime_ValidBcd_ReturnsCorrectTimeSpan(
        byte h, byte m, byte s, byte frameAndRate, int expHours, int expMinutes, int expSeconds)
    {
        var time = BuildDvdTime(h, m, s, frameAndRate);
        var result = ParseOnePgcDuration(time);
        Assert.NotNull(result);
        Assert.Equal(expHours, result!.Value.Hours);
        Assert.Equal(expMinutes, result!.Value.Minutes);
        Assert.Equal(expSeconds, result!.Value.Seconds);
    }

    [Fact]
    public void TryReadDvdTime_InvalidMinutes_ReturnsNull()
    {
        // minutes=0x99 → BCD decodes to 99, which is ≥ 60 → invalid
        var time = BuildDvdTime(0x00, 0x99, 0x00, 0x40);
        var result = ParseOnePgcDuration(time);
        Assert.Null(result);
    }

    [Fact]
    public void TryReadDvdTime_InvalidSeconds_ReturnsNull()
    {
        var time = BuildDvdTime(0x00, 0x00, 0x99, 0x40);
        var result = ParseOnePgcDuration(time);
        Assert.Null(result);
    }

    // ── VTS parsing end-to-end tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that a one-title, one-PGC VTS IFO is parsed correctly, yielding the
    /// expected duration for the title.
    /// </summary>
    [Fact]
    public void ParseVideoTsDirectory_WithVtsIfo_ReturnsCorrectDuration()
    {
        // VIDEO_TS.IFO: 1 title → VTS 1, VTS title 1, 34 chapters
        var vmgData = BuildFullVmgIfo(new (int TitleSetNr, int VtsTtn, int Chapters, int Angles, uint Sector)[]
        {
            (TitleSetNr: 1, VtsTtn: 1, Chapters: 34, Angles: 1, Sector: 10u)
        });

        // VTS_01_0.IFO: VTS title 1 → PGC 1 → 1h 55m 34s
        var vtsData = BuildFullVtsIfo(new (int PgcNumber, int ProgramNumber, byte DurationH, byte DurationM, byte DurationS, byte FrameAndRate)[]
        {
            (PgcNumber: 1, ProgramNumber: 1, DurationH: 0x01, DurationM: 0x55, DurationS: 0x34, FrameAndRate: (byte)(0x40 | 7))
        });

        using var dir = new TempVideoTsDirectory(vmgData, vts01Data: vtsData);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());

        Assert.Single(disc.Titles);
        var t = disc.Titles[0];
        Assert.Equal(1, t.TitleNumber);
        Assert.Equal(1, t.ProgramChainNumber);
        Assert.NotNull(t.Duration);
        Assert.Equal(1, t.Duration!.Value.Hours);
        Assert.Equal(55, t.Duration.Value.Minutes);
        Assert.Equal(34, t.Duration.Value.Seconds);
    }

    /// <summary>
    /// Verifies the 7-title layout from the bug-report IFO (all titles in VTS 1).
    /// </summary>
    [Fact]
    public void ParseVideoTsDirectory_SevenTitlesOneTitleSet_AllTitlesPresent()
    {
        var vmgData = BuildFullVmgIfo(new (int TitleSetNr, int VtsTtn, int Chapters, int Angles, uint Sector)[]
        {
            (TitleSetNr: 1, VtsTtn: 1, Chapters: 34, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 2, Chapters: 1, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 3, Chapters: 1, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 4, Chapters: 1, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 5, Chapters: 1, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 6, Chapters: 1, Angles: 1, Sector: 10u),
            (TitleSetNr: 1, VtsTtn: 7, Chapters: 1, Angles: 1, Sector: 10u),
        });

        var vtsData = BuildFullVtsIfo(new (int PgcNumber, int ProgramNumber, byte DurationH, byte DurationM, byte DurationS, byte FrameAndRate)[]
        {
            (PgcNumber: 1, ProgramNumber: 1, DurationH: 0x01, DurationM: 0x55, DurationS: 0x34, FrameAndRate: (byte)(0x40 | 7)),
            (PgcNumber: 2, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x00, DurationS: 0x12, FrameAndRate: (byte)(0x40 | 0)),
            (PgcNumber: 3, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x00, DurationS: 0x32, FrameAndRate: (byte)(0x40 | 0)),
            (PgcNumber: 4, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x00, DurationS: 0x29, FrameAndRate: (byte)(0xC0 | 29)),
            (PgcNumber: 5, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x01, DurationS: 0x05, FrameAndRate: (byte)(0x40 | 0)),
            (PgcNumber: 6, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x00, DurationS: 0x07, FrameAndRate: (byte)(0x40 | 0)),
            (PgcNumber: 7, ProgramNumber: 1, DurationH: 0x00, DurationM: 0x02, DurationS: 0x34, FrameAndRate: (byte)(0xC0 | 5)),
        });

        using var dir = new TempVideoTsDirectory(vmgData, vts01Data: vtsData);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());

        Assert.Equal(7, disc.Titles.Count);
        Assert.Equal(1, disc.Titles[0].TitleNumber);
        Assert.Equal(34, disc.Titles[0].ChapterCount);
        Assert.NotNull(disc.Titles[0].Duration);
        Assert.Equal(1, disc.Titles[0].Duration!.Value.Hours);
        Assert.Equal(55, disc.Titles[0].Duration!.Value.Minutes);
    }

    [Fact]
    public void FindMainFeature_ReturnsLongestTitle()
    {
        var disc = new DvdDiscInfo();
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 1, Duration = TimeSpan.FromMinutes(5) });
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 2, Duration = TimeSpan.FromMinutes(115) });
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 3, Duration = TimeSpan.FromMinutes(2) });

        var main = DvdVideoProber.FindMainFeature(disc);
        Assert.NotNull(main);
        Assert.Equal(2, main!.TitleNumber);
    }

    [Fact]
    public void FindMainFeature_WithMinDuration_ExcludesShortTitles()
    {
        var disc = new DvdDiscInfo();
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 1, Duration = TimeSpan.FromMinutes(115) });
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 2, Duration = TimeSpan.FromMinutes(5) });

        var main = DvdVideoProber.FindMainFeature(disc, minDuration: TimeSpan.FromMinutes(10));
        Assert.NotNull(main);
        Assert.Equal(1, main!.TitleNumber);
    }

    [Fact]
    public void FindMainFeature_AllShortTitles_ReturnsNull()
    {
        var disc = new DvdDiscInfo();
        disc.Titles.Add(new DvdTitleInfo { TitleNumber = 1, Duration = TimeSpan.FromMinutes(2) });

        Assert.Null(DvdVideoProber.FindMainFeature(disc, minDuration: TimeSpan.FromMinutes(10)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void WriteBE16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteBE32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static byte[] BuildDvdTime(byte h, byte m, byte s, byte frameAndRate)
        => new[] { h, m, s, frameAndRate };

    /// <summary>
    /// Exercises TryReadDvdTime by embedding the 4-byte dvd_time_t inside a
    /// minimal VTS_PGCITI table (one PGC) and parsing it via ParseVideoTsDirectory.
    /// </summary>
    private static TimeSpan? ParseOnePgcDuration(byte[] dvdTime)
    {
        // Build a VMG IFO pointing to VTS 1, VTS title 1
        var vmgData = BuildFullVmgIfo(new (int TitleSetNr, int VtsTtn, int Chapters, int Angles, uint Sector)[]
        {
            (TitleSetNr: 1, VtsTtn: 1, Chapters: 1, Angles: 1, Sector: 10u)
        });

        // Build a VTS IFO with the given dvd_time_t bytes injected as the PGC playback time
        var vtsData = BuildFullVtsIfoWithRawTime(dvdTime);

        using var dir = new TempVideoTsDirectory(vmgData, vts01Data: vtsData);
        var disc = DvdVideoProber.ParseVideoTsDirectory(dir.Path, Mock.Of<ILogger<DvdVideoProber>>());
        return disc.Titles.Count == 1 ? disc.Titles[0].Duration : null;
    }

    /// <summary>
    /// Builds a complete VMG IFO with the given title entries.
    /// Each title entry follows the title_info_t layout (12 bytes).
    /// </summary>
    private static byte[] BuildFullVmgIfo(
        IReadOnlyList<(int TitleSetNr, int VtsTtn, int Chapters, int Angles, uint Sector)> entries)
    {
        const int srptSector = 1;
        const int srptBase = srptSector * 2048;
        int entryCount = entries.Count;
        int tableSize = 8 + (entryCount * 12);
        var data = new byte[srptBase + tableSize];

        "DVDVIDEO-VMG"u8.CopyTo(data.AsSpan(0));
        WriteBE32(data, 0xC4, srptSector);
        WriteBE16(data, srptBase + 0, entryCount);
        WriteBE32(data, srptBase + 4, (uint)(tableSize - 1));

        for (int i = 0; i < entryCount; i++)
        {
            var (titleSetNr, vtsTtn, chapters, angles, sector) = entries[i];
            int o = srptBase + 8 + (i * 12);
            data[o + 1] = (byte)angles;
            WriteBE16(data, o + 2, (ushort)chapters);
            data[o + 6] = (byte)titleSetNr;
            data[o + 7] = (byte)vtsTtn;
            WriteBE32(data, o + 8, sector);
        }

        return data;
    }

    /// <summary>
    /// Builds a VTS IFO where each entry maps (VTS title N → PGC N).
    /// VTS_PTT_SRPT at sector 1, VTS_PGCITI at sector 2.
    /// </summary>
    private static byte[] BuildFullVtsIfo(
        IReadOnlyList<(int PgcNumber, int ProgramNumber, byte DurationH, byte DurationM, byte DurationS, byte FrameAndRate)> pgcs)
    {
        int vtsTitleCount = pgcs.Count;

        // Sector layout: 0=header, 1=VTS_PTT_SRPT, 2=VTS_PGCITI
        const int pttSector = 1;
        const int pgcitiSector = 2;

        // --- VTS_PTT_SRPT ---
        // header (8 bytes) + one UInt32 offset per title + one PTT entry (4 bytes) per title
        int pttOffsetTableSize = vtsTitleCount * 4;
        int pttDataSize = 8 + pttOffsetTableSize + (vtsTitleCount * 4);
        int pttBase = pttSector * 2048;

        // --- VTS_PGCITI ---
        // header (8 bytes) + 8 bytes per pgci_srp_t + 8-byte PGC header per entry
        const int pgcHeaderSize = 8; // only the bytes we write (+4=playback_time)
        int pgcitiSrpSize = vtsTitleCount * 8;
        int pgcitiDataSize = 8 + pgcitiSrpSize + (vtsTitleCount * pgcHeaderSize);
        int pgcitiBase = pgcitiSector * 2048;

        var data = new byte[Math.Max(pttBase + pttDataSize, pgcitiBase + pgcitiDataSize)];

        // Magic
        "DVDVIDEO-VTS"u8.CopyTo(data.AsSpan(0));

        // vtsi_mat_t pointers
        WriteBE32(data, 0xC8, pttSector);
        WriteBE32(data, 0xCC, pgcitiSector);

        // VTS_PTT_SRPT header
        WriteBE16(data, pttBase + 0, (ushort)vtsTitleCount);
        WriteBE32(data, pttBase + 4, (uint)(pttDataSize - 1));

        // Offset table: one UInt32 per title, relative to pttBase.
        // Each title occupies 4 bytes (one PTT entry) starting after the offset table.
        for (int n = 0; n < vtsTitleCount; n++)
        {
            uint titleRelOffset = (uint)(8 + pttOffsetTableSize + (n * 4));
            WriteBE32(data, pttBase + 8 + (n * 4), titleRelOffset);

            // PTT entry: pgcn and pgn
            var (pgcNumber, programNumber, _, _, _, _) = pgcs[n];
            int pttEntryAddr = pttBase + (int)titleRelOffset;
            WriteBE16(data, pttEntryAddr + 0, (ushort)pgcNumber);
            WriteBE16(data, pttEntryAddr + 2, (ushort)programNumber);
        }

        // VTS_PGCITI header
        WriteBE16(data, pgcitiBase + 0, (ushort)vtsTitleCount);
        WriteBE32(data, pgcitiBase + 4, (uint)(pgcitiDataSize - 1));

        // pgci_srp_t entries: each is 8 bytes, pgc_start_byte at srp+4 relative to pgcitiBase
        for (int n = 0; n < vtsTitleCount; n++)
        {
            int srpOffset = pgcitiBase + 8 + (n * 8);
            uint pgcRelOff = (uint)(8 + pgcitiSrpSize + (n * pgcHeaderSize));
            WriteBE32(data, srpOffset + 4, pgcRelOff);

            // PGC at pgcitiBase + pgcRelOff: playback_time at +4
            var (_, _, durationH, durationM, durationS, frameAndRate) = pgcs[n];
            int pgcAddr = pgcitiBase + (int)pgcRelOff;
            data[pgcAddr + 4] = durationH;
            data[pgcAddr + 5] = durationM;
            data[pgcAddr + 6] = durationS;
            data[pgcAddr + 7] = frameAndRate;
        }

        return data;
    }

    /// <summary>
    /// Like <see cref="BuildFullVtsIfo"/> but injects raw dvd_time_t bytes for PGC 1.
    /// </summary>
    private static byte[] BuildFullVtsIfoWithRawTime(byte[] dvdTime)
    {
        var pgcs = new (int PgcNumber, int ProgramNumber, byte DurationH, byte DurationM, byte DurationS, byte FrameAndRate)[]
        {
            (PgcNumber: 1, ProgramNumber: 1, DurationH: dvdTime[0], DurationM: dvdTime[1],
             DurationS: dvdTime[2], FrameAndRate: dvdTime[3])
        };
        return BuildFullVtsIfo(pgcs);
    }

    // ── TempVideoTsDirectory helper ────────────────────────────────────────────

    /// <summary>
    /// Creates a temporary VIDEO_TS directory structure for testing and deletes it on dispose.
    /// </summary>
    private sealed class TempVideoTsDirectory : IDisposable
    {
        public TempVideoTsDirectory(byte[]? vmgIfoData, byte[]? vts01Data = null)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            var videoTs = System.IO.Path.Combine(Path, "VIDEO_TS");
            Directory.CreateDirectory(videoTs);

            if (vmgIfoData is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(videoTs, "VIDEO_TS.IFO"), vmgIfoData);
            }

            if (vts01Data is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(videoTs, "VTS_01_0.IFO"), vts01Data);
            }
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // best effort
            }
        }
    }
}
