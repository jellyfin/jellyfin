#pragma warning disable CS1591

namespace Emby.Dlna.PlayTo
{
    public enum TransportState
    {
        STOPPED,
        PLAYING,
        TRANSITIONING,
        PAUSED_PLAYBACK,
        PAUSED_RECORDING,
        RECORDING,
        NO_MEDIA_PRESENT,
        PAUSED,
        ERROR
    }
}
