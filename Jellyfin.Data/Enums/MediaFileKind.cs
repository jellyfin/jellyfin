using System;

namespace Jellyfin.Data.Enums
{
    public enum MediaFileKind : Int32
    {
        Main,
        Sidecar,
        AdditionalPart,
        AlternativeFormat,
        AdditionalStream
    }
}
