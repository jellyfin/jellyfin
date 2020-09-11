#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Users
{
    public class ForgotPasswordResult
    {
        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>The action.</value>
        public ForgotPasswordAction Action { get; set; }

        /// <summary>
        /// Gets or sets the pin file.
        /// </summary>
        /// <value>The pin file.</value>
        public string PinFile { get; set; }

        /// <summary>
        /// Gets or sets the pin expiration date.
        /// </summary>
        /// <value>The pin expiration date.</value>
        public DateTime? PinExpirationDate { get; set; }
    }
}
