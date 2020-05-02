using System;

namespace Jellyfin.Data.Enums
{
    public enum PreferenceKind : Int32
    {
        MaxParentalRating,
        BlockedTags,
        RemoteClientBitrateLimit,
        EnabledDevices,
        EnabledChannels,
        EnabledFolders,
        EnableContentDeletionFromFolders
    }
}
