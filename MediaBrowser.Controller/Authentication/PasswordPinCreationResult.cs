using System;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// The result of creating a password reset pin.
    /// </summary>
    public class PasswordPinCreationResult
    {
        /// <summary>
        /// Gets or sets the path of the file holding the pin.
        /// </summary>
        public string PinFile { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date the pin expires.
        /// </summary>
        public DateTime ExpirationDate { get; set; }
    }
}
