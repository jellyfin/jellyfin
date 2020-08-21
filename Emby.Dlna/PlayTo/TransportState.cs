#pragma warning disable CS1591
#pragma warning disable SA1602

namespace Emby.Dlna.PlayTo
{
    public enum TransportState
    {
        STOPPED,
        PLAYING,
        TRANSITIONING,
        PAUSEDPLAYBACK,
        PAUSEDRECORDING,
        RECORDING,
        NOMEDIAPRESENT,
        PAUSED,
        ERROR
    }
}
