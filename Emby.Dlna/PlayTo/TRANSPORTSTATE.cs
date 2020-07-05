#pragma warning disable CS1591

namespace Emby.Dlna.PlayTo
{
    public enum TransportState
    {
        /// <summary>
        /// Stopped.
        /// </summary>
        STOPPED,

        /// <summary>
        /// Playing.
        /// </summary>
        PLAYING,

        /// <summary>
        /// Transitioning.
        /// </summary>
        TRANSITIONING,

        /// <summary>
        /// Paused playback.
        /// </summary>
        PAUSED_PLAYBACK,

        /// <summary>
        /// Paused.
        /// </summary>
        PAUSED
    }
}
