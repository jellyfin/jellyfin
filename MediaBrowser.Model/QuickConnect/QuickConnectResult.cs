using System;

namespace MediaBrowser.Model.QuickConnect
{
    /// <summary>
    /// Stores the state of an quick connect request.
    /// </summary>
    public class QuickConnectResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QuickConnectResult"/> class.
        /// </summary>
        /// <param name="dateAdded">The time when the request was created.</param>
        /// <param name="deviceId">The requesting device id.</param>
        /// <param name="deviceName">The requesting device name.</param>
        /// <param name="appName">The requesting app name.</param>
        /// <param name="appVersion">The requesting app version.</param>
        public QuickConnectResult(
            DateTime dateAdded,
            string deviceId,
            string deviceName,
            string appName,
            string appVersion)
        {
            DateAdded = dateAdded;
            DeviceId = deviceId;
            DeviceName = deviceName;
            AppName = appName;
            AppVersion = appVersion;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this request is authorized.
        /// </summary>
        public bool Authenticated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating an optional UserId of a user whom this request is authorized to authenticate as.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the secret value used to uniquely identify this request. Can be used to retrieve authentication information.
        /// </summary>
        public string? Secret { get; set; }

        /// <summary>
        /// Gets or sets the user facing code used so the user can quickly differentiate this request from others.
        /// </summary>
        public string? Code { get; set;  }

        /// <summary>
        /// Gets the requesting device id.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Gets the requesting device name.
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        /// Gets the requesting app name.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        /// Gets the requesting app version.
        /// </summary>
        public string AppVersion { get; }

        /// <summary>
        /// Gets or sets the DateTime that this request was created.
        /// </summary>
        public DateTime DateAdded { get; set; }
    }
}
