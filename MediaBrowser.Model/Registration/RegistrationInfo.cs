using System;

namespace MediaBrowser.Model.Registration
{
    public class RegistrationInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        /// <value>The expiration date.</value>
        public DateTime ExpirationDate { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is trial.
        /// </summary>
        /// <value><c>true</c> if this instance is trial; otherwise, <c>false</c>.</value>
        public bool IsTrial { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is valid.
        /// </summary>
        /// <value><c>true</c> if this instance is valid; otherwise, <c>false</c>.</value>
        public bool IsValid { get; set; }
    }
}
