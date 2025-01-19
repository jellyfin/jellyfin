#pragma warning disable CS1591
namespace Jellyfin.Data.Entities;

public enum BaseItemExtraType
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
    ThemeVideo = 9,
    Featurette = 10,
    Short = 11
}
