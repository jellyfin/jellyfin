using SkiaSharp;

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// The SkiaSharp extensions.
/// </summary>
public static class SkiaExtensions
{
    /// <summary>
    /// Draws an SKBitmap on the canvas with specified SkSamplingOptions.
    /// </summary>
    /// <param name="canvas">The SKCanvas to draw on.</param>
    /// <param name="bitmap">The SKBitmap to draw.</param>
    /// <param name="dest">The destination SKRect.</param>
    /// <param name="options">The SKSamplingOptions to use for rendering.</param>
    /// <param name="paint">Optional SKPaint to apply additional effects or styles.</param>
    public static void DrawBitmap(this SKCanvas canvas, SKBitmap bitmap, SKRect dest, SKSamplingOptions options, SKPaint? paint = null)
    {
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, dest, options, paint);
    }

    /// <summary>
    /// Draws an SKBitmap on the canvas at the specified coordinates with the given SkSamplingOptions.
    /// </summary>
    /// <param name="canvas">The SKCanvas to draw on.</param>
    /// <param name="bitmap">The SKBitmap to draw.</param>
    /// <param name="x">The x-coordinate where the bitmap will be drawn.</param>
    /// <param name="y">The y-coordinate where the bitmap will be drawn.</param>
    /// <param name="options">The SKSamplingOptions to use for rendering.</param>
    /// <param name="paint">Optional SKPaint to apply additional effects or styles.</param>
    public static void DrawBitmap(this SKCanvas canvas, SKBitmap bitmap, float x, float y, SKSamplingOptions options, SKPaint? paint = null)
    {
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, x, y, options, paint);
    }

    /// <summary>
    /// Draws an SKBitmap on the canvas using a specified source rectangle, destination rectangle,
    /// and optional paint, with the given SkSamplingOptions.
    /// </summary>
    /// <param name="canvas">The SKCanvas to draw on.</param>
    /// <param name="bitmap">The SKBitmap to draw.</param>
    /// <param name="source">
    /// The source SKRect defining the portion of the bitmap to draw.
    /// </param>
    /// <param name="dest">
    /// The destination SKRect defining the area on the canvas where the bitmap will be drawn.
    /// </param>
    /// <param name="options">The SKSamplingOptions to use for rendering.</param>
    /// <param name="paint">Optional SKPaint to apply additional effects or styles.</param>
    public static void DrawBitmap(this SKCanvas canvas, SKBitmap bitmap, SKRect source, SKRect dest, SKSamplingOptions options, SKPaint? paint = null)
    {
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, source, dest, options, paint);
    }
}
