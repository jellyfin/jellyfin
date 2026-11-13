using System.Collections.Generic;

namespace MediaBrowser.MediaEncoding.Probing;

/// <summary>
/// Holds parsed information about all titles on a DVD disc,
/// derived from VIDEO_TS.IFO and the per-title-set VTS_xx_0.IFO files.
/// </summary>
internal sealed class DvdDiscInfo
{
    /// <summary>Gets the ordered list of titles found on the disc.</summary>
    public List<DvdTitleInfo> Titles { get; } = new();
}
