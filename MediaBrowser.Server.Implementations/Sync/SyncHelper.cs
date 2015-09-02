using System;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncHelper
    {
        public static int? AdjustBitrate(int? profileBitrate, string quality)
        {
            if (profileBitrate.HasValue)
            {
                if (string.Equals(quality, "medium", StringComparison.OrdinalIgnoreCase))
                {
                    profileBitrate = Math.Min(Convert.ToInt32(profileBitrate.Value * .7), 4000000);
                }
                else if (string.Equals(quality, "low", StringComparison.OrdinalIgnoreCase))
                {
                    profileBitrate = Math.Min(Convert.ToInt32(profileBitrate.Value * .5), 1500000);
                }
            }

            return profileBitrate;
        }
    }
}
