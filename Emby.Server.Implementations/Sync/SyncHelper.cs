using System;

namespace Emby.Server.Implementations.Sync
{
    public class SyncHelper
    {
        public static long? AdjustBitrate(long? profileBitrate, string quality)
        {
            if (profileBitrate.HasValue)
            {
                if (string.Equals(quality, "medium", StringComparison.OrdinalIgnoreCase))
                {
                    profileBitrate = Math.Min(profileBitrate.Value, 4000000);
                }
                else if (string.Equals(quality, "low", StringComparison.OrdinalIgnoreCase))
                {
                    profileBitrate = Math.Min(profileBitrate.Value, 1500000);
                }
            }

            return profileBitrate;
        }
    }
}
