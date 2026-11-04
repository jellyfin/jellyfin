#pragma warning disable CS1591

namespace MediaBrowser.Model.Entities
{
    public enum Video3DFormat
    {
        HalfSideBySide,
        FullSideBySide,
        FullTopAndBottom,
        HalfTopAndBottom,
        MVC,
        Equirectangular180SideBySide,
        Equirectangular180TopAndBottom,
        Equirectangular180Mono,
        Equirectangular360SideBySide,
        Equirectangular360TopAndBottom,
        Equirectangular360Mono,
        Fisheye180SideBySide,
        Fisheye180Mono,
        MVHEVC
    }
}
