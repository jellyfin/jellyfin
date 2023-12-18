using System;
using MediaBrowser.Model.Drawing;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Static helper class used to draw percentage-played indicators on images.
/// </summary>
public static class PercentPlayedDrawer
{
    private const int IndicatorHeight = 8;

    /// <summary>
    /// Draw a percentage played indicator on a canvas.
    /// </summary>
    /// <param name="canvas">The canvas to draw the indicator on.</param>
    /// <param name="imageSize">The size of the image being drawn on.</param>
    /// <param name="percent">The percentage played to display with the indicator.</param>
    public static void Process(SKCanvas canvas, ImageDimensions imageSize, double percent)
    {
        using var paint = new SKPaint();
        var endX = imageSize.Width - 1;
        var endY = imageSize.Height - 1;

        paint.Color = SKColor.Parse("#99000000");
        paint.Style = SKPaintStyle.Fill;
        canvas.DrawRect(SKRect.Create(0, (float)endY - IndicatorHeight, endX, endY), paint);

        double foregroundWidth = (endX * percent) / 100;

        paint.Color = SKColor.Parse("#FF00A4DC");
        canvas.DrawRect(SKRect.Create(0, (float)endY - IndicatorHeight, Convert.ToInt32(foregroundWidth), endY), paint);
    }
}
