#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

namespace MediaBrowser.Model.Session
{
    public enum PlayMethod
    {
        Transcode = 0,
        DirectStream = 1,
        DirectPlay = 2
    }
}
