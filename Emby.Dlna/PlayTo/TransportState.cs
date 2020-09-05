#pragma warning disable CS1591
#pragma warning disable SA1602
#pragma warning disable CA1707

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// DO NOT CHANGE THESE VALUES OR REMOVE THE UNDERSCORES.
    /// </summary>
    public enum TransportState
    {
        Stopped,
        Playing,
        Transitioning,
        Paused_Playback,
        Paused_Recording,
        Recording,
        No_Media_Present,
        Paused,
        Error
    }
}
