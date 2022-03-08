#pragma warning disable CS1591

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Core of the AVTransport service. It defines the conceptually top-
    /// level state of the transport, for example, whether it is playing, recording, etc.
    /// </summary>
    public enum TransportState
    {
        STOPPED,
        PLAYING,
        TRANSITIONING,
        PAUSED_PLAYBACK
    }
}
