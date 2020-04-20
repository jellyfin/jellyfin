#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Devices
{
    public class DeviceQuery
    {
        /// <summary>
        /// Gets or sets a value indicating whether [supports synchronize].
        /// </summary>
        /// <value><c>null</c> if [supports synchronize] contains no value, <c>true</c> if [supports synchronize]; otherwise, <c>false</c>.</value>
        public bool? SupportsSync { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public Guid UserId { get; set; }
    }
}
