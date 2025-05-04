namespace Jellyfin.MediaEncoding.Keyframes.Matroska;

/// <summary>
/// Constants for the Matroska identifiers.
/// </summary>
public static class MatroskaConstants
{
    internal const ulong SegmentContainer = 0x18538067;

    internal const ulong SeekHead = 0x114D9B74;
    internal const ulong Seek = 0x4DBB;

    internal const ulong Info = 0x1549A966;
    internal const ulong TimestampScale = 0x2AD7B1;
    internal const ulong Duration = 0x4489;

    internal const ulong Tracks = 0x1654AE6B;
    internal const ulong TrackEntry = 0xAE;
    internal const ulong TrackNumber = 0xD7;
    internal const ulong TrackType = 0x83;

    internal const ulong TrackTypeVideo = 0x1;
    internal const ulong TrackTypeSubtitle = 0x11;

    internal const ulong Cues = 0x1C53BB6B;
    internal const ulong CueTime = 0xB3;
    internal const ulong CuePoint = 0xBB;
    internal const ulong CueTrackPositions = 0xB7;
    internal const ulong CuePointTrackNumber = 0xF7;
}
