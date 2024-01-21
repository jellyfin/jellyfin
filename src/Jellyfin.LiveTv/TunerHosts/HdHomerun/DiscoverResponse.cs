#nullable disable

using System;

namespace Jellyfin.LiveTv.TunerHosts.HdHomerun
{
    internal class DiscoverResponse
    {
        public string FriendlyName { get; set; }

        public string ModelNumber { get; set; }

        public string FirmwareName { get; set; }

        public string FirmwareVersion { get; set; }

        public string DeviceID { get; set; }

        public string DeviceAuth { get; set; }

        public string BaseURL { get; set; }

        public string LineupURL { get; set; }

        public int TunerCount { get; set; }

        public bool SupportsTranscoding
        {
            get
            {
                var model = ModelNumber ?? string.Empty;

                if (model.Contains("hdtc", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return false;
            }
        }
    }
}
