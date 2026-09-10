using System;
using System.Globalization;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// The trickplay api model.
/// </summary>
public record TrickplayInfoDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayInfoDto"/> class.
    /// </summary>
    /// <param name="info">The trickplay info.</param>
    public TrickplayInfoDto(TrickplayInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        Width = info.Width;
        Height = info.Height;
        TileWidth = info.TileWidth;
        TileHeight = info.TileHeight;
        ThumbnailCount = info.ThumbnailCount;
        Interval = info.Interval;
        Bandwidth = info.Bandwidth;
        DetectedAspectRatio = info.DetectedAspectRatio;

        if (!string.IsNullOrEmpty(info.DetectedAspectRatio)
            && double.TryParse(info.DetectedAspectRatio, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
        {
            var snapped = AspectRatioLookup.SnapToStandard(raw);
            if (snapped > 0)
            {
                DetectedAspectRatioSnapped = AspectRatioLookup.Format(snapped);
                DetectedAspectRatioSnappedName = AspectRatioLookup.GetName(snapped);
            }
        }
    }

    /// <summary>
    /// Gets the width of an individual thumbnail.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Gets the height of an individual thumbnail.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Gets the amount of thumbnails per row.
    /// </summary>
    public int TileWidth { get; init; }

    /// <summary>
    /// Gets the amount of thumbnails per column.
    /// </summary>
    public int TileHeight { get; init; }

    /// <summary>
    /// Gets the total amount of non-black thumbnails.
    /// </summary>
    public int ThumbnailCount { get; init; }

    /// <summary>
    /// Gets the interval in milliseconds between each trickplay thumbnail.
    /// </summary>
    public int Interval { get; init; }

    /// <summary>
    /// Gets the peak bandwidth usage in bits per second.
    /// </summary>
    public int Bandwidth { get; init; }

    /// <summary>
    /// Gets the raw aspect ratio detected by black bar analysis of trickplay thumbnails.
    /// </summary>
    public string? DetectedAspectRatio { get; init; }

    /// <summary>
    /// Gets the detected aspect ratio snapped to the nearest industry standard value.
    /// </summary>
    public string? DetectedAspectRatioSnapped { get; init; }

    /// <summary>
    /// Gets the industry name of the snapped aspect ratio (e.g. "Modern Anamorphic Scope").
    /// </summary>
    public string? DetectedAspectRatioSnappedName { get; init; }
}
