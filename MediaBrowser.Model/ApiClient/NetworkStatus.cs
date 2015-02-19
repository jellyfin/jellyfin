
namespace MediaBrowser.Model.ApiClient
{
    public class NetworkStatus
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is network available.
        /// </summary>
        /// <value><c>true</c> if this instance is network available; otherwise, <c>false</c>.</value>
        public bool IsNetworkAvailable { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is local network available.
        /// </summary>
        /// <value><c>null</c> if [is local network available] contains no value, <c>true</c> if [is local network available]; otherwise, <c>false</c>.</value>
        public bool? IsLocalNetworkAvailable { get; set; }
        /// <summary>
        /// Gets the is any local network available.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool GetIsAnyLocalNetworkAvailable()
        {
            if (!IsLocalNetworkAvailable.HasValue)
            {
                return IsNetworkAvailable;
            }

            return IsLocalNetworkAvailable.Value;
        }
    }
}
