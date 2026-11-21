using System;
using System.Collections.Generic;
using System.Globalization;

namespace MediaBrowser.Model.Drawing;

/// <summary>
/// Provides industry-standard aspect ratio lookup and snapping.
/// </summary>
public static class AspectRatioLookup
{
    /// <summary>
    /// Maximum allowed deviation between a computed ratio and a standard ratio
    /// for snapping to apply. Beyond this tolerance the ratio is considered unresolvable.
    /// </summary>
    public const double SnapTolerance = 0.075;

    /// <summary>
    /// Industry-standard aspect ratios ordered by value.
    /// Key is the decimal ratio, value is the common name.
    /// </summary>
    public static readonly IReadOnlyList<(double Ratio, string Name)> StandardRatios = new[]
    {
        (1.00, "1:1 Square"),
        (1.19, "Movietone"),
        (1.33, "4:3 Standard TV"),
        (1.37, "Academy Ratio"),
        (1.43, "IMAX GT"),
        (1.50, "VistaVision (3:2)"),
        (1.56, "14:9 Transition Broadcast"),
        (1.66, "European Widescreen"),
        (1.75, "7:4 Widescreen"),
        (1.78, "16:9 HDTV"),
        (1.85, "US Widescreen (Flat)"),
        (1.90, "IMAX Digital"),
        (2.00, "Univisium"),
        (2.06, "18.5:9 Digital/Smartphone"),
        (2.11, "19:9 Smartphone / ARRI Alexa 65"),
        (2.20, "70mm Standard"),
        (2.22, "20:9 Ultrawide Smartphone"),
        (2.33, "21:9 Ultrawide Monitor"),
        (2.35, "Early Anamorphic Scope"),
        (2.39, "Modern Anamorphic Scope"),
        (2.40, "Blu-ray Scope"),
        (2.55, "Early CinemaScope"),
        (2.66, "Original 1953 CinemaScope"),
        (2.76, "Ultra Panavision 70"),
    };

    /// <summary>
    /// Snaps a computed aspect ratio to the nearest industry-standard value,
    /// provided the deviation is within <see cref="SnapTolerance"/>.
    /// </summary>
    /// <param name="computedRatio">The raw computed aspect ratio.</param>
    /// <returns>
    /// The snapped ratio if a standard match was found within tolerance,
    /// or -1 if the ratio could not be resolved to any standard.
    /// </returns>
    public static double SnapToStandard(double computedRatio)
    {
        var bestDelta = double.MaxValue;
        var bestRatio = -1.0;

        foreach (var (ratio, _) in StandardRatios)
        {
            var delta = Math.Abs(computedRatio - ratio);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestRatio = ratio;
            }
        }

        return bestDelta <= SnapTolerance ? bestRatio : -1.0;
    }

    /// <summary>
    /// Looks up the industry name for a given standard aspect ratio.
    /// </summary>
    /// <param name="ratio">The aspect ratio value.</param>
    /// <returns>The industry name, or null if not found.</returns>
    public static string? GetName(double ratio)
    {
        foreach (var (r, name) in StandardRatios)
        {
            if (Math.Abs(r - ratio) < 0.005)
            {
                return name;
            }
        }

        return null;
    }

    /// <summary>
    /// Formats a ratio as a 3-decimal string using invariant culture.
    /// </summary>
    /// <param name="ratio">The aspect ratio.</param>
    /// <returns>Formatted string like "2.350".</returns>
    public static string Format(double ratio)
        => ratio.ToString("F3", CultureInfo.InvariantCulture);
}
