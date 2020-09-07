#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public class DeviceService
    {
        public string ServiceType { get; set; } = string.Empty;

        public string ServiceId { get; set; } = string.Empty;

        public string ScpdUrl { get; set; } = string.Empty;

        public string ControlUrl { get; set; } = string.Empty;

        public string EventSubUrl { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString()
            => ServiceId;
    }
}
