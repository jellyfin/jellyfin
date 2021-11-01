using System;

namespace Jellyfin.Api.Models.PluginDtos
{
    /// <summary>
    /// MB Registration Record.
    /// </summary>
    public class MBRegistrationRecord
    {
        /// <summary>
        /// Gets or sets expiration date.
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is registered.
        /// </summary>
        public bool IsRegistered { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether reg checked.
        /// </summary>
        public bool RegChecked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether reg error.
        /// </summary>
        public bool RegError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trial version.
        /// </summary>
        public bool TrialVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is valid.
        /// </summary>
        public bool IsValid { get; set; }
    }
}
