using System.Diagnostics.CodeAnalysis;
using Nikse.SubtitleEdit.Core.SubtitleFormats;

namespace MediaBrowser.MediaEncoding.Subtitles;

internal static class SubtitleFormatExtensions
{
    /// <summary>
    /// Will try to find errors if supported by provider.
    /// </summary>
    /// <param name="format">The subtitle format.</param>
    /// <param name="errors">The out errors value.</param>
    /// <returns>True if errors are available for given format.</returns>
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

        return !string.IsNullOrWhiteSpace(errors);
    }
}
