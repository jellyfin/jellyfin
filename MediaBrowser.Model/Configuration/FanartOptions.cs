
namespace MediaBrowser.Model.Configuration
{
    public class FanartOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable automatic updates].
        /// </summary>
        /// <value><c>true</c> if [enable automatic updates]; otherwise, <c>false</c>.</value>
        public bool EnableAutomaticUpdates { get; set; }
        /// <summary>
        /// Gets or sets the user API key.
        /// </summary>
        /// <value>The user API key.</value>
        public string UserApiKey { get; set; }
    }
}
