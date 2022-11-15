namespace Emby.Dlna.Common
{
    /// <summary>
    /// Defines the <see cref="DeviceService" />.
    /// </summary>
    public class DeviceService
    {
        /// <summary>
        /// Gets or sets the Service Type.
        /// </summary>
        public string ServiceType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Service Id.
        /// </summary>
        public string ServiceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Scpd Url.
        /// </summary>
        public string ScpdUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Control Url.
        /// </summary>
        public string ControlUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the EventSubUrl.
        /// </summary>
        public string EventSubUrl { get; set; } = string.Empty;

        /// <inheritdoc />
        public override string ToString() => ServiceId;
    }
}
