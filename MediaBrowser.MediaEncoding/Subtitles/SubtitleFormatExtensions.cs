using System.Diagnostics.CodeAnalysis;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles;

internal static class SubtitleFormatExtensions
{
    public static bool TryGetErrors(this SubtitleFormat format, [NotNullWhen(true)] out string? errors)
    {
        errors = format switch
        {
            SubStationAlpha ssa => ssa.Errors,
            AdvancedSubStationAlpha assa => assa.Errors,
            SubRip subRip => subRip.Errors,
            MicroDvd microDvd => microDvd.Errors,
            DCinemaSmpte2007 smpte2007 => smpte2007.Errors,
            DCinemaSmpte2010 smpte2010 => smpte2010.Errors,
            _ => null,
        };

        return errors is not null;
    }
}
