#pragma warning disable CS1591

namespace Emby.Dlna.Common
{
    public class DeviceService
    {
        public string ServiceType { get; set; }

        public string ServiceId { get; set; }

        public string ScpdUrl { get; set; }

        public string ControlUrl { get; set; }

        public string EventSubUrl { get; set; }

        /// <inheritdoc />
        public override string ToString()
            => ServiceId;
    }
}
