using System;

namespace MediaBrowser.MediaEncoding.Probing;

/// <summary>
/// Holds parsed information about a single DVD title, combining data from
/// VIDEO_TS.IFO (TT_SRPT entry) and the corresponding VTS_xx_0.IFO
/// (VTS_PTT_SRPT + VTS_PGCITI tables).
/// </summary>
internal sealed class DvdTitleInfo
{
    /// <summary>Gets the DVD title number (1-based, as seen by the user).</summary>
    public int TitleNumber { get; init; }

    /// <summary>Gets the title-set number (VTS number) that owns this title.</summary>
    public int TitleSetNumber { get; init; }

    /// <summary>Gets the title number within its title set (VTS-local, 1-based).</summary>
    public int VtsTitleNumber { get; init; }

    /// <summary>Gets the number of chapters (PTT entries) in this title.</summary>
    public int ChapterCount { get; init; }

    /// <summary>Gets the number of angles available for this title.</summary>
    public int AngleCount { get; init; }

    /// <summary>Gets or sets the Program Chain number resolved from VTS_PTT_SRPT.</summary>
    public int ProgramChainNumber { get; set; }

    /// <summary>Gets or sets the program number within the PGC.</summary>
    public int ProgramNumber { get; set; }

    /// <summary>
    /// Gets or sets the playback duration parsed from the PGC in VTS_PGCITI.
    /// <c>null</c> if the VTS IFO was missing or the PGC data could not be parsed.
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>Gets the start sector of the VTS within the disc image.</summary>
    public uint VtsStartSector { get; init; }
}
