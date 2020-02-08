#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dlna
{
    public enum PlaybackErrorCode
    {
        NotAllowed = 0,
        NoCompatibleStream = 1,
        RateLimitExceeded = 2
    }
}
