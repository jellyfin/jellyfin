#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

namespace MediaBrowser.Model.Entities
{
    public enum ExtraType
    {
        Unknown = 0,
        Clip = 1,
        Trailer = 2,
        BehindTheScenes = 3,
        DeletedScene = 4,
        Interview = 5,
        Scene = 6,
        Sample = 7,
        ThemeSong = 8,
        ThemeVideo = 9
    }
}
