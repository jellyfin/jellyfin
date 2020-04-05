#nullable disable
#pragma warning disable CS1591

namespace MediaBrowser.Model.Users
{
    public class PinRedeemResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PinRedeemResult"/> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the users reset.
        /// </summary>
        /// <value>The users reset.</value>
        public string[] UsersReset { get; set; }
    }
}
