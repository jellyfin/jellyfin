using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities.Security
{
    /// <summary>
    /// An entity representing custom options for a device.
    /// </summary>
    public class DeviceOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceOptions"/> class.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        public DeviceOptions(string deviceId)
        {
            DeviceId = deviceId;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the device id.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets or sets the custom name.
        /// </summary>
        public string? CustomName { get; set; }
    }
}
