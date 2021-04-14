using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities.Security
{
    /// <summary>
    /// An entity representing a device.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="appName">The app name.</param>
        /// <param name="appVersion">The app version.</param>
        /// <param name="deviceName">The device name.</param>
        /// <param name="deviceId">The device id.</param>
        public Device(Guid userId, string accessToken, string appName, string appVersion, string deviceName, string deviceId)
        {
            UserId = userId;
            AccessToken = accessToken;
            AppName = appName;
            AppVersion = appVersion;
            DeviceName = deviceName;
            DeviceId = deviceId;

            DateCreated = DateTime.UtcNow;
            DateLastActivity = DateCreated;

            // Non-nullable for EF Core, as this is a required relationship.
            User = null!;
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the app name.
        /// </summary>
        [MaxLength(64)]
        [StringLength(64)]
        public string AppName { get; set; }

        /// <summary>
        /// Gets or sets the app version.
        /// </summary>
        [MaxLength(32)]
        [StringLength(32)]
        public string AppVersion { get; set; }

        /// <summary>
        /// Gets or sets the device name.
        /// </summary>
        [MaxLength(64)]
        [StringLength(64)]
        public string DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the device id.
        /// </summary>
        [MaxLength(256)]
        [StringLength(256)]
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this device is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets the date this device was created.
        /// </summary>
        public DateTime DateCreated { get; private set; }

        /// <summary>
        /// Gets or sets the date of last activity.
        /// </summary>
        public DateTime DateLastActivity { get; set; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        public User User { get; private set; }
    }
}
