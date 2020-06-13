#pragma warning disable CS1591
#pragma warning disable SA1602

using System;

namespace MediaBrowser.Common.Net
{
    [Flags]
    public enum CompressionMethods
    {
        None = 0b00000001,
        Deflate = 0b00000010,
        Gzip = 0b00000100
    }
}
