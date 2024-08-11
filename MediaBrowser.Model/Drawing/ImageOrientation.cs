#pragma warning disable CS1591

namespace MediaBrowser.Model.Drawing
{
#pragma warning disable CA1008 // Disabling Null default rule check since the enum is already shipped and default is handled in GetSKEncodedOrigin()
    public enum ImageOrientation
    {
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 3,
        BottomLeft = 4,
        LeftTop = 5,
        RightTop = 6,
        RightBottom = 7,
        LeftBottom = 8,
    }
#pragma warning restore CA1008
}
