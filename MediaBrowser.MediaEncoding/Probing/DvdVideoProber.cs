using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Parses DVD-Video IFO files (VIDEO_TS.IFO and VTS_xx_0.IFO) to enumerate disc titles
    /// and resolve per-title durations.
    /// </summary>
    /// <remarks>
    /// Binary layout follows the DVD specification as implemented in libdvdread / libdvdnav.
    /// Key references:
    /// <list type="bullet">
    ///   <item><description>libdvdread <c>ifo_types.h</c> — structure definitions.</description></item>
    ///   <item><description>libdvdread <c>ifo_read.c</c> — parsing functions (ifoRead_TT_SRPT, ifoRead_VTS_PTT_SRPT, ifoRead_VTS_PGCIT).</description></item>
    ///   <item><description>libdvdnav <c>read_cache.c</c> / <c>dvdnav.c</c> — sector navigation.</description></item>
    /// </list>
    /// All multi-byte integers in IFO files are big-endian.
    /// DVD sectors are 2 048 bytes.
    /// Sector pointers in header fields are relative to the start of the IFO file.
    /// Table-internal offsets are relative to the start of the table (not the file).
    /// </remarks>
    internal static class DvdVideoProber
    {
        private const string VmgIfoDirectoryName = "VIDEO_TS";
        private const string VmgIfoFileName = "VIDEO_TS.IFO";
        private const int DvdSectorSize = 2048;

        // ── Public convenience wrappers (title-number lists only) ──────────────

        /// <summary>
        /// Returns the list of title numbers (1-based) present in a DVD ISO file,
        /// by reading the Video Manager IFO embedded in the UDF image.
        /// </summary>
        /// <param name="isoPath">Path to the ISO image file.</param>
        /// <returns>An ordered list of title numbers, or an empty list on failure.</returns>
        internal static IReadOnlyList<int> GetIsoTitleNumbers(string isoPath)
        {
            var disc = ParseVideoTsIso(isoPath);
            return disc.Titles.Count > 0
                ? disc.Titles.Select(t => t.TitleNumber).ToList()
                : Array.Empty<int>();
        }

        /// <summary>
        /// Returns the list of title numbers (1-based) present in an unpacked DVD directory,
        /// by reading the Video Manager IFO from the filesystem.
        /// </summary>
        /// <param name="dvdPath">
        /// Path to either the root directory (containing a VIDEO_TS sub-directory)
        /// or directly to the VIDEO_TS directory itself.
        /// </param>
        /// <returns>An ordered list of title numbers, or an empty list on failure.</returns>
        internal static IReadOnlyList<int> GetDirectoryTitleNumbers(string dvdPath)
        {
            var disc = ParseVideoTsDirectory(dvdPath);
            return disc.Titles.Count > 0
                ? disc.Titles.Select(t => t.TitleNumber).ToList()
                : Array.Empty<int>();
        }

        // ── Full disc parsers ──────────────────────────────────────────────────

        /// <summary>
        /// Parses a DVD VIDEO_TS directory (on disk) and returns full disc information
        /// including per-title durations resolved from the VTS IFO files.
        /// </summary>
        /// <param name="dvdPath">
        /// Root DVD directory (may contain a VIDEO_TS sub-directory) or the VIDEO_TS
        /// directory itself.
        /// </param>
        /// <param name="logger">Optional logger; warnings are emitted for missing/corrupt IFOs.</param>
        /// <returns>Parsed disc info; titles list may be empty on failure.</returns>
        internal static DvdDiscInfo ParseVideoTsDirectory(string dvdPath, ILogger? logger = null)
        {
            var videoTsPath = ResolveVideoTsPath(dvdPath);
            if (videoTsPath is null)
            {
                return new DvdDiscInfo();
            }

            var ifoPath = FindFileInsensitive(videoTsPath, VmgIfoFileName);
            if (ifoPath is null)
            {
                logger?.LogWarning("VIDEO_TS.IFO not found in {VideoTsPath}", videoTsPath);
                return new DvdDiscInfo();
            }

            byte[] vmgData;
            try
            {
                vmgData = File.ReadAllBytes(ifoPath);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to read VIDEO_TS.IFO at {Path}", ifoPath);
                return new DvdDiscInfo();
            }

            var disc = ParseVmgIfo(vmgData);
            if (disc.Titles.Count == 0)
            {
                return disc;
            }

            foreach (var vtsGroup in disc.Titles.GroupBy(t => t.TitleSetNumber))
            {
                var vtsNum = vtsGroup.Key;
                if (vtsNum == 0)
                {
                    continue;
                }

                var vtsIfoName = FormattableString.Invariant($"VTS_{vtsNum:D2}_0.IFO");
                var vtsIfoPath = FindFileInsensitive(videoTsPath, vtsIfoName);
                if (vtsIfoPath is null)
                {
                    logger?.LogWarning("VTS IFO {FileName} not found in {VideoTsPath}", vtsIfoName, videoTsPath);
                    continue;
                }

                byte[] vtsData;
                try
                {
                    vtsData = File.ReadAllBytes(vtsIfoPath);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to read VTS IFO at {Path}", vtsIfoPath);
                    continue;
                }

                ParseVtsIfo(vtsData, vtsNum, vtsGroup, logger);
            }

            return disc;
        }

        /// <summary>
        /// Parses a DVD ISO image and returns full disc information including per-title durations.
        /// </summary>
        /// <param name="isoPath">Path to the ISO image file.</param>
        /// <param name="logger">Optional logger.</param>
        /// <returns>Parsed disc info; titles list may be empty on failure.</returns>
        internal static DvdDiscInfo ParseVideoTsIso(string isoPath, ILogger? logger = null)
        {
            try
            {
                using var fileStream = File.Open(isoPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var udfReader = new DiscUtils.Udf.UdfReader(fileStream);

                var vtsDirectory = udfReader.GetDirectoryInfo(VmgIfoDirectoryName);
                if (!vtsDirectory.Exists)
                {
                    logger?.LogWarning("VIDEO_TS directory not found in ISO {Path}", isoPath);
                    return new DvdDiscInfo();
                }

                var allFiles = vtsDirectory.GetFiles();

                var vmgIfoFile = allFiles.FirstOrDefault(
                    f => string.Equals(f.Name, VmgIfoFileName, StringComparison.OrdinalIgnoreCase));
                if (vmgIfoFile is null)
                {
                    logger?.LogWarning("VIDEO_TS.IFO not found in ISO {Path}", isoPath);
                    return new DvdDiscInfo();
                }

                byte[] vmgData;
                using (var stream = udfReader.OpenFile(vmgIfoFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    vmgData = ReadStreamFully(stream);
                }

                var disc = ParseVmgIfo(vmgData);
                if (disc.Titles.Count == 0)
                {
                    return disc;
                }

                foreach (var vtsGroup in disc.Titles.GroupBy(t => t.TitleSetNumber))
                {
                    var vtsNum = vtsGroup.Key;
                    if (vtsNum == 0)
                    {
                        continue;
                    }

                    var vtsIfoName = FormattableString.Invariant($"VTS_{vtsNum:D2}_0.IFO");
                    var vtsIfoFile = allFiles.FirstOrDefault(
                        f => string.Equals(f.Name, vtsIfoName, StringComparison.OrdinalIgnoreCase));
                    if (vtsIfoFile is null)
                    {
                        logger?.LogWarning("VTS IFO {FileName} not found in ISO {Path}", vtsIfoName, isoPath);
                        continue;
                    }

                    byte[] vtsData;
                    try
                    {
                        using var stream = udfReader.OpenFile(vtsIfoFile.FullName, FileMode.Open, FileAccess.Read);
                        vtsData = ReadStreamFully(stream);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to read VTS IFO {FileName} from ISO", vtsIfoName);
                        continue;
                    }

                    ParseVtsIfo(vtsData, vtsNum, vtsGroup, logger);
                }

                return disc;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to parse DVD ISO at {Path}", isoPath);
                return new DvdDiscInfo();
            }
        }

        /// <summary>
        /// Returns the title most likely to be the main feature: the longest title whose
        /// duration is at or above <paramref name="minDuration"/> (default: no minimum).
        /// </summary>
        /// <param name="disc">Parsed disc info.</param>
        /// <param name="minDuration">Optional minimum duration threshold.</param>
        /// <returns>The best-candidate <see cref="DvdTitleInfo"/>, or <c>null</c> if none qualifies.</returns>
        internal static DvdTitleInfo? FindMainFeature(DvdDiscInfo disc, TimeSpan? minDuration = null)
        {
            IEnumerable<DvdTitleInfo> candidates = disc.Titles.Where(t => t.Duration.HasValue);
            if (minDuration.HasValue)
            {
                candidates = candidates.Where(t => t.Duration >= minDuration);
            }

            return candidates.OrderByDescending(t => t.Duration).FirstOrDefault();
        }

        // ── VMG IFO (VIDEO_TS.IFO) parser ────────────────────────────────────

        /// <summary>
        /// Reads the number of titles from a DVD Video Manager IFO (VIDEO_TS.IFO) stream.
        /// Kept for callers that only need the count without full disc parsing.
        /// </summary>
        /// <remarks>
        /// Implements the same logic as <c>ifoRead_TT_SRPT</c> in libdvdread.
        /// vmgi_mat_t layout (big-endian multi-byte fields):
        ///   0x00–0x0B  vmg_identifier   "DVDVIDEO-VMG"
        ///   0xC0–0xC3  vmgm_vobs        sector address of menu VOBs (skipped)
        ///   0xC4–0xC7  tt_srpt          sector address of the TT_SRPT table
        /// tt_srpt_t starts at tt_srpt×2048 within the file:
        ///   +0x00–0x01 nr_of_srpts      number of titles (big-endian uint16).
        /// </remarks>
        /// <param name="ifoStream">Seekable stream positioned at the start of VIDEO_TS.IFO.</param>
        /// <returns>The number of titles, or 0 if the stream is not a valid VMG IFO.</returns>
        internal static int ReadVmgTitleCount(Stream ifoStream)
        {
            Span<byte> magic = stackalloc byte[12];
            if (ifoStream.Read(magic) < 12)
            {
                return 0;
            }

            if (!magic.SequenceEqual("DVDVIDEO-VMG"u8))
            {
                return 0;
            }

            ifoStream.Seek(0xC4, SeekOrigin.Begin);
            Span<byte> sectorBytes = stackalloc byte[4];
            if (ifoStream.Read(sectorBytes) < 4)
            {
                return 0;
            }

            if (BitConverter.IsLittleEndian)
            {
                sectorBytes.Reverse();
            }

            var srptSector = BitConverter.ToUInt32(sectorBytes);
            if (srptSector == 0)
            {
                return 0;
            }

            var srptOffset = (long)srptSector * DvdSectorSize;
            if (srptOffset + 2 > ifoStream.Length)
            {
                return 0;
            }

            ifoStream.Seek(srptOffset, SeekOrigin.Begin);
            Span<byte> countBytes = stackalloc byte[2];
            if (ifoStream.Read(countBytes) < 2)
            {
                return 0;
            }

            if (BitConverter.IsLittleEndian)
            {
                countBytes.Reverse();
            }

            return BitConverter.ToUInt16(countBytes);
        }

        // ── Core IFO parsers ──────────────────────────────────────────────────

        /// <summary>
        /// Parses the VMG IFO byte array and builds a <see cref="DvdDiscInfo"/> from the
        /// TT_SRPT (Title Search Pointer Table).
        /// </summary>
        /// <remarks>
        /// vmgi_mat_t (VIDEO_TS.IFO header, all multi-byte values big-endian):
        ///   0x00  vmg_identifier[12]   "DVDVIDEO-VMG"
        ///   0xC4  tt_srpt              sector pointer to TT_SRPT table
        ///
        /// tt_srpt_t at tt_srpt×2048:
        ///   +0x00 nr_of_srpts          UInt16BE — title count
        ///   +0x04 last_byte            UInt32BE — byte offset of last byte of table
        ///   +0x08 title entries        12 bytes each (title_info_t)
        ///
        /// title_info_t (12 bytes, per libdvdread ifo_types.h):
        ///   +0x00 pb_ty                1 byte — playback type flags
        ///   +0x01 nr_of_angles         1 byte
        ///   +0x02 nr_of_ptts           UInt16BE — chapter count
        ///   +0x04 parental_id          UInt16BE (ignored)
        ///   +0x06 title_set_nr         1 byte — VTS number
        ///   +0x07 vts_ttn              1 byte — title number within VTS
        ///   +0x08 title_set_sector     UInt32BE — VTS start sector on disc.
        /// </remarks>
        private static DvdDiscInfo ParseVmgIfo(byte[] data)
        {
            var disc = new DvdDiscInfo();

            if (!HasMagic(data, "DVDVIDEO-VMG"))
            {
                return disc;
            }

            // tt_srpt sector pointer at 0xC4
            if (data.Length < 0xC8)
            {
                return disc;
            }

            uint srptSector = ReadUInt32BE(data, 0xC4);
            if (srptSector == 0)
            {
                return disc;
            }

            long srptByteOffset = (long)srptSector * DvdSectorSize;

            // Need at least the 8-byte TT_SRPT header
            if (srptByteOffset + 8 > data.Length)
            {
                return disc;
            }

            int srptBase = (int)srptByteOffset;
            int titleCount = ReadUInt16BE(data, srptBase + 0);
            // srptBase+2: reserved (skipped)
            // srptBase+4: last_byte (used for an additional bounds check)
            uint lastByte = ReadUInt32BE(data, srptBase + 4);

            if (titleCount <= 0)
            {
                return disc;
            }

            // Each title_info_t entry is 12 bytes; verify all entries fit.
            long entriesEnd = srptByteOffset + 8 + ((long)titleCount * 12);
            if (entriesEnd > data.Length)
            {
                return disc;
            }

            for (int i = 0; i < titleCount; i++)
            {
                int o = srptBase + 8 + (i * 12);

                // title_info_t layout (12 bytes):
                // o+0  pb_ty          — playback type (unused here)
                // o+1  nr_of_angles   — angle count
                // o+2  nr_of_ptts     — chapter count (UInt16BE)
                // o+4  parental_id    — parental mask (UInt16BE, unused)
                // o+6  title_set_nr   — VTS number
                // o+7  vts_ttn        — VTS-local title number
                // o+8  title_set_sector — VTS start sector (UInt32BE)
                byte angleCount = data[o + 1];
                int chapterCount = ReadUInt16BE(data, o + 2);
                byte vtsNumber = data[o + 6];
                byte vtsTitleNum = data[o + 7];
                uint vtsStartSec = ReadUInt32BE(data, o + 8);

                disc.Titles.Add(new DvdTitleInfo
                {
                    TitleNumber = i + 1,
                    TitleSetNumber = vtsNumber,
                    VtsTitleNumber = vtsTitleNum,
                    ChapterCount = chapterCount,
                    AngleCount = angleCount,
                    VtsStartSector = vtsStartSec
                });
            }

            return disc;
        }

        /// <summary>
        /// Parses a VTS IFO byte array and fills in <see cref="DvdTitleInfo.Duration"/>,
        /// <see cref="DvdTitleInfo.ProgramChainNumber"/>, and <see cref="DvdTitleInfo.ProgramNumber"/>
        /// for each of the supplied titles belonging to this title set.
        /// </summary>
        /// <remarks>
        /// vtsi_mat_t sector pointers used (big-endian UInt32, relative to VTS IFO start):
        ///   0xC8  vts_ptt_srpt    → VTS_PTT_SRPT table
        ///   0xCC  vts_pgcit       → VTS_PGCITI table
        ///
        /// VTS_PTT_SRPT maps VTS-local title number → first PGC + program number.
        /// VTS_PGCITI maps PGC number → PGC playback time.
        ///
        /// Mutates <paramref name="titles"/> in-place, setting <see cref="DvdTitleInfo.ProgramChainNumber"/>,
        /// <see cref="DvdTitleInfo.ProgramNumber"/>, and <see cref="DvdTitleInfo.Duration"/> on each entry.
        /// </remarks>
        private static void ParseVtsIfo(byte[] data, int vtsNumber, IEnumerable<DvdTitleInfo> titles, ILogger? logger)
        {
            if (!HasMagic(data, "DVDVIDEO-VTS"))
            {
                logger?.LogWarning("VTS_{VtsNum:D2}_0.IFO has wrong magic bytes — skipping", vtsNumber);
                return;
            }

            // vtsi_mat_t: vts_ptt_srpt at 0xC8, vts_pgcit at 0xCC
            if (data.Length < 0xD0)
            {
                logger?.LogWarning("VTS_{VtsNum:D2}_0.IFO is too short to contain VTSI_MAT pointers", vtsNumber);
                return;
            }

            uint pttSrptSector = ReadUInt32BE(data, 0xC8);
            uint pgcitiSector = ReadUInt32BE(data, 0xCC);

            // Parse VTS_PTT_SRPT → VTS title → (PGC number, program number)
            Dictionary<int, (int PgcNumber, int ProgramNumber)>? pttMap = null;
            if (pttSrptSector > 0)
            {
                long pttBase = (long)pttSrptSector * DvdSectorSize;
                if (pttBase + 8 <= data.Length)
                {
                    pttMap = ParseVtsPttSrpt(data, (int)pttBase);
                }
                else
                {
                    logger?.LogWarning(
                        "VTS_{VtsNum:D2}_0.IFO: VTS_PTT_SRPT sector {Sector} points outside file",
                        vtsNumber,
                        pttSrptSector);
                }
            }

            // Parse VTS_PGCITI → PGC number → duration
            Dictionary<int, TimeSpan?>? pgcDurations = null;
            if (pgcitiSector > 0)
            {
                long pgcitiBase = (long)pgcitiSector * DvdSectorSize;
                if (pgcitiBase + 8 <= data.Length)
                {
                    pgcDurations = ParseVtsPgciti(data, (int)pgcitiBase);
                }
                else
                {
                    logger?.LogWarning(
                        "VTS_{VtsNum:D2}_0.IFO: VTS_PGCITI sector {Sector} points outside file",
                        vtsNumber,
                        pgcitiSector);
                }
            }

            foreach (var title in titles)
            {
                if (pttMap is not null && pttMap.TryGetValue(title.VtsTitleNumber, out var pttEntry))
                {
                    title.ProgramChainNumber = pttEntry.PgcNumber;
                    title.ProgramNumber = pttEntry.ProgramNumber;

                    if (pgcDurations is not null && pgcDurations.TryGetValue(pttEntry.PgcNumber, out var duration))
                    {
                        title.Duration = duration;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the VTS_PTT_SRPT (VTS Part-of-Title Search Pointer Table) and returns a
        /// dictionary mapping VTS-local title number → (first PGC number, first program number).
        /// </summary>
        /// <remarks>
        /// vts_ptt_srpt_t layout (per libdvdread ifo_types.h):
        ///   +0x00 nr_of_srpts    UInt16BE — number of VTS titles
        ///   +0x02 (reserved)
        ///   +0x04 last_byte      UInt32BE — last byte of table, relative to table base
        ///   +0x08 offset[]       UInt32BE per VTS title, relative to table base
        ///         Each offset points to the first ttu_t (PTT) entry for that title.
        ///
        /// ttu_t (4 bytes per chapter/PTT):
        ///   +0x00 pgcn           UInt16BE — PGC number (1-based)
        ///   +0x02 pgn            UInt16BE — program number within PGC (1-based).
        /// </remarks>
        private static Dictionary<int, (int PgcNumber, int ProgramNumber)> ParseVtsPttSrpt(byte[] data, int pttBase)
        {
            var result = new Dictionary<int, (int PgcNumber, int ProgramNumber)>();

            if (pttBase + 8 > data.Length)
            {
                return result;
            }

            int vtsTitleCount = ReadUInt16BE(data, pttBase + 0);
            uint lastByte = ReadUInt32BE(data, pttBase + 4);

            if (vtsTitleCount <= 0)
            {
                return result;
            }

            // Verify offset table fits
            int offsetTableEnd = pttBase + 8 + (vtsTitleCount * 4);
            if (offsetTableEnd > data.Length)
            {
                return result;
            }

            for (int n = 1; n <= vtsTitleCount; n++)
            {
                int offsetPos = pttBase + 8 + ((n - 1) * 4);
                uint titleRelOffset = ReadUInt32BE(data, offsetPos);

                // Determine how many PTT/chapter entries this VTS title has (for bounds safety).
                uint nextRelOffset;
                if (n < vtsTitleCount)
                {
                    nextRelOffset = ReadUInt32BE(data, offsetPos + 4);
                }
                else
                {
                    // last_byte is the byte index of the last byte, so +1 gives exclusive end
                    nextRelOffset = lastByte + 1;
                }

                if (nextRelOffset <= titleRelOffset)
                {
                    // Degenerate or corrupt entry — no chapters
                    continue;
                }

                // Each ttu_t is 4 bytes; only the first (title's first chapter) is needed
                long firstPttOffset = (long)pttBase + titleRelOffset;
                if (firstPttOffset + 4 > data.Length)
                {
                    break;
                }

                int pgcNumber = ReadUInt16BE(data, (int)firstPttOffset + 0);
                int programNumber = ReadUInt16BE(data, (int)firstPttOffset + 2);

                if (pgcNumber > 0) // PGC 0 is invalid per spec
                {
                    result[n] = (pgcNumber, programNumber);
                }
            }

            return result;
        }

        /// <summary>
        /// Parses the VTS_PGCITI (VTS PGC Information Table) and returns a dictionary
        /// mapping PGC number (1-based) → playback duration.
        /// </summary>
        /// <remarks>
        /// vts_pgcit_t layout (per libdvdread ifo_types.h):
        ///   +0x00 nr_of_pgci_srp  UInt16BE — number of PGC search pointers
        ///   +0x02 (reserved)
        ///   +0x04 last_byte       UInt32BE
        ///   +0x08 pgci_srp[]      8 bytes each (pgci_srp_t)
        ///
        /// pgci_srp_t (8 bytes):
        ///   +0x00 entry_id        1 byte
        ///   +0x01 flags           1 byte  (block_mode / block_type bits)
        ///   +0x02 ptl_id_mask     UInt16BE
        ///   +0x04 pgc_start_byte  UInt32BE — byte offset of PGC, relative to table base
        ///
        /// pgc_t at pgciti_base + pgc_start_byte:
        ///   +0x00 (reserved)
        ///   +0x01 nr_of_programs  1 byte
        ///   +0x02 nr_of_cells     1 byte
        ///   +0x03 (reserved)
        ///   +0x04 playback_time   4 bytes  (dvd_time_t / BCD + frame-rate).
        /// </remarks>
        private static Dictionary<int, TimeSpan?> ParseVtsPgciti(byte[] data, int pgcitiBase)
        {
            var result = new Dictionary<int, TimeSpan?>();

            if (pgcitiBase + 8 > data.Length)
            {
                return result;
            }

            int pgcCount = ReadUInt16BE(data, pgcitiBase + 0);
            if (pgcCount <= 0)
            {
                return result;
            }

            for (int n = 1; n <= pgcCount; n++)
            {
                // pgci_srp_t is 8 bytes; search pointer for PGC n is at index n-1
                int srpOffset = pgcitiBase + 8 + ((n - 1) * 8);
                if (srpOffset + 8 > data.Length)
                {
                    result[n] = null;
                    continue;
                }

                // pgc_start_byte at srp+4, relative to pgcitiBase
                uint pgcRelOffset = ReadUInt32BE(data, srpOffset + 4);
                long pgcAddress = (long)pgcitiBase + pgcRelOffset;

                // Need at least 8 bytes of PGC header to read playback_time at +4
                if (pgcAddress + 8 > data.Length)
                {
                    result[n] = null;
                    continue;
                }

                result[n] = TryReadDvdTime(data, (int)pgcAddress + 4);
            }

            return result;
        }

        // ── DVD time decoding ─────────────────────────────────────────────────

        /// <summary>
        /// Decodes a 4-byte DVD time field (dvd_time_t) into a <see cref="TimeSpan"/>.
        /// Returns <c>null</c> if the field contains invalid BCD values or is out of bounds.
        /// </summary>
        /// <remarks>
        /// dvd_time_t layout (per libdvdread ifo_types.h):
        ///   +0  hour      BCD
        ///   +1  minute    BCD
        ///   +2  second    BCD
        ///   +3  frame_u   bits 7-6 = frame-rate code (0x40=25fps, 0xC0≈29.97fps),
        ///                 bits 5-0 = frame count.
        /// </remarks>
        private static TimeSpan? TryReadDvdTime(byte[] data, int offset)
        {
            if (offset + 4 > data.Length)
            {
                return null;
            }

            int hours = FromBcd(data[offset + 0]);
            int minutes = FromBcd(data[offset + 1]);
            int seconds = FromBcd(data[offset + 2]);

            // Validate decoded BCD values before constructing TimeSpan
            if (minutes >= 60 || seconds >= 60)
            {
                return null;
            }

            byte frameAndRate = data[offset + 3];
            int frames = frameAndRate & 0x3F;
            int rateCode = frameAndRate & 0xC0;

            double fps = rateCode switch
            {
                0x40 => 25.0,
                0xC0 => 29.97,
                _ => 0.0
            };

            double frameSeconds = fps > 0 ? frames / fps : 0.0;

            try
            {
                return new TimeSpan(hours, minutes, seconds) + TimeSpan.FromSeconds(frameSeconds);
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        /// <summary>
        /// Decodes a BCD (Binary-Coded Decimal) byte into its integer value.
        /// E.g. 0x59 → 59, 0x23 → 23.
        /// </summary>
        private static int FromBcd(byte value)
            => ((value >> 4) * 10) + (value & 0x0F);

        // ── Big-endian binary helpers ─────────────────────────────────────────

        /// <summary>Reads a big-endian unsigned 16-bit integer from <paramref name="data"/> at <paramref name="offset"/>.</summary>
        /// <param name="data">Source byte array.</param>
        /// <param name="offset">Zero-based byte offset to read from.</param>
        /// <returns>The decoded <see cref="ushort"/> value.</returns>
        internal static ushort ReadUInt16BE(byte[] data, int offset)
            => (ushort)((data[offset] << 8) | data[offset + 1]);

        /// <summary>Reads a big-endian unsigned 32-bit integer from <paramref name="data"/> at <paramref name="offset"/>.</summary>
        /// <param name="data">Source byte array.</param>
        /// <param name="offset">Zero-based byte offset to read from.</param>
        /// <returns>The decoded <see cref="uint"/> value.</returns>
        internal static uint ReadUInt32BE(byte[] data, int offset)
            => ((uint)data[offset] << 24)
             | ((uint)data[offset + 1] << 16)
             | ((uint)data[offset + 2] << 8)
             | (uint)data[offset + 3];

        // ── Utility helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if <paramref name="data"/> begins with the ASCII bytes of <paramref name="magic"/>.
        /// </summary>
        private static bool HasMagic(byte[] data, string magic)
        {
            if (data.Length < magic.Length)
            {
                return false;
            }

            for (int i = 0; i < magic.Length; i++)
            {
                if (data[i] != (byte)magic[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves the VIDEO_TS directory from a DVD root or VIDEO_TS path.
        /// Returns <c>null</c> if neither path exists.
        /// </summary>
        private static string? ResolveVideoTsPath(string dvdPath)
        {
            var candidate = Path.Combine(dvdPath, VmgIfoDirectoryName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            if (Directory.Exists(dvdPath))
            {
                return dvdPath;
            }

            return null;
        }

        /// <summary>
        /// Case-insensitive file lookup inside a directory.
        /// Returns the full path of the first match, or <c>null</c> if not found.
        /// </summary>
        private static string? FindFileInsensitive(string directory, string fileName)
            => Directory.GetFiles(directory)
                .FirstOrDefault(f => string.Equals(
                    Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));

        /// <summary>Reads all bytes from <paramref name="stream"/> regardless of its initial position.</summary>
        private static byte[] ReadStreamFully(Stream stream)
        {
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
