
namespace MediaBrowser.Controller.Security
{
    public class AuthenticationInfoQuery
    {
        /// <summary>
        /// Gets or sets the device identifier.
        /// </summary>
        /// <value>The device identifier.</value>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>null</c> if [is active] contains no value, <c>true</c> if [is active]; otherwise, <c>false</c>.</value>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has user.
        /// </summary>
        /// <value><c>null</c> if [has user] contains no value, <c>true</c> if [has user]; otherwise, <c>false</c>.</value>
        public bool? HasUser { get; set; }
        
        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }
    }
}
